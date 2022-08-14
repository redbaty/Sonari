using k8s.Models;

namespace Sonari.Kubernetes;

public record JobWithPods
{
    public V1Job Job { get; init; } = null!;
    
    public V1Pod[] Pods { get; init; } = null!;
}