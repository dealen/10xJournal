using _10xJournal.Client.Features.JournalEntries.Models;

namespace _10xJournal.Client.Features.JournalEntries.Services;

public interface IJournalDataService
{
    Task<List<JournalEntry>> GetEntriesAsync();
    Task<UserStreak?> GetStreakAsync();
}
