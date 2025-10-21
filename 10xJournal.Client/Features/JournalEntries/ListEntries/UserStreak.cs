using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace _10xJournal.Client.Features.JournalEntries.ListEntries;

/// <summary>
/// Represents user streak data tracking consecutive days with journal entries.
/// Maps to the 'user_streaks' table in Supabase.
/// </summary>
[Table("user_streaks")]
public class UserStreak : BaseModel
{
    /// <summary>
    /// The ID of the user who owns this streak record.
    /// This is the primary key and references the user's profile ID.
    /// </summary>
    [PrimaryKey("user_id", false)]
    public Guid? UserId { get; set; }

    /// <summary>
    /// The current streak - number of consecutive days with journal entries.
    /// </summary>
    [Column("current_streak")]
    public int CurrentStreak { get; set; }

    /// <summary>
    /// The longest streak ever achieved by this user.
    /// </summary>
    [Column("longest_streak")]
    public int LongestStreak { get; set; }

    /// <summary>
    /// The date of the last journal entry that contributed to the streak.
    /// </summary>
    [Column("last_entry_date")]
    public DateTime LastEntryDate { get; set; }
}