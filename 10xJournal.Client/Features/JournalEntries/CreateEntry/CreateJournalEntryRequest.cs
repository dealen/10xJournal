using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.JournalEntries.CreateEntry;

/// <summary>
/// Represents the request model for creating a new journal entry.
/// </summary>
public class CreateJournalEntryRequest
{
    /// <summary>
    /// The content of the journal entry.
    /// </summary>
    [Required(ErrorMessage = "Content is required.")]
    public string Content { get; set; } = string.Empty;
}
