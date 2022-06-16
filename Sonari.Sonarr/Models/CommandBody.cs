using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record CommandBody(
    [property: JsonPropertyName("sendUpdatesToClient")] bool SendUpdatesToClient,
    [property: JsonPropertyName("updateScheduledTask")] bool UpdateScheduledTask,
    [property: JsonPropertyName("completionMessage")] string CompletionMessage,
    [property: JsonPropertyName("requiresDiskAccess")] bool RequiresDiskAccess,
    [property: JsonPropertyName("isExclusive")] bool IsExclusive,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("trigger")] string Trigger,
    [property: JsonPropertyName("suppressMessages")] bool SuppressMessages
);