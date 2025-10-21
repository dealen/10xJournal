using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.JournalEntries.EditEntry;

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
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the user who owns this entry.
    /// This is not actually updateable but included for validation.
    /// </summary>
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional: Flag to mark the entry as a favorite.
    /// </summary>
    [JsonPropertyName("is_favorite")]
    public bool? IsFavorite { get; set; }
}