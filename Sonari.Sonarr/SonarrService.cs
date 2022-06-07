using System.Net.Http.Json;
using Sonari.Sonarr.Models;

namespace Sonari.Sonarr;

public class SonarrOptions
{
    public Uri BaseAddress { get; set; }
    
    public string Key { get; set; }
}

public class SonarrService
{
    public SonarrService(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    private HttpClient HttpClient { get; }

    public async IAsyncEnumerable<Tag> GetTags()
    {
        var tagsArray = await HttpClient.GetFromJsonAsync<Tag[]>("tag");

        if (tagsArray != null)
            foreach (var tag in tagsArray)
                yield return tag;
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(int seriesId)
    {
        var episodesArray = await HttpClient.GetFromJsonAsync<Episode[]>($"episode?seriesId={seriesId}");

        if (episodesArray != null)
            foreach (var episode in episodesArray)
                yield return episode;
    }

    public async IAsyncEnumerable<Series> GetSeries()
    {
        var seriesArray = await HttpClient.GetFromJsonAsync<Series[]>("series");

        if (seriesArray != null)
            foreach (var series in seriesArray)
                yield return series;
    }
}