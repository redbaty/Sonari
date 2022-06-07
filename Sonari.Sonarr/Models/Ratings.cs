using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record Ratings(
    [property: JsonPropertyName("votes")]
    int Votes,
    [property: JsonPropertyName("value")]
    double Value
);