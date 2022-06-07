using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record Tag(
    [property: JsonPropertyName("label")]
    string Label,
    [property: JsonPropertyName("id")]
    int Id
);