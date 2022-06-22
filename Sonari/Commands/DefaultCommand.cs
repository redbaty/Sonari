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

    private IServiceProvider ServiceProvider { get; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        PopulateOptions();

        if (!string.IsNullOrEmpty(Namespace))
            KubernetesOptions.Value.Namespace = Namespace;
        
        await using var serviceScope = ServiceProvider.CreateAsyncScope();
        var sonariService = serviceScope.ServiceProvider.GetRequiredService<SonariService>();
        var crunchyrollFactory = serviceScope.ServiceProvider.GetRequiredService<CrunchyrollApiServiceFactory>();

        await crunchyrollFactory.CreateUnauthenticatedService();
        await sonariService.CreateJobs();
    }
}