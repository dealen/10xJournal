using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.JournalEntries.Models;

/// <summary>
/// Request model for updating an existing journal entry.
/// Maps to the 'Update' type for journal_entries table.
/// All properties are optional since updates can be partial.
/// </summary>
public class UpdateJournalEntryRequest
{
    /// <summary>
    /// Optional: The updated content/body of the journal entry.
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// Optional: The ID of the journal entry to update.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid? Id { get; set; }

    /// <summary>
    /// Optional: The updated user ID.
    /// </summary>
    [JsonPropertyName("user_id")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Optional: The updated creation timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Optional: The updated timestamp.
    /// Typically set automatically by the database.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}
