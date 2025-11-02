using System.Text.Json;
using _10xJournal.Client.Infrastructure.ErrorHandling;
using Microsoft.Extensions.Logging;

namespace _10xJournal.Client.Features.Authentication.Login;

/// <summary>
/// Handles login functionality for the application.
/// </summary>
public sealed class LoginHandler
{
    private readonly Supabase.Client _supabaseClient;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(
        Supabase.Client supabaseClient,
        ILogger<LoginHandler> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="email">User's email address</param>
    /// <param name="password">User's password</param>
    /// <returns>A task representing the login operation</returns>
    /// <exception cref="Supabase.Gotrue.Exceptions.GotrueException">Thrown when login fails due to auth errors</exception>
    /// <exception cref="InvalidOperationException">Thrown when login fails for other reasons</exception>
    public async Task LoginAsync(string email, string password)
    {
        try
        {
            _logger.LogInformation("Starting login process for {Email}", email);
            
            // Step 1: Sign in with Supabase Auth
            var session = await _supabaseClient.Auth.SignIn(email, password);

            if (session?.User == null || string.IsNullOrEmpty(session.User.Id))
            {
                _logger.LogError("SignIn succeeded but returned null user for {Email}", email);
                throw new InvalidOperationException("Login failed - no user ID returned");
            }

            var userId = Guid.Parse(session.User.Id);
            _logger.LogInformation("User {UserId} signed in successfully, ensuring profile and streak records exist", userId);

            // Step 2: Ensure user profile and streak records exist
            // This is idempotent and handles cases where user was created outside normal registration flow
            await EnsureUserInitializedAsync(userId);

            _logger.LogInformation("Login completed successfully for user {UserId}", userId);
        }
        catch (Supabase.Gotrue.Exceptions.GotrueException ex)
        {
            // Log the EXACT exception message for debugging
            _logger.LogWarning(ex, "GotrueException during login for {Email}. Exception message: '{Message}'", 
                email, ex.Message ?? "null");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during login for {Email}. Exception type: {ExceptionType}, Message: '{Message}'", 
                email, ex.GetType().Name, ex.Message ?? "null");
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

            // Add timeout to prevent hanging on slow mobile networks (30 seconds)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            var rpcTask = _supabaseClient.Rpc("initialize_new_user", parameters);
            var completedTask = await Task.WhenAny(rpcTask, Task.Delay(Timeout.Infinite, cts.Token));
            
            if (completedTask != rpcTask)
            {
                _logger.LogError("initialize_new_user RPC timed out for user {UserId} after 30 seconds", userId);
                throw new OperationCanceledException("RPC call timed out");
            }
            
            var result = await rpcTask;

            if (result?.Content == null)
            {
                _logger.LogError("initialize_new_user RPC returned null response for user {UserId}", userId);
                throw new InvalidOperationException("Failed to initialize user - empty response from database");
            }

            // Parse the JSON response
            var response = JsonSerializer.Deserialize<Register.Models.InitializeUserResponse>(
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
        catch (OperationCanceledException)
        {
            _logger.LogError("initialize_new_user RPC timed out for user {UserId} after 30 seconds", userId);
            throw new InvalidOperationException("Login is taking longer than expected. Please check your connection and try again.");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Unexpected error initializing user records for {UserId}. Exception type: {ExceptionType}", 
                userId, ex.GetType().Name);
            throw new InvalidOperationException($"Failed to initialize user profile and streak: {ex.Message}", ex);
        }
    }
}