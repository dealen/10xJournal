namespace _10xJournal.Client.Features.Settings.Models;

/// <summary>
/// Represents a request to delete the user's account.
/// This class is used as a command model for the 'delete_my_account' RPC call.
/// While the current implementation of the RPC function does not require a body,
/// this model is created to encapsulate the request logic and allow for future
/// extensions, such as requiring a confirmation password.
/// </summary>
public class DeleteMyAccountRequest
{
    // This class is intentionally empty for now.
    // It serves as a strongly-typed representation of the delete account command.
}
