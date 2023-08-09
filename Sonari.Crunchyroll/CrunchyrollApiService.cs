using System.Net.Http.Json;
using System.Text.Json;
using Flurl;
using JsonExtensions.Http;
using JsonExtensions.Reading;

namespace Sonari.Crunchyroll
{
    public class CrunchyrollApiService
    {
        public CrunchyrollApiService(HttpClient httpClient)
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

            return new ApiSignature(
                root.GetPropertyByPath("cms.signature").GetNonNullString(),
                root.GetPropertyByPath("cms.policy").GetNonNullString(),
                root.GetPropertyByPath("cms.bucket").GetNonNullString(),
                root.GetPropertyByPath("cms.key_pair_id").GetNonNullString()
            );
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

        public async Task<ApiSeason?> GetSeason(string seasonId)
        {
            var url = await BuildUrlFromSignature($"seasons/{seasonId}");
            return await HttpClient.GetFromJsonAsync<ApiSeason>(url);
        }

        public async IAsyncEnumerable<ApiSearchResult> SearchSeries(string searchTerm)
        {
            var url = "content/v1/search".SetQueryParam("q", searchTerm)
                .SetQueryParam("locale", "en-US");

            var responseJson = await HttpClient.GetJsonAsync(url);

            foreach (var itemsRoot in responseJson.GetProperty("items").EnumerateArray())
            {
                foreach (var jsonElement in itemsRoot.GetProperty("items").EnumerateArray())
                {
                    var apiSeries = jsonElement.Deserialize<ApiSearchResult>();

                    if (apiSeries != null)
                    {
                        if (apiSeries.Type == "season")
                        {
                            var seasonLink = apiSeries.Links.GetValueOrDefault("resource")?.Href;
                            var seriesId = seasonLink?.Split('/')?.LastOrDefault();

                            if (!string.IsNullOrEmpty(seriesId))
                            {
                                var season = await GetSeason(seriesId);

                                if (season != null)
                                    yield return new ApiSearchResult
                                    {
                                        Id = season.SeriesId,
                                        SlugTitle = season.SlugTitle,
                                        Type = "series"
                                    };
                            }
                        }
                        else if (apiSeries.Type == "series") yield return apiSeries;
                    }
                }
            }
        }
    }
}