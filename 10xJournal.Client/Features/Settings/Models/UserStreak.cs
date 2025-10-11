using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.Settings.Models;

/// <summary>
/// Represents user streak information from the database.
/// Maps to the 'user_streaks' table in Supabase.
/// Tracks the user's journaling consistency and streak data.
/// </summary>
public class UserStreak
{
    /// <summary>
    /// The ID of the user this streak belongs to.
    /// This is the primary key for this table.
    /// </summary>
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The user's current active streak (number of consecutive days with entries).
    /// </summary>
    [JsonPropertyName("current_streak")]
    public int CurrentStreak { get; set; }

    /// <summary>
    /// The user's longest streak ever achieved.
    /// </summary>
    [JsonPropertyName("longest_streak")]
    public int LongestStreak { get; set; }

    /// <summary>
    /// The date of the last journal entry (nullable).
    /// Used to determine if the streak should be maintained or reset.
    /// </summary>
    [JsonPropertyName("last_entry_date")]
    public DateTime? LastEntryDate { get; set; }
}
