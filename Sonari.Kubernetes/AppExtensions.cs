﻿using k8s;
using Microsoft.Extensions.DependencyInjection;

namespace Sonari.Kubernetes;

public static class AppExtensions
{
    public static void AddKubernetesServices(this IServiceCollection serviceCollection)
    {
        var clientConfiguration = KubernetesClientConfiguration.IsInCluster() ? KubernetesClientConfiguration.InClusterConfig() : KubernetesClientConfiguration.BuildDefaultConfig();
        serviceCollection.AddScoped(_ => new k8s.Kubernetes(clientConfiguration));
        serviceCollection.AddScoped<KubernetesService>();
    }
}