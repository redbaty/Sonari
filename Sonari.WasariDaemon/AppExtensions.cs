using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace Sonari.WasariDaemon;

public static class AppExtensions
{
    public static void AddWasariDaemonServices(this IServiceCollection serviceCollection, Uri apiUrl)
    {
        serviceCollection.AddRefitClient<IWasariDaemonApi>()
            .ConfigureHttpClient(e => { e.BaseAddress = apiUrl; });
    }
}