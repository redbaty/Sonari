using System.Text.Json.Serialization;

namespace Sonari.Crunchyroll;

public record ApiSearchResult
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }
    
    [JsonPropertyName("slug_title")]
    public string? SlugTitle { get; init; }
    
    [JsonPropertyName("id")]
    public string? Id { get; init; }
    
    [JsonPropertyName("__links__")]
    public Dictionary<string, ApiLink> Links { get; init; }
}

public record ApiLink
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }
}