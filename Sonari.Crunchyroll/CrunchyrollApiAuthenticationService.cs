using System.Text.Json;
using JsonExtensions.Reading;

namespace Sonari.Crunchyroll
{
    internal class CrunchyrollApiAuthenticationService
    {
        public CrunchyrollApiAuthenticationService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        private HttpClient HttpClient { get; }

        private string? AnonymousAccessToken { get; set; }

        public async Task<string> GetAccessToken()
        {
            AnonymousAccessToken ??= await CreateAccessToken();
            return AnonymousAccessToken;
        }

        private async Task<string> CreateAccessToken()
        {
            using var formUrlEncodedContent = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("grant_type", "client_id") });
            using var authResponse = await HttpClient.PostAsync("auth/v1/token", formUrlEncodedContent);
            authResponse.EnsureSuccessStatusCode();
            
            await using var responseStream = await authResponse.Content.ReadAsStreamAsync();
            var jsonDocument = await JsonDocument.ParseAsync(responseStream);
            return jsonDocument.RootElement.GetProperty("access_token").GetNonNullString();
        }
    }
}