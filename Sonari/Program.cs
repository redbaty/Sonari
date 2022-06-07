using CliFx;
using Microsoft.Extensions.DependencyInjection;
using Sonari.App;
using Sonari.Commands;
using Sonari.Kubernetes;
using Sonari.Sonarr;

namespace Sonari;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<DefaultCommand>();
        serviceCollection.AddSonarrServices();
        serviceCollection.AddSonariServices();
        serviceCollection.AddKubernetesServices();
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        
        return await new CliApplicationBuilder()
            .AddCommand<DefaultCommand>()
            .UseTypeActivator(serviceProvider.GetService)
            .Build()
            .RunAsync(args);
    }
}