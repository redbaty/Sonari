using Microsoft.Extensions.DependencyInjection;

namespace Sonari.App;

public static class AppExtensions
{
    public static void AddSonariServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddScoped<SonariService>();
    }
}