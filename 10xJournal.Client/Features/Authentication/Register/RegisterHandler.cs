using System.Text.Json;
using _10xJournal.Client.Features.Authentication.Register.Models;

namespace _10xJournal.Client.Features.Authentication.Register;

/// <summary>
/// Handles user registration functionality.
/// </summary>
public sealed class RegisterHandler
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<RegisterHandler> _logger;

    public RegisterHandler(
        Supabase.Client supabaseClient,
        ILogger<RegisterHandler> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user with email and password.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password</param>
    /// <returns>A task representing the registration operation</returns>
    /// <exception cref="Supabase.Gotrue.Exceptions.GotrueException">Thrown when registration fails due to auth errors</exception>
    /// <exception cref="InvalidOperationException">Thrown when registration fails for other reasons</exception>
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