using System.Text.Json.Serialization;

namespace Sonari.Crunchyroll;

public class ApiSeries
{
    [JsonPropertyName("slug_title")]
    public string? SlugTitle { get; init; }
    
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}