using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace _10xJournal.Client.Shared.Models;

/// <summary>
/// Represents a user profile from the database.
/// Maps to the 'profiles' table in Supabase.
/// </summary>
[Table("profiles")]
public class UserProfile : BaseModel
{
    /// <summary>
    /// Unique identifier for the user profile.
    /// This matches the user's auth.users.id from Supabase Auth.
    /// Must be explicitly set during insert to match the authenticated user's ID.
    /// </summary>
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the profile was created.
    /// Database handles this value automatically via DEFAULT now().
    /// </summary>
    [Column("created_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the profile was last updated.
    /// Database handles this value automatically via triggers.
    /// </summary>
    [Column("updated_at", ignoreOnInsert: true, ignoreOnUpdate: true)]
    public DateTimeOffset UpdatedAt { get; set; }
}