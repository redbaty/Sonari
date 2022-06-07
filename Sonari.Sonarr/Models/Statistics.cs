using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record Statistics(
    [property: JsonPropertyName("episodeFileCount")]
    int EpisodeFileCount,
    [property: JsonPropertyName("episodeCount")]
    int EpisodeCount,
    [property: JsonPropertyName("totalEpisodeCount")]
    int TotalEpisodeCount,
    [property: JsonPropertyName("sizeOnDisk")]
    object SizeOnDisk,
    [property: JsonPropertyName("percentOfEpisodes")]
    double PercentOfEpisodes,
    [property: JsonPropertyName("previousAiring")]
    DateTime? PreviousAiring,
    [property: JsonPropertyName("nextAiring")]
    DateTime? NextAiring
);