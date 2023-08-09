using System.Diagnostics.CodeAnalysis;
using CliFx.Attributes;
using k8s.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sonari.Converters;
using Sonari.Kubernetes;
using Sonari.Sonarr;
using Sonari.WasariDaemon;

namespace Sonari.Commands;

[SuppressMessage("CliFx", "CliFx_OptionMustBeInsideCommand:Options must be defined inside commands")]
public class DefaultCommandBase
{
    public DefaultCommandBase(IOptions<KubernetesOptions> kubernetesOptions, IOptions<SonarrOptions> sonarrOptions, IServiceProvider serviceProvider)
    {
        KubernetesOptions = kubernetesOptions;
        SonarrOptions = sonarrOptions;
        ServiceProvider = serviceProvider;
        WasariDaemonApi = serviceProvider.GetService<IWasariDaemonApi>();
    }

    protected IOptions<KubernetesOptions> KubernetesOptions { get; }

    protected IOptions<SonarrOptions> SonarrOptions { get; }

    [CommandOption("s-base-url", EnvironmentVariable = "SONARR_API_URL", IsRequired = true)]
    public Uri SonarrBaseUrl { get; set; } = null!;

    [CommandOption("s-api-key", EnvironmentVariable = "SONARR_API_KEY", IsRequired = true)]
    public string SonarrApiKey { get; set; } = null!;

    [CommandOption("s-tag-id", EnvironmentVariable = "SONARR_WASARI_TAG", IsRequired = true)]
    public string WasariTagName { get; set; } = null!;

    [CommandOption("tag-4k-id", EnvironmentVariable = "SONARR_4K_TAG")]
    public string? _4KTagName { get; set; }

    [CommandOption("nfs-host", 'h', EnvironmentVariable = "NFS_HOST")]
    public string? NfsHost { get; set; }

    [CommandOption("nfs-path", 'p', EnvironmentVariable = "NFS_PATH")]
    public string? NfsPath { get; set; }

    [CommandOption("job-image", 'i', EnvironmentVariable = "JOB_IMAGE")]
    public string JobImage { get; set; } = "redbaty/wasari:latest";

    [CommandOption("l-cpu", Converter = typeof(ResourceQuantityConverter), EnvironmentVariable = "CPU_LIMITS")]
    public ResourceQuantity? CpuLimits { get; set; }

    [CommandOption("l-memory", Converter = typeof(ResourceQuantityConverter), EnvironmentVariable = "MEM_LIMITS")]
    public ResourceQuantity? MemoryLimits { get; set; }

    [CommandOption("nv-memory", Converter = typeof(ResourceQuantityConverter), EnvironmentVariable = "NVIDIA_GPU_LIMITS")]
    public ResourceQuantity? NvidiaGpuLimits { get; set; }

    [CommandOption("job-ttl", EnvironmentVariable = "JOB_TTL")]
    public int? JobTtl { get; set; } = (int?)TimeSpan.FromMinutes(10).TotalSeconds;

    [CommandOption("namespace", 'n', EnvironmentVariable = "NAMESPACE")]
    public string? Namespace { get; set; }
    
    protected IWasariDaemonApi? WasariDaemonApi { get; }

    protected IServiceProvider ServiceProvider { get; }

    protected async Task PopulateOptions()
    {
        SonarrOptions.Value.BaseAddress = SonarrBaseUrl;
        SonarrOptions.Value.Key = SonarrApiKey;

        KubernetesOptions.Value.NfsHost = NfsHost;
        KubernetesOptions.Value.NfsPath = NfsPath;
        KubernetesOptions.Value.JobImage = JobImage;
        KubernetesOptions.Value.JobCpuLimit = CpuLimits;
        KubernetesOptions.Value.JobMemoryLimit = MemoryLimits;
        KubernetesOptions.Value.JobTtl = JobTtl;
        KubernetesOptions.Value.NvidiaGpuLimit = NvidiaGpuLimits;

        var sonarrService = ServiceProvider.GetRequiredService<SonarrService>();
        var tagsLookup = await sonarrService.GetTags()
            .Where(i => i.Label == WasariTagName || !string.IsNullOrEmpty(_4KTagName) && i.Label == _4KTagName)
            .ToLookupAsync(i => i.Label);
        
        SonarrOptions.Value.TagId = tagsLookup[WasariTagName].Single().Id;
        SonarrOptions.Value._4KTagId = !string.IsNullOrEmpty(_4KTagName) ? tagsLookup[_4KTagName].Single().Id : null;
    }
}