using System.Text.Json.Serialization;

namespace CrossOff.LobbyDodger;

public sealed class BlacklistDocument
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("entries")]
    public List<BlacklistEntry> Entries { get; init; } = [];
}

public sealed class BlacklistEntry
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; init; } = string.Empty;

    [JsonPropertyName("aliases")]
    public List<string> Aliases { get; init; } = [];

    [JsonPropertyName("reason")]
    public string Reason { get; init; } = string.Empty;

    [JsonPropertyName("evidenceUrl")]
    public string EvidenceUrl { get; init; } = string.Empty;

    [JsonPropertyName("addedAt")]
    public DateTimeOffset AddedAt { get; init; }

    [JsonPropertyName("active")]
    public bool Active { get; init; }
}

public sealed record NameMatch(BlacklistEntry Entry, string Alias, string OcrLine);

public sealed record OcrScan(string Text, float MeanConfidence);
