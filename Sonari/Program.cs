using CliFx;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Sonari.App;
using Sonari.Commands;
using Sonari.Converters;
using Sonari.Crunchyroll;
using Sonari.Kubernetes;
using Sonari.Sonarr;

namespace Sonari;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<DefaultCommand>();
        serviceCollection.AddScoped<CheckCommand>();
        serviceCollection.AddSonarrServices();
        serviceCollection.AddSonariServices();
        serviceCollection.AddKubernetesServices();
        serviceCollection.AddCrunchyrollApiServices();
        serviceCollection.AddLogging(c => c.AddSerilog());
        serviceCollection.AddSingleton<ResourceQuantityConverter>();
        await using var serviceProvider = serviceCollection.BuildServiceProvider();
        
        return await new CliApplicationBuilder()
            .AddCommand<DefaultCommand>()
            .AddCommand<CheckCommand>()
            .UseTypeActivator(serviceProvider)
            .Build()
            .RunAsync(args);
    }
}