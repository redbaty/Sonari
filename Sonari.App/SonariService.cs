using Sonari.Sonarr;
using Sonari.Sonarr.Models;

namespace Sonari.App;

public class SonariService
{
    public SonariService(SonarrService sonarrService)
    {
        SonarrService = sonarrService;
    }

    private SonarrService SonarrService { get; }

    public async IAsyncEnumerable<Episode> GetMissingEpisodes(int tagId)
    {
        var todayUtc = DateTime.Today.ToUniversalTime();
        
        await foreach (var series in SonarrService.GetSeries().Where(i => i.Tags.Contains(tagId)))
        {
            var episodes = await SonarrService.GetEpisodes(series.Id).ToArrayAsync();
            
            foreach (var episode in episodes.Where(o => o.AirDateUtc <= todayUtc && o.EpisodeFileId == 0))
            {
                yield return episode with { Series = series };
            }
        }
    }
}