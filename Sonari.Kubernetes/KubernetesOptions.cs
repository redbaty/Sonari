using k8s.Models;

namespace Sonari.Kubernetes;

public class KubernetesOptions
{
    public string? JobImage { get; set; }
    
    public string? NfsHost { get; set; }
    
    public string? NfsPath { get; set; }
    
    public ResourceQuantity? JobCpuLimit { get; set; }
    
    public ResourceQuantity? JobMemoryLimit { get; set; }

    public int? JobTtl { get; set; }

    public string Namespace { get; set; } = "default";
}