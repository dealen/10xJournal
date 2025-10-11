using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.JournalEntries.Models;

/// <summary>
/// Request model for creating a new journal entry.
/// Maps to the 'Insert' type for journal_entries table.
/// </summary>
public class CreateJournalEntryRequest
{
    /// <summary>
    /// The content/body of the journal entry.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user creating this entry.
    /// </summary>
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional: Timestamp when the entry was created.
    /// If not provided, Supabase will use the default (NOW()).
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Optional: Unique identifier for the journal entry.
    /// If not provided, Supabase will generate a UUID.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// Optional: Timestamp when the entry was last updated.
    /// If not provided, Supabase will use the default (NOW()).
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
