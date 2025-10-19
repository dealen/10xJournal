namespace _10xJournal.Client.Features.JournalEntries.EditEntry;

/// <summary>
/// Represents the current state of the auto-save operation.
/// </summary>
public enum SaveStatus
{
    /// <summary>
    /// No save operation is currently happening, and no recent save has been completed.
    /// This is the initial state before any user input.
    /// </summary>
    Idle,

    /// <summary>
    /// A save operation is currently in progress.
    /// The user should see a visual indicator (e.g., spinner, "Saving..." text).
    /// </summary>
    Saving,

    /// <summary>
    /// The most recent save operation completed successfully.
    /// Display a confirmation message like "Saved at HH:MM".
    /// </summary>
    Saved,

    /// <summary>
    /// The most recent save operation failed due to an error (e.g., network issue, server error).
    /// Display an error message and optionally offer retry functionality.
    /// </summary>
    Error
}
