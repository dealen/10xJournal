using _10xJournal.Client.Features.JournalEntries.Models;
using System.Text.Json.Serialization;

namespace _10xJournal.Client.Features.Settings.ExportData.Models;

/// <summary>
/// Represents the response from the 'export_journal_entries' RPC function.
/// This model is a wrapper for a collection of journal entries.
/// </summary>
public class ExportJournalEntriesResponse
{
    /// <summary>
    /// A list of all journal entries belonging to the user.
    /// The JSON property name "data" is a common convention for RPC responses
    /// that return a collection of items.
    /// </summary>
    [JsonPropertyName("data")]
    public List<JournalEntry> Entries { get; set; } = new();
}
