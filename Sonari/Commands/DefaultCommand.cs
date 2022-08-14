using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sonari.App;
using Sonari.Crunchyroll;
using Sonari.Kubernetes;
using Sonari.Sonarr;

namespace Sonari.Commands;

[Command]
public class DefaultCommand : DefaultCommandBase, ICommand
{
    public DefaultCommand(IServiceProvider serviceProvider, IOptions<SonarrOptions> sonarrOptions, IOptions<KubernetesOptions> kubernetesOptions) : base(kubernetesOptions, sonarrOptions)
    {
        ServiceProvider = serviceProvider;
    }

    [CommandOption("c:username", EnvironmentVariable = "CRUNCHY_USERNAME")]
    public string? CrunchyUsername { get; set; }
    
    [CommandOption("c:password", EnvironmentVariable = "CRUNCHY_PASSWORD")]
    public string? CrunchyPassword { get; set; }
    
    private IServiceProvider ServiceProvider { get; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        PopulateOptions();

        if (!string.IsNullOrEmpty(Namespace))
            KubernetesOptions.Value.Namespace = Namespace;
        
        await using var serviceScope = ServiceProvider.CreateAsyncScope();
        var sonariService = serviceScope.ServiceProvider.GetRequiredService<SonariService>();
        var crunchyrollFactory = serviceScope.ServiceProvider.GetRequiredService<CrunchyrollApiServiceFactory>();

        if (!string.IsNullOrEmpty(CrunchyUsername) || !string.IsNullOrEmpty(CrunchyPassword))
        {
            if (string.IsNullOrEmpty(CrunchyUsername))
                throw new InvalidOperationException("Failed to authenticate");

            if (string.IsNullOrEmpty(CrunchyPassword))
                throw new InvalidOperationException("Failed to authenticate");

            await crunchyrollFactory.CreateAuthenticatedService(CrunchyUsername, CrunchyPassword);
        }
        else
        {
            await crunchyrollFactory.CreateUnauthenticatedService();
        }

        await sonariService.CreateJobs();
    }
}