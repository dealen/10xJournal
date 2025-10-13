using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace _10xJournal.Client.Features.Authentication.Logout;

/// <summary>
/// Handles user logout operations.
/// Encapsulates logout logic following Vertical Slice Architecture.
/// </summary>
public class LogoutHandler
{
    private readonly Supabase.Client _supabaseClient;
    private readonly NavigationManager _navigationManager;
    private readonly ILogger<LogoutHandler> _logger;

    public LogoutHandler(
        Supabase.Client supabaseClient, 
        NavigationManager navigationManager,
        ILogger<LogoutHandler> logger)
    {
        _supabaseClient = supabaseClient;
        _navigationManager = navigationManager;
        _logger = logger;
    }

    /// <summary>
    /// Logs out the current user and redirects to login page.
    /// </summary>
    /// <returns>Task representing the asynchronous operation.</returns>
    public async Task LogoutAsync()
    {
        try
        {
            _logger.LogInformation("User logout initiated");

            // Sign out from Supabase Auth
            await _supabaseClient.Auth.SignOut();

            _logger.LogInformation("User logged out successfully");

            // Redirect to login page
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            
            // Even if logout fails on server, clear client state and redirect
            // This ensures user can't remain in a half-logged-out state
            _navigationManager.NavigateTo("/login", forceLoad: true);
        }
    }
}
