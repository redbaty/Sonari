using System.Text.Json.Serialization;

namespace Sonari.Crunchyroll
{
    public class ApiSeason
    {
        [JsonPropertyName("series_id")]
        public string SeriesId { get; init; } = null!;
        
        [JsonPropertyName("slug_title")]
        public string SlugTitle { get; init; } = null!;
    }
}