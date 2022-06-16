using System.Text.Json;
using Flurl;
using JsonExtensions.Http;
using JsonExtensions.Reading;

namespace Sonari.Crunchyroll
{
    public class CrunchyrollApiService
    {
        internal CrunchyrollApiService(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        private HttpClient HttpClient { get; }

        private ApiSignature? ApiSignature { get; set; }

        private async Task<ApiSignature> GetApiSignature()
        {
            ApiSignature ??= await CreateApiSignature();
            return ApiSignature;
        }

        private async Task<ApiSignature> CreateApiSignature()
        {
            await using var responseStream = await HttpClient.GetStreamAsync("index/v2");
            using var jsonDocument = await JsonDocument.ParseAsync(responseStream);
            var root = jsonDocument.RootElement;

            return new ApiSignature
            {
                Bucket = root.GetPropertyByPath("cms.bucket").GetNonNullString(),
                Policy = root.GetPropertyByPath("cms.policy").GetNonNullString(),
                Signature = root.GetPropertyByPath("cms.signature").GetNonNullString(),
                KeyPairId = root.GetPropertyByPath("cms.key_pair_id").GetNonNullString(),
            };
        }

        private async Task<Url> BuildUrlFromSignature(string endpoint)
        {
            var signature = await GetApiSignature();
            return "cms/v2/"
                .AppendPathSegments(signature.Bucket, endpoint)
                .SetQueryParam("Policy", signature.Policy)
                .SetQueryParam("Signature", signature.Signature)
                .SetQueryParam("Key-Pair-Id", signature.KeyPairId)
                .SetQueryParam("locale", "en-US");
        }

        public async IAsyncEnumerable<ApiSeries> SearchSeries(string searchTerm)
        {
            var url = await BuildUrlFromSignature("search");
            url = url.SetQueryParam("q", searchTerm)
                .SetQueryParam("locale", "en-US");

            var responseJson = await HttpClient.GetJsonAsync(url);
            
            foreach (var jsonElement in responseJson.GetProperty("items").EnumerateArray())
            {
                var apiSeries = jsonElement.Deserialize<ApiSeries>();

                if (apiSeries != null && jsonElement.GetPropertyOrNull("type")?.GetString() == "series")
                    yield return apiSeries;
            }
        }
    }
}