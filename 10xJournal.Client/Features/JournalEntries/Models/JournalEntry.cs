using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace _10xJournal.Client.Features.JournalEntries.Models;

/// <summary>
/// Represents a journal entry from the database.
/// Maps to the 'journal_entries' table in Supabase.
/// </summary>
[Table("journal_entries")]
public class JournalEntry : BaseModel
{
    /// <summary>
    /// Unique identifier for the journal entry.
    /// </summary>
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    /// <summary>
    /// The ID of the user who created this entry.
    /// </summary>
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The content/body of the journal entry.
    /// </summary>
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the entry was created.
    /// Uses DateTimeOffset to properly handle timezone-aware timestamps from Supabase.
    /// Database handles this value automatically via DEFAULT now(), so we ignore it on insert.
    /// </summary>
    [Column("created_at", ignoreOnInsert: true)]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the entry was last updated.
    /// Uses DateTimeOffset to properly handle timezone-aware timestamps from Supabase.
    /// Database handles this value automatically via DEFAULT now(), so we ignore it on insert and update.
    /// </summary>
    [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTimeOffset UpdatedAt { get; set; }
}

