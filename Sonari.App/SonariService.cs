using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sonari.Crunchyroll;
using Sonari.Kubernetes;
using Sonari.Sonarr;
using Sonari.Sonarr.Models;
using Sonari.WasariDaemon;
using TomLonghurst.EnumerableAsyncProcessor.Extensions;

namespace Sonari.App;

public class SonariService
{
    private static readonly string[] Anime4KArguments = new[]
    {
        "--shader",
        "anime4k",
        "-r",
        "4k"
    };

    public SonariService(SonarrService sonarrService,
        KubernetesService kubernetesService,
        ILogger<SonariService> logger,
        IOptions<SonarrOptions> sonarrOptions,
        IOptions<SonariOptions> sonariOptions, IServiceProvider serviceProvider,
        CrunchyrollApiService crunchyrollApi)
    {
        SonarrService = sonarrService;
        KubernetesService = kubernetesService;
        Logger = logger;
        ServiceProvider = serviceProvider;
        CrunchyrollApi = crunchyrollApi;
        SonarrOptions = sonarrOptions.Value;
        SonariOptions = sonariOptions.Value;
    }

    private SonarrService SonarrService { get; }

    private KubernetesService KubernetesService { get; }

    private ILogger<SonariService> Logger { get; }

    private SonarrOptions SonarrOptions { get; }

    private SonariOptions SonariOptions { get; }

    private IServiceProvider ServiceProvider { get; }

    private CrunchyrollApiService CrunchyrollApi { get; }

    private async IAsyncEnumerable<Episode> GetMissingEpisodes(int tagId)
    {
        await SonarrService.CreateAndAwaitCommand(SonarrCommands.RescanSeries);

        var todayUtc = DateTime.Now.ToUniversalTime();

        await foreach (var series in SonarrService.GetSeries().Where(i => i.Tags.Contains(tagId)))
        await foreach (var episode in SonarrService.GetEpisodes(series.Id)
                           .Where(o => !string.IsNullOrEmpty(o.AirDate)
                                       && o.AirDateUtc <= todayUtc
                                       && o is { EpisodeFileId: 0, SeasonNumber: > 0, AbsoluteEpisodeNumber: > 0, Monitored: true }))
            yield return episode with { Series = series };
    }

    public async Task<int> CreateRequestsToDaemonApi()
    {
        var wasariDaemonApi = ServiceProvider.GetRequiredService<IWasariDaemonApi>();
        var createdRequests = 0;

        await foreach (var missingEpisodesBySeries in GetMissingEpisodes(SonarrOptions.TagId).GroupBy(i => i.Series))
        {
            var betaUrl = await GetCrunchyrollUrl(CrunchyrollApi, missingEpisodesBySeries);
            
            await foreach (var missingEpisode in missingEpisodesBySeries)
            {
                var downloadPath = string.IsNullOrEmpty(missingEpisode.Series.Path) ? null : missingEpisode.Series.Path[(missingEpisode.Series.Path.IndexOf('/', 1) + 1)..];
                createdRequests += await wasariDaemonApi.Download(new DownloadRequest(new Uri(betaUrl), missingEpisode.EpisodeNumber, missingEpisode.SeasonNumber, downloadPath)).ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        Logger.LogInformation("Created request for {Episode}", missingEpisode);
                        return 1;
                    }
                
                    Logger.LogError(t.Exception, "Failed to create request for {Episode}", missingEpisode);
                    return 0;
                });
            }
        }

        return createdRequests;
    }

    public Task<int> CreateJobs()
    {
        return CreateJobs(GetMissingEpisodes(SonarrOptions.TagId));
    }

    public async Task ClearNotFoundEpisodes()
    {
        await foreach (var jobWithPods in KubernetesService.GetJobsPods())
        {
            if (jobWithPods.Pods.All(o => o.Status.ContainerStatuses.SingleOrDefault()?.State?.Terminated is { ExitCode: 404 }))
            {
                await KubernetesService.DeleteJob(jobWithPods.Job.Name());
            }
        }
    }

    private async Task<int> CreateJobs(IAsyncEnumerable<Episode> missingEpisodes)
    {
        var episodesGroupedBySeries = await missingEpisodes.GroupBy(i => i.Series).ToArrayAsync();

        var processInParallel = await episodesGroupedBySeries.ToAsyncProcessorBuilder()
            .SelectAsync(e => GetInformationAndCreateJob(CrunchyrollApi, e))
            .ProcessInParallel(SonariOptions.SonarrLevelOfParallelism)
            .GetResultsAsync();

        return processInParallel.Sum();
    }

    private async Task<int> GetInformationAndCreateJob(CrunchyrollApiService crunchyrollDiscoverApi, IAsyncGrouping<Series, Episode> missingEpisodesBySeries)
    {
        var betaUrl = await GetCrunchyrollUrl(crunchyrollDiscoverApi, missingEpisodesBySeries);
        var existingJobs = await KubernetesService.ListJobs()
            .Select(i => i.Name())
            .ToHashSetAsync();

        return await missingEpisodesBySeries.SelectAwait(async missingEpisode => await CreateJob(missingEpisode, existingJobs, betaUrl)).SumAsync();
    }

    private record CrunchyEpisode(string? Id, string? SlugTitle);

    private static async Task<string> GetCrunchyrollUrl(CrunchyrollApiService crunchyrollApiService, IAsyncGrouping<Series, Episode> missingEpisodesBySeries)
    {
        var episodes = await crunchyrollApiService
            .SearchSeries(missingEpisodesBySeries.Key.TitleSlug)
            .GroupBy(i => i.Id)
            .SelectAwait(i => i.FirstAsync())
            .ToArrayAsync();
        
        var crunchySeries = episodes
            .Where(i => i.SlugTitle == missingEpisodesBySeries.Key.TitleSlug)
            .GroupBy(i => new CrunchyEpisode(i.Id, i.SlugTitle))
            .Select(i => i.Key)
            .SingleOrDefault();

        if (crunchySeries == null && episodes.Length != 1)
            throw new InvalidOperationException($"Failed to get url for series: {missingEpisodesBySeries.Key.Title} ({missingEpisodesBySeries.Key.TitleSlug})");

        crunchySeries ??= new CrunchyEpisode(episodes.Single().Id, episodes.Single().SlugTitle);
        return $"https://beta.crunchyroll.com/series/{crunchySeries.Id}/{crunchySeries.SlugTitle}";
    }

    private async Task<int> CreateJob(Episode missingEpisode, IReadOnlySet<string> existingJobs, string betaUrl)
    {
        var jobName = $"{missingEpisode.SeriesId}-s{missingEpisode.SeasonNumber:00}-e{missingEpisode.EpisodeNumber:000}";

        if (existingJobs.Contains(jobName))
        {
            Logger.LogWarning("Skipping job creation, due to existing job. {@Name}", jobName);
            return 0;
        }

        var downloadPath = string.IsNullOrEmpty(missingEpisode.Series.Path) ? null : missingEpisode.Series.Path[(missingEpisode.Series.Path.IndexOf('/', 1) + 1)..];

        var job = await KubernetesService.CreateJob(jobName, betaUrl, missingEpisode.EpisodeNumber, missingEpisode.SeasonNumber, null,
                SonarrOptions._4KTagId.HasValue && missingEpisode.Series.Tags.Contains(SonarrOptions._4KTagId.Value)
                    ? Anime4KArguments
                    : Array.Empty<string>(), downloadPath)
            .ContinueWith(t =>
            {
                if (t.IsCompletedSuccessfully)
                    return t.Result;

                Logger.LogError(t.Exception, "Failed to create job");
                return null;
            });

        if (job != null)
        {
            Logger.LogInformation("Job created for episode {@Episode} {@JobName}", missingEpisode, job.Name());
            return 1;
        }

        return 0;
    }
}