using Microsoft.Extensions.Logging;
using _10xJournal.Client.Features.Authentication.Models;
using _10xJournal.Client.Features.JournalEntries.Models;
using _10xJournal.Client.Features.Authentication.Register.Models;
using System.Text.Json;

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
            var session = await _supabaseClient.Auth.SignUp(email, password);
            
            if (session?.User == null || string.IsNullOrEmpty(session.User.Id))
            {
                _logger.LogError("SignUp succeeded but returned null user for {Email}", email);
                throw new InvalidOperationException("User registration failed - no user ID returned");
            }

            var userId = Guid.Parse(session.User.Id);
            _logger.LogInformation("User created with ID {UserId}, initializing profile and streak records", userId);

            // Step 2: Initialize user profile and streak records
            await EnsureUserInitializedAsync(userId);

            // Step 3: Set session if available (for immediate login without email confirmation)
            // If email confirmation is required, session will be null and user must confirm first
            if (!string.IsNullOrEmpty(session.AccessToken) && !string.IsNullOrEmpty(session.RefreshToken))
            {
                await _supabaseClient.Auth.SetSession(session.AccessToken, session.RefreshToken);
                _logger.LogInformation("Session established for user {UserId}", userId);
            }
            else
            {
                _logger.LogInformation("No session tokens available for user {UserId}. Email confirmation may be required.", userId);
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

    public async Task LoginAsync(string email, string password)
    {
        try
        {
            // Step 1: Sign in with Supabase Auth
            var session = await _supabaseClient.Auth.SignIn(email, password);

            if (session?.User == null || string.IsNullOrEmpty(session.User.Id))
            {
                _logger.LogError("SignIn succeeded but returned null user for {Email}", email);
                throw new InvalidOperationException("Login failed - no user ID returned");
            }

            var userId = Guid.Parse(session.User.Id);
            _logger.LogInformation("User {UserId} signed in, ensuring profile and streak records exist", userId);

            // Step 2: Ensure user profile and streak records exist
            // This is idempotent and handles cases where user was created outside normal registration flow
            await EnsureUserInitializedAsync(userId);

            _logger.LogInformation("Login completed successfully for user {UserId}", userId);
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during login for {Email}", email);
            throw;
        }
    }

    /// <summary>
    /// Ensures that the user's profile and streak records exist in the database.
    /// This method is idempotent - it's safe to call multiple times for the same user.
    /// Uses the initialize_new_user RPC function which has SECURITY DEFINER to bypass RLS.
    /// </summary>
    private async Task EnsureUserInitializedAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Calling initialize_new_user RPC for user {UserId}", userId);

            var parameters = new Dictionary<string, object>
            {
                { "p_user_id", userId }
            };

            var result = await _supabaseClient.Rpc("initialize_new_user", parameters);

            if (result?.Content == null)
            {
                _logger.LogError("initialize_new_user RPC returned null response for user {UserId}", userId);
                throw new InvalidOperationException("Failed to initialize user - empty response from database");
            }

            // Parse the JSON response
            var response = JsonSerializer.Deserialize<InitializeUserResponse>(
                result.Content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response == null)
            {
                _logger.LogError("Failed to parse initialize_new_user response for user {UserId}", userId);
                throw new InvalidOperationException("Failed to initialize user - invalid response format");
            }

            if (!response.Success)
            {
                var errorMessage = response.Error ?? "Unknown error";
                _logger.LogError("initialize_new_user failed for user {UserId}: {Error}", userId, errorMessage);
                throw new InvalidOperationException($"Failed to initialize user profile and streak: {errorMessage}");
            }

            _logger.LogInformation("Profile and streak records ensured for user {UserId}: {Message}", userId, response.Message);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error initializing user records for {UserId}", userId);
            throw new InvalidOperationException($"Failed to initialize user profile and streak: {ex.Message}", ex);
        }
    }
}
