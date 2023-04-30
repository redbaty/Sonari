﻿using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sonari.Crunchyroll;
using Sonari.Kubernetes;
using Sonari.Sonarr;
using Sonari.Sonarr.Models;
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
        CrunchyrollApiServiceFactory crunchyrollApiServiceFactory,
        KubernetesService kubernetesService,
        ILogger<SonariService> logger,
        IOptions<SonarrOptions> sonarrOptions,
        IOptions<SonariOptions> sonariOptions)
    {
        SonarrService = sonarrService;
        CrunchyrollApiServiceFactory = crunchyrollApiServiceFactory;
        KubernetesService = kubernetesService;
        Logger = logger;
        SonarrOptions = sonarrOptions.Value;
        SonariOptions = sonariOptions.Value;
    }

    private SonarrService SonarrService { get; }

    private CrunchyrollApiServiceFactory CrunchyrollApiServiceFactory { get; }

    private KubernetesService KubernetesService { get; }

    private ILogger<SonariService> Logger { get; }

    private SonarrOptions SonarrOptions { get; }

    private SonariOptions SonariOptions { get; }

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
        var crunchyrollApiService = CrunchyrollApiServiceFactory.GetService();
        var episodesGroupedBySeries = await missingEpisodes.GroupBy(i => i.Series).ToArrayAsync();

        var processInParallel = await episodesGroupedBySeries.ToAsyncProcessorBuilder()
            .SelectAsync(e => GetInformationAndCreateJob(crunchyrollApiService, e))
            .ProcessInParallel(SonariOptions.SonarrLevelOfParallelism)
            .GetResultsAsync();

        return processInParallel.Sum();
    }

    private async Task<int> GetInformationAndCreateJob(CrunchyrollApiService crunchyrollApiService, IAsyncGrouping<Series, Episode> missingEpisodesBySeries)
    {
        var crunchySeries = await crunchyrollApiService
            .SearchSeries(missingEpisodesBySeries.Key.TitleSlug)
            .Where(i => i.SlugTitle == missingEpisodesBySeries.Key.TitleSlug)
            .GroupBy(i => new { i.Id, i.SlugTitle })
            .Select(i => i.Key)
            .SingleAsync();

        if (crunchySeries == null)
            throw new InvalidOperationException($"Failed to get url for series: {missingEpisodesBySeries.Key.Title}");

        var betaUrl = $"https://beta.crunchyroll.com/series/{crunchySeries.Id}/{crunchySeries.SlugTitle}";
        var existingJobs = await KubernetesService.ListJobs()
            .Select(i => i.Name())
            .ToHashSetAsync();

        return await missingEpisodesBySeries.SelectAwait(async missingEpisode => await CreateJob(missingEpisode, existingJobs, betaUrl)).SumAsync();
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

        var job = await KubernetesService.CreateJob(jobName, betaUrl, missingEpisode.EpisodeNumber, missingEpisode.SeasonNumber, CrunchyrollApiServiceFactory.Token,
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