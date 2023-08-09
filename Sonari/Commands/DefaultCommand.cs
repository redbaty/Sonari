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
    [CommandOption("c:username", EnvironmentVariable = "CRUNCHY_USERNAME")]
    public string? CrunchyUsername { get; set; }

    [CommandOption("c:password", EnvironmentVariable = "CRUNCHY_PASSWORD")]
    public string? CrunchyPassword { get; set; }
    
    private IOptions<AuthenticationOptions> AuthenticationOptions => ServiceProvider.GetRequiredService<IOptions<AuthenticationOptions>>();
    
    public async ValueTask ExecuteAsync(IConsole console)
    {
        await PopulateOptions();

        if (!string.IsNullOrEmpty(Namespace))
            KubernetesOptions.Value.Namespace = Namespace;

        await using var serviceScope = ServiceProvider.CreateAsyncScope();
        var sonariService = serviceScope.ServiceProvider.GetRequiredService<SonariService>();
        
        AuthenticationOptions.Value.Username = CrunchyUsername;
        AuthenticationOptions.Value.Password = CrunchyPassword;

        if (WasariDaemonApi != null)
            await sonariService.CreateRequestsToDaemonApi();
        else
            await sonariService.CreateJobs();
    }

    public DefaultCommand(IOptions<KubernetesOptions> kubernetesOptions, IOptions<SonarrOptions> sonarrOptions, IServiceProvider serviceProvider) : base(kubernetesOptions, sonarrOptions, serviceProvider)
    {
    }
}