using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace _10xJournal.Client.Features.Authentication.Models;

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
    /// </summary>
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    /// <summary>
    /// Timestamp when the profile was created.
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the profile was last updated.
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
