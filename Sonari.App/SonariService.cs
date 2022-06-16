using k8s.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sonari.Crunchyroll;
using Sonari.Kubernetes;
using Sonari.Sonarr;
using Sonari.Sonarr.Models;

namespace Sonari.App;

public class SonariService
{
    public SonariService(SonarrService sonarrService, CrunchyrollApiServiceFactory crunchyrollApiServiceFactory, KubernetesService kubernetesService, ILogger<SonariService> logger, IOptions<SonarrOptions> options)
    {
        SonarrService = sonarrService;
        CrunchyrollApiServiceFactory = crunchyrollApiServiceFactory;
        KubernetesService = kubernetesService;
        Logger = logger;
        Options = options.Value;
    }

    private SonarrService SonarrService { get; }
    
    private CrunchyrollApiServiceFactory CrunchyrollApiServiceFactory { get; }

    private KubernetesService KubernetesService { get; }
    
    private ILogger<SonariService> Logger { get; }
    
    private SonarrOptions Options { get; }

    private async IAsyncEnumerable<Episode> GetMissingEpisodes(int tagId)
    {
        await SonarrService.CreateAndAwaitCommand(SonarrCommands.RescanSeries);
        
        var todayUtc = DateTime.Now.ToUniversalTime();
        
        await foreach (var series in SonarrService.GetSeries().Where(i => i.Tags.Contains(tagId)))
        {
            await foreach (var episode in SonarrService.GetEpisodes(series.Id).Where(o => !string.IsNullOrEmpty(o.AirDate) && o.AirDateUtc <= todayUtc && o.EpisodeFileId == 0))
                yield return episode with { Series = series };
        }
    }

    public Task<int> CreateJobs() => CreateJobs(GetMissingEpisodes(Options.TagId));

    private async Task<int> CreateJobs(IAsyncEnumerable<Episode> missingEpisodes)
    {
        var crunchyrollApiService = CrunchyrollApiServiceFactory.GetService();
        var createdJobs = 0;
        
        await foreach (var missingEpisodesBySeries in missingEpisodes.GroupBy(i => i.Series))
        {
            var crunchySeries = await crunchyrollApiService
                .SearchSeries(missingEpisodesBySeries.Key.TitleSlug)
                .SingleOrDefaultAsync(i => i.SlugTitle == missingEpisodesBySeries.Key.TitleSlug);

            if (crunchySeries == null)
                throw new InvalidOperationException($"Failed to get url for series: {missingEpisodesBySeries.Key.Title}");

            var betaUrl = $"https://beta.crunchyroll.com/series/{crunchySeries.Id}/{crunchySeries.SlugTitle}";
            var existingJobs = await KubernetesService.ListJobs()
                .Select(i => i.Name())
                .ToHashSetAsync();

            await foreach (var missingEpisode in missingEpisodesBySeries)
            {
                var jobName = $"job-sonari-{missingEpisode.SeriesId}-s{missingEpisode.SeasonNumber:00}-e{missingEpisode.EpisodeNumber:000}";

                if (existingJobs.Contains(jobName))
                {
                    Logger.LogWarning("Skipping job creation, due to existing job. {@Name}", jobName);
                    continue;
                }

                var job = await KubernetesService.CreateJob(jobName, betaUrl, missingEpisode.EpisodeNumber, missingEpisode.SeasonNumber, missingEpisode.SeriesId);
                Logger.LogInformation("Job created for episode {@Episode} {@JobName}", missingEpisode, job.Name());
                createdJobs++;
            }
        }

        return createdJobs;
    }
}