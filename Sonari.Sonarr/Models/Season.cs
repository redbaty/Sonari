using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record Season(
    [property: JsonPropertyName("seasonNumber")]
    int SeasonNumber,
    [property: JsonPropertyName("monitored")]
    bool Monitored,
    [property: JsonPropertyName("statistics")]
    Statistics Statistics
);