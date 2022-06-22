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
        PopulateOptions();

        await KubernetesService.ListJobs().ToArrayAsync();
    }
    
    public CheckCommand(IOptions<KubernetesOptions> kubernetesOptions, IOptions<SonarrOptions> sonarrOptions, KubernetesService kubernetesService) : base(kubernetesOptions, sonarrOptions)
    {
        KubernetesService = kubernetesService;
    }
}