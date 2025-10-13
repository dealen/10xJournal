using _10xJournal.Client.Features.JournalEntries.Models;
using Microsoft.Extensions.Logging;

namespace _10xJournal.Client.Features.JournalEntries.WelcomeEntry;

/// <summary>
/// Service responsible for creating the welcome entry for new users.
/// </summary>
public class WelcomeEntryService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<WelcomeEntryService> _logger;
    
    // Welcome entry content in Polish
    private const string WELCOME_CONTENT = @"Witaj w 10xJournal!

To jest Twoja prywatna przestrzeń do myślenia i pisania, wolna od rozpraszaczy. Celem tej aplikacji jest pomóc Ci w budowaniu nawyku regularnego prowadzenia dziennika.

Możesz edytować lub usunąć ten wpis. Kliknij przycisk 'Nowy wpis', aby rozpocząć swoją historię.";

    public WelcomeEntryService(Supabase.Client supabaseClient, ILogger<WelcomeEntryService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a welcome entry for the authenticated user if they have no entries yet.
    /// </summary>
    /// <returns>True if welcome entry was created, false if user already has entries or creation failed.</returns>
    public async Task<bool> CreateWelcomeEntryIfNeededAsync()
    {
        try
        {
            // Check if user already has any entries
            var existingEntries = await _supabaseClient
                .From<JournalEntry>()
                .Select("id")
                .Limit(1)
                .Get();

            if (existingEntries?.Models?.Count > 0)
            {
                _logger.LogDebug("User already has entries, skipping welcome entry creation");
                return false;
            }

            // Get current user ID
            var user = _supabaseClient.Auth.CurrentUser;
            if (user == null || string.IsNullOrEmpty(user.Id))
            {
                _logger.LogWarning("Cannot create welcome entry: user not authenticated");
                return false;
            }

            // Create welcome entry
            var welcomeEntry = new JournalEntry
            {
                UserId = Guid.Parse(user.Id),
                Content = WELCOME_CONTENT,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var response = await _supabaseClient
                .From<JournalEntry>()
                .Insert(welcomeEntry);

            if (response?.Models?.Count > 0)
            {
                _logger.LogInformation("Welcome entry created successfully for user {UserId}", user.Id);
                return true;
            }

            _logger.LogWarning("Welcome entry creation returned empty response");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating welcome entry");
            return false;
        }
    }
}
