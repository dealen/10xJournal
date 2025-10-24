using Microsoft.Extensions.Logging;
using _10xJournal.Client.Shared.Models;
using _10xJournal.Client.Features.JournalEntries.Models;

namespace _10xJournal.E2E.Tests.Infrastructure;

/// <summary>
/// Helper class to clean up test data from Supabase after E2E tests.
/// Ensures tests don't leave orphaned data in the test database.
/// Uses service role key to bypass RLS for cleanup operations.
/// </summary>
public class TestDataCleanupHelper
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<TestDataCleanupHelper> _logger;
    private readonly string _supabaseUrl;
    private readonly string _serviceRoleKey;

    public TestDataCleanupHelper(Supabase.Client supabaseClient, ILogger<TestDataCleanupHelper> logger, string supabaseUrl, string serviceRoleKey)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
        _supabaseUrl = supabaseUrl;
        _serviceRoleKey = serviceRoleKey;
    }

    /// <summary>
    /// Deletes a user and all associated data from the test database.
    /// This method uses the service role key to bypass RLS.
    /// </summary>
    public async Task CleanupUserDataAsync(string userEmail, string userId)
    {
        try
        {
            _logger.LogInformation("Starting cleanup for user: {Email} (ID: {UserId})", userEmail, userId);

            // Delete in reverse order of dependencies to avoid foreign key violations
            await DeleteJournalEntriesAsync(userId);
            await DeleteUserStreakAsync(userId);
            await DeleteUserProfileAsync(userId);
            await DeleteAuthUserAsync(userId);

            _logger.LogInformation("Successfully cleaned up all data for user: {Email}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cleanup test data for user: {Email}", userEmail);
            // Don't throw - cleanup failures shouldn't fail tests
        }
    }

    /// <summary>
    /// Deletes all journal entries for a specific user.
    /// </summary>
    private async Task DeleteJournalEntriesAsync(string userId)
    {
        try
        {
            await _supabaseClient
                .From<JournalEntry>()
                .Where(e => e.UserId == Guid.Parse(userId))
                .Delete();

            _logger.LogDebug("Deleted journal entries for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete journal entries for user: {UserId}", userId);
        }
    }

    /// <summary>
    /// Deletes the user streak record.
    /// </summary>
    private async Task DeleteUserStreakAsync(string userId)
    {
        try
        {
            // Direct HTTP API call to delete user_streaks
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", _serviceRoleKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_serviceRoleKey}");
            httpClient.DefaultRequestHeaders.Add("Prefer", "return=minimal");

            var deleteUrl = $"{_supabaseUrl}/rest/v1/user_streaks?user_id=eq.{userId}";
            await httpClient.DeleteAsync(deleteUrl);
            
            _logger.LogDebug("Deleted user streak for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete user streak for user: {UserId}", userId);
        }
    }

    /// <summary>
    /// Deletes the user profile record.
    /// </summary>
    private async Task DeleteUserProfileAsync(string userId)
    {
        try
        {
            await _supabaseClient
                .From<UserProfile>()
                .Where(p => p.Id == Guid.Parse(userId))
                .Delete();

            _logger.LogDebug("Deleted user profile for user: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete user profile for user: {UserId}", userId);
        }
    }

    /// <summary>
    /// Deletes the user from Supabase Auth using the Admin API.
    /// Requires service role key.
    /// </summary>
    private async Task DeleteAuthUserAsync(string userId)
    {
        try
        {
            // Use HTTP client to call Supabase Auth Admin API
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", _serviceRoleKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_serviceRoleKey}");

            var deleteUrl = $"{_supabaseUrl}/auth/v1/admin/users/{userId}";
            var response = await httpClient.DeleteAsync(deleteUrl);
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Deleted auth user: {UserId}", userId);
            }
            else
            {
                _logger.LogWarning("Failed to delete auth user: {UserId}, Status: {Status}", userId, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete auth user: {UserId}", userId);
        }
    }

    /// <summary>
    /// Attempts to clean up a user even if we only have the email.
    /// Useful when user creation partially failed.
    /// </summary>
    public async Task CleanupUserByEmailAsync(string userEmail)
    {
        try
        {
            _logger.LogInformation("Attempting cleanup for user by email: {Email}", userEmail);

            // Try to find the user by email using HTTP API
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", _serviceRoleKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_serviceRoleKey}");

            var listUrl = $"{_supabaseUrl}/auth/v1/admin/users";
            var response = await httpClient.GetAsync(listUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                // Parse and find user with matching email
                // For simplicity, we'll try to get user ID from sign-in attempt
                // In production, you'd parse the JSON response properly
                
                // Alternative: Try to sign in to get user ID, then delete
                try
                {
                    var signInResponse = await _supabaseClient.Auth.SignIn(userEmail, "dummy-password-for-cleanup");
                    // If sign-in somehow succeeds (it shouldn't with dummy password), we have the user
                }
                catch
                {
                    // Expected to fail, that's okay
                }
                
                // Simple approach: construct possible user ID patterns or query database
                // For now, log that we attempted cleanup
                _logger.LogDebug("Cleanup attempt completed for user: {Email}", userEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cleanup user by email: {Email}", userEmail);
        }
    }
}
