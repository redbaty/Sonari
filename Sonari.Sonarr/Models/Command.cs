using System.Text.Json.Serialization;

namespace Sonari.Sonarr.Models;

public record Command(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("body")] CommandBody Body,
    [property: JsonPropertyName("priority")] string Priority,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("queued")] DateTime Queued,
    [property: JsonPropertyName("started")] DateTime Started,
    [property: JsonPropertyName("trigger")] string Trigger,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("manual")] bool Manual,
    [property: JsonPropertyName("startedOn")] DateTime StartedOn,
    [property: JsonPropertyName("stateChangeTime")] DateTime StateChangeTime,
    [property: JsonPropertyName("sendUpdatesToClient")] bool SendUpdatesToClient,
    [property: JsonPropertyName("updateScheduledTask")] bool UpdateScheduledTask,
    [property: JsonPropertyName("id")] int Id
);

