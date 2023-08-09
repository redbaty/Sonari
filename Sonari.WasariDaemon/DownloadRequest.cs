namespace Sonari.WasariDaemon;

public record DownloadRequest(Uri Url, int EpisodeNumber, int SeasonNumber, string? SeriesNameOverride);