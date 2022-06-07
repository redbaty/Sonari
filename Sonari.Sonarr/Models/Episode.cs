using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record Episode(
    [property: JsonPropertyName("seriesId")] int SeriesId,
    [property: JsonPropertyName("tvdbId")] int TvdbId,
    [property: JsonPropertyName("episodeFileId")] int EpisodeFileId,
    [property: JsonPropertyName("seasonNumber")] int SeasonNumber,
    [property: JsonPropertyName("episodeNumber")] int EpisodeNumber,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("airDate")] string AirDate,
    [property: JsonPropertyName("airDateUtc")] DateTime AirDateUtc,
    [property: JsonPropertyName("overview")] string Overview,
    [property: JsonPropertyName("hasFile")] bool HasFile,
    [property: JsonPropertyName("monitored")] bool Monitored,
    [property: JsonPropertyName("absoluteEpisodeNumber")] int AbsoluteEpisodeNumber,
    [property: JsonPropertyName("sceneAbsoluteEpisodeNumber")] int SceneAbsoluteEpisodeNumber,
    [property: JsonPropertyName("sceneEpisodeNumber")] int SceneEpisodeNumber,
    [property: JsonPropertyName("sceneSeasonNumber")] int SceneSeasonNumber,
    [property: JsonPropertyName("unverifiedSceneNumbering")] bool UnverifiedSceneNumbering,
    [property: JsonPropertyName("id")] int Id,
    [property: JsonIgnore] Series Series
);