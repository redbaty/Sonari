using Microsoft.Extensions.DependencyInjection;

namespace Sonari.Crunchyroll
{
    public static class AppExtensions
    {
        public static void AddCrunchyrollApiServices(this IServiceCollection serviceCollection)
        {
            var crunchyBaseAddress = new Uri("https://beta-api.crunchyroll.com/");
            
            serviceCollection.AddMemoryCache();
            serviceCollection.AddHttpClient<CrunchyrollAuthenticationHandler>(c =>
            {
                c.BaseAddress = crunchyBaseAddress;
                c.DefaultRequestHeaders.Add("Authorization",
                    "Basic a3ZvcGlzdXZ6Yy0teG96Y21kMXk6R21JSTExenVPVnRnTjdlSWZrSlpibzVuLTRHTlZ0cU8=");
            });
            
            serviceCollection.AddHttpClient<CrunchyrollApiService>()
                .ConfigureHttpClient(c => c.BaseAddress = crunchyBaseAddress)
                .AddHttpMessageHandler<CrunchyrollAuthenticationHandler>();
        }
    }
}