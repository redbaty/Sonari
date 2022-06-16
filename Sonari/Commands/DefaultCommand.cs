using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sonari.App;
using Sonari.Converters;
using Sonari.Crunchyroll;
using Sonari.Kubernetes;
using Sonari.Sonarr;

namespace Sonari.Commands;

[Command]
public class DefaultCommand : ICommand
{
    public DefaultCommand(IServiceProvider serviceProvider, IOptions<SonarrOptions> sonarrOptions, IOptions<KubernetesOptions> kubernetesOptions)
    {
        ServiceProvider = serviceProvider;
        SonarrOptions = sonarrOptions;
        KubernetesOptions = kubernetesOptions;
    }

    private IServiceProvider ServiceProvider { get; }

    private IOptions<SonarrOptions> SonarrOptions { get; }
    
    private IOptions<KubernetesOptions> KubernetesOptions { get; }

    [CommandOption("s-base-url", EnvironmentVariable = "SONARR_API_URL", IsRequired = true)]
    public Uri SonarrBaseUrl { get; set; } = null!;

    [CommandOption("s-api-key", EnvironmentVariable = "SONARR_API_KEY", IsRequired = true)]
    public string SonarrApiKey { get; set; } = null!;
    
    [CommandOption("s-tag-id", EnvironmentVariable = "SONARR_TAGID", IsRequired = true)]
    public int TagId { get; set; }
    
    [CommandOption("nfs-host", 'h', EnvironmentVariable = "NFS_HOST")]
    public string NfsHost { get; set; }
    
    [CommandOption("nfs-path", 'p', EnvironmentVariable = "NFS_PATH")]
    public string NfsPath { get; set; }

    [CommandOption("job-image", 'i', EnvironmentVariable = "JOB_IMAGE")]
    public string JobImage { get; set; } = "redbaty/wasari:latest-kb";
    
    [CommandOption("l-cpu", Converter = typeof(ResourceQuantityConverter), EnvironmentVariable = "CPU_LIMITS")]
    public ResourceQuantity? CpuLimits { get; set; }
    
    [CommandOption("l-memory", Converter = typeof(ResourceQuantityConverter), EnvironmentVariable = "MEM_LIMITS")]
    public ResourceQuantity? MemoryLimits { get; set; }

    [CommandOption("job-ttl", EnvironmentVariable = "JOB_TTL")]
    public int? JobTtl { get; set; } = (int?)TimeSpan.FromMinutes(10).TotalSeconds;
    
    [CommandOption("namespace", 'n', EnvironmentVariable = "NAMESPACE")]
    public string Namespace { get; set; }
    
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

    private void PopulateOptions()
    {
        SonarrOptions.Value.BaseAddress = SonarrBaseUrl;
        SonarrOptions.Value.Key = SonarrApiKey;
        SonarrOptions.Value.TagId = TagId;
        KubernetesOptions.Value.NfsHost = NfsHost;
        KubernetesOptions.Value.NfsPath = NfsPath;
        KubernetesOptions.Value.JobImage = JobImage;
        KubernetesOptions.Value.JobCpuLimit = CpuLimits;
        KubernetesOptions.Value.JobMemoryLimit = MemoryLimits;
        KubernetesOptions.Value.JobTtl = JobTtl;
    }
}