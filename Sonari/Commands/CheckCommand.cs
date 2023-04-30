using CliFx;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Microsoft.Extensions.Options;
using Sonari.Kubernetes;
using Sonari.Sonarr;

namespace Sonari.Commands;

[Command("check")]
public class CheckCommand : DefaultCommandBase, ICommand
{
    private KubernetesService KubernetesService { get; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        await PopulateOptions();
        await KubernetesService.ListJobs().ToArrayAsync();
    }


    public CheckCommand(IOptions<KubernetesOptions> kubernetesOptions, IOptions<SonarrOptions> sonarrOptions, IServiceProvider serviceProvider, KubernetesService kubernetesService) : base(kubernetesOptions, sonarrOptions, serviceProvider)
    {
        KubernetesService = kubernetesService;
    }
}