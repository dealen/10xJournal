using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.Authentication.Models;

/// <summary>
/// Represents a user profile from the database.
/// Maps to the 'profiles' table in Supabase.
/// </summary>
public class UserProfile
{
    /// <summary>
    /// Unique identifier for the user profile.
    /// This matches the user's auth.users.id from Supabase Auth.
    /// </summary>
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the profile was created.
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the profile was last updated.
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
