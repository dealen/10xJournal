namespace _10xJournal.Client.Features.Settings.DeleteAccount.Models;

/// <summary>
/// User deletion request model.
/// According to the API documentation, the /rpc/delete_my_account
/// endpoint does not require a request body (user_id is taken from the JWT).
/// This model is used to store validation data on the client side.
/// </summary>
public class DeleteAccountRequest
{
    /// <summary>
    /// User password for identity verification.
    /// Required before account deletion.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation phrase for account deletion.
    /// Must be exactly "delete my data".
    /// </summary>
    public string ConfirmationPhrase { get; set; } = string.Empty;

    /// <summary>
    /// Flag indicating whether the user has exported their data.
    /// </summary>
    public bool HasExportedData { get; set; }
}
