using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.Authentication.Register.Models;

/// <summary>
/// Response model for the initialize_new_user RPC function.
/// Maps the JSON structure returned by /rpc/initialize_new_user.
/// </summary>
public sealed class InitializeUserResponse
{
    /// <summary>
    /// Indicates whether the initialization was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// The user ID that was initialized.
    /// </summary>
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Message describing the result of the operation.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Error message if the operation failed.
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
