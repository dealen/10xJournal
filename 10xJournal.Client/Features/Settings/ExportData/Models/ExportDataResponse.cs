using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.Settings.ExportData.Models;

/// <summary>
/// Data export response model.
/// Maps the JSON structure returned by /rpc/export_journal_entries.
/// </summary>
public class ExportDataResponse
{
    /// <summary>
    /// Number of exported journal entries.
    /// </summary>
    [JsonPropertyName("total_entries")]
    public int TotalEntries { get; set; }

    /// <summary>
    /// Date and time of export.
    /// </summary>
    [JsonPropertyName("exported_at")]
    public DateTimeOffset ExportedAt { get; set; }

    /// <summary>
    /// List of all user journal entries.
    /// </summary>
    [JsonPropertyName("entries")]
    public List<ExportedEntry> Entries { get; set; } = new();
}

/// <summary>
/// Single journal entry export model.
/// </summary>
public class ExportedEntry
{
    /// <summary>
    /// Unique ID of the entry.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Date and time of entry creation.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Content of the journal entry.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
