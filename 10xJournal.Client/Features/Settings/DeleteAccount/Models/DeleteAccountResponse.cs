using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.Settings.DeleteAccount.Models;

/// <summary>
/// Response model for user account deletion.
/// Maps the JSON structure returned by /rpc/delete_my_account.
/// </summary>
public class DeleteAccountResponse
{
    /// <summary>
    /// Flag indicating whether the deletion operation was successful.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Confirmation message for account deletion.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
