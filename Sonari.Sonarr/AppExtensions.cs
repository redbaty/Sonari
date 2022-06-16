using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Sonari.Sonarr;

public static class AppExtensions
{
    public static void AddSonarrServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient<SonarrService>((provider, c) =>
        {
            var options = provider.GetService<IOptions<SonarrOptions>>();

            if (options != null)
            {
                if (options.Value.BaseAddress == null)
                    throw new InvalidOperationException("No URL was provided to Sonarr Service");

                c.BaseAddress = options.Value.BaseAddress;
                c.DefaultRequestHeaders.Add("X-Api-Key", options.Value.Key);
            }
        });
    }
}