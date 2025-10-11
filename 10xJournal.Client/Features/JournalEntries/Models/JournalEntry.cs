using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.JournalEntries.Models;

/// <summary>
/// Represents a journal entry from the database.
/// Maps to the 'journal_entries' table in Supabase.
/// </summary>
public class JournalEntry
{
    /// <summary>
    /// Unique identifier for the journal entry.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The content/body of the journal entry.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the user who created this entry.
    /// </summary>
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Timestamp when the entry was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entry was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
