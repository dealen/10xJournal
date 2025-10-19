using Microsoft.Extensions.Logging;
using _10xJournal.Client.Features.Authentication.Models;
using _10xJournal.Client.Features.JournalEntries.Models;

namespace _10xJournal.Client.Features.Authentication.Register;

/// <summary>
/// Handles Supabase-backed authentication operations shared across authentication features.
/// </summary>
public sealed class SupabaseAuthService : IAuthService
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<SupabaseAuthService> _logger;

    public SupabaseAuthService(Supabase.Client supabaseClient, ILogger<SupabaseAuthService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task RegisterAsync(string email, string password)
    {
        try
        {
            // Step 1: Create the user account in Supabase Auth
            var response = await _supabaseClient.Auth.SignUp(email, password);
            
            if (response?.User == null || string.IsNullOrEmpty(response.User.Id))
            {
                _logger.LogError("SignUp succeeded but returned null user for {Email}", email);
                throw new InvalidOperationException("User registration failed - no user ID returned");
            }

            var userId = Guid.Parse(response.User.Id);
            _logger.LogInformation("User created with ID {UserId}, creating profile and streak records", userId);

            // Step 2: Create profile record
            // Note: The removed trigger used to do this automatically, now we do it manually
            try
            {
                await _supabaseClient
                    .From<UserProfile>()
                    .Insert(new UserProfile
                    {
                        Id = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                
                _logger.LogInformation("Profile record created for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create profile for user {UserId}", userId);
                throw new InvalidOperationException($"Failed to create user profile: {ex.Message}", ex);
            }

            // Step 3: Create streak record
            // Note: The removed trigger used to do this automatically, now we do it manually
            try
            {
                await _supabaseClient
                    .From<UserStreak>()
                    .Insert(new UserStreak
                    {
                        UserId = userId,
                        CurrentStreak = 0,
                        LongestStreak = 0,
                        LastEntryDate = DateTime.MinValue // Using MinValue for NULL equivalent
                    });
                
                _logger.LogInformation("Streak record created for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create streak for user {UserId}", userId);
                // Don't throw here - profile is more critical than streak
                // Streak can be created later if needed
            }
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during registration for {Email}", email);
            throw;
        }
    }
}
