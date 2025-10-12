using Microsoft.Extensions.Logging;

namespace _10xJournal.Client.Features.Authentication.Register;

/// <summary>
/// Handles Supabase-backed authentication operations for the registration flow.
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
            await _supabaseClient.Auth.SignUp(email, password);
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
