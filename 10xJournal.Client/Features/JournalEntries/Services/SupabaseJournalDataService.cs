using _10xJournal.Client.Features.JournalEntries.Models;

namespace _10xJournal.Client.Features.JournalEntries.Services;

public class SupabaseJournalDataService : IJournalDataService
{
    private readonly Supabase.Client _supabaseClient;

    public SupabaseJournalDataService(Supabase.Client supabaseClient)
    {
        _supabaseClient = supabaseClient;
    }

    public async Task<List<JournalEntry>> GetEntriesAsync()
    {
        var response = await _supabaseClient
            .From<JournalEntry>()
            .Order(entry => entry.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
            .Get();

        return response?.Models ?? new List<JournalEntry>();
    }

    public async Task<UserStreak?> GetStreakAsync()
    {
        var response = await _supabaseClient
            .From<UserStreak>()
            .Get();

        return response?.Models?.FirstOrDefault();
    }
}
