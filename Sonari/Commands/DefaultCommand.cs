using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sonari.App;
using Sonari.Kubernetes;
using Sonari.Sonarr;

namespace Sonari.Commands;

[Command]
public class DefaultCommand : ICommand
{
    private IServiceProvider ServiceProvider { get; }

    public DefaultCommand(IServiceProvider serviceProvider, IOptions<SonarrOptions> options)
    {
        ServiceProvider = serviceProvider;
        Options = options;
    }

    private IOptions<SonarrOptions> Options { get; }
    
    [CommandOption("s-base-url", EnvironmentVariable = "SONARR_API_URL")]
    public Uri SonarrBaseUrl { get; set; }
    
    [CommandOption("s-api-key", EnvironmentVariable = "SONARR_API_KEY")]
    public string SonarrApiKey { get; set; }
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        Options.Value.BaseAddress = SonarrBaseUrl;
        Options.Value.Key = SonarrApiKey;

        await using var serviceScope = ServiceProvider.CreateAsyncScope();
        var sonariService = serviceScope.ServiceProvider.GetRequiredService<SonariService>();
        var missingEpisodes = await sonariService.GetMissingEpisodes(3).ToArrayAsync();

        var kubernetesService = serviceScope.ServiceProvider.GetRequiredService<KubernetesService>();

        foreach (var missingEpisode in missingEpisodes)
        {
            await kubernetesService.CreateJob($"https://crunchyroll.com/{missingEpisode.Series.TitleSlug}", missingEpisode.EpisodeNumber, missingEpisode.SeasonNumber, missingEpisode.SeriesId);
        }

        var y = 0;
    }
}