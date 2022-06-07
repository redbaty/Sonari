using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record AlternateTitle(
    [property: JsonPropertyName("title")]
    string Title,
    [property: JsonPropertyName("sceneSeasonNumber")]
    int SceneSeasonNumber,
    [property: JsonPropertyName("seasonNumber")]
    int? SeasonNumber
);