using System.Diagnostics.CodeAnalysis;
using CliFx.Attributes;
using k8s.Models;
using Microsoft.Extensions.Options;
using Sonari.Converters;
using Sonari.Kubernetes;
using Sonari.Sonarr;

namespace Sonari.Commands;

[SuppressMessage("CliFx", "CliFx_OptionMustBeInsideCommand:Options must be defined inside commands")]
public class DefaultCommandBase
{
    public DefaultCommandBase(IOptions<KubernetesOptions> kubernetesOptions, IOptions<SonarrOptions> sonarrOptions)
    {
        KubernetesOptions = kubernetesOptions;
        SonarrOptions = sonarrOptions;
    }

    protected IOptions<KubernetesOptions> KubernetesOptions { get; }
    
    protected IOptions<SonarrOptions> SonarrOptions { get; }

    [CommandOption("s-base-url", EnvironmentVariable = "SONARR_API_URL", IsRequired = true)]
    public Uri SonarrBaseUrl { get; set; } = null!;

    [CommandOption("s-api-key", EnvironmentVariable = "SONARR_API_KEY", IsRequired = true)]
    public string SonarrApiKey { get; set; } = null!;

    [CommandOption("s-tag-id", EnvironmentVariable = "SONARR_TAGID", IsRequired = true)]
    public int TagId { get; set; }

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

    [CommandOption("job-ttl", EnvironmentVariable = "JOB_TTL")]
    public int? JobTtl { get; set; } = (int?)TimeSpan.FromMinutes(10).TotalSeconds;

    [CommandOption("namespace", 'n', EnvironmentVariable = "NAMESPACE")]
    public string? Namespace { get; set; }

    protected void PopulateOptions()
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