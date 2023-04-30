using System.Net.Http.Json;
using Sonari.Sonarr.Models;

namespace Sonari.Sonarr;

public class SonarrService
{
    public SonarrService(HttpClient httpClient)
    {
        HttpClient = httpClient;
    }

    private HttpClient HttpClient { get; }

    private async Task<Command?> CreateCommand(string name)
    {
        using var resposta = await HttpClient.PostAsJsonAsync("command", new { name });
        return await resposta.Content.ReadFromJsonAsync<Command>();
    }

    private Task<Command?> GetCommandStatus(int id) => HttpClient.GetFromJsonAsync<Command>($"command/{id}");

    public async Task CreateAndAwaitCommand(string name)
    {
        var command = await CreateCommand(name);

        if (command == null)
            throw new InvalidOperationException("Failed to create sonarr command");

        var status = await GetCommandStatus(command.Id);

        while (status is not { Status: "completed" })
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            status = await GetCommandStatus(command.Id);
        }
    }

    public async IAsyncEnumerable<Episode> GetEpisodes(int seriesId)
    {
        var episodesArray = await HttpClient.GetFromJsonAsync<Episode[]>($"episode?seriesId={seriesId}");

        if (episodesArray != null)
            foreach (var episode in episodesArray)
                yield return episode;
    }

    public async IAsyncEnumerable<Tag> GetTags()
    {
        var tagsArray = await HttpClient.GetFromJsonAsync<Tag[]>("tag");

        if (tagsArray != null)
            foreach (var tag in tagsArray)
                yield return tag;
    }

    public async IAsyncEnumerable<Series> GetSeries()
    {
        var seriesArray = await HttpClient.GetFromJsonAsync<Series[]>("series");

        if (seriesArray != null)
            foreach (var series in seriesArray)
                yield return series;
    }
}