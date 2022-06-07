using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record Image(
    [property: JsonPropertyName("coverType")]
    string CoverType,
    [property: JsonPropertyName("url")]
    string Url,
    [property: JsonPropertyName("remoteUrl")]
    string RemoteUrl
);