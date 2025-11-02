using System;

namespace _10xJournal.Client.Infrastructure.ErrorHandling;

/// <summary>
/// Central place for translating Supabase authentication errors into user-facing messages.
/// </summary>
public static class AuthErrorMapper
{
    public static string MapLoginError(Supabase.Gotrue.Exceptions.GotrueException exception)
    {
        var message = exception.Message ?? string.Empty;

        // Invalid credentials variations
        if (message.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Invalid login", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("invalid credentials", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("incorrect password", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("wrong password", StringComparison.OrdinalIgnoreCase))
        {
            return "Nieprawidłowy e-mail lub hasło.";
        }

        // Email confirmation variations
        if (message.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("email confirmation", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("confirm your email", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("verify your email", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("unverified", StringComparison.OrdinalIgnoreCase))
        {
            return "Potwierdź adres e-mail, zanim spróbujesz się zalogować.";
        }

        // Rate limiting
        if (message.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("too many", StringComparison.OrdinalIgnoreCase))
        {
            return "Zbyt wiele prób logowania. Spróbuj ponownie za kilka minut.";
        }

        // Network/timeout errors
        if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("network", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("connection", StringComparison.OrdinalIgnoreCase))
        {
            return "Problem z połączeniem. Sprawdź internet i spróbuj ponownie.";
        }

        // Log unmapped error for debugging (especially important for mobile issues)
        Console.Error.WriteLine($"[AuthErrorMapper] Unmapped login error: StatusCode={exception.StatusCode}, Message='{message}'");

        return "Nie udało się zalogować. Spróbuj ponownie później.";
    }

    public static string MapRegistrationError(Supabase.Gotrue.Exceptions.GotrueException exception)
    {
        var message = exception.Message ?? string.Empty;

        if (message.Contains("User already registered", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            return "Użytkownik o tym adresie e-mail już istnieje.";
        }

        if (message.Contains("Password", StringComparison.OrdinalIgnoreCase) &&
            message.Contains("weak", StringComparison.OrdinalIgnoreCase))
        {
            return "Podane hasło jest zbyt słabe.";
        }

        if (message.Contains("Email rate limit", StringComparison.OrdinalIgnoreCase))
        {
            return "Osiągnięto limit rejestracji. Spróbuj ponownie za kilka minut.";
        }

        return "Nie udało się utworzyć konta. Sprawdź dane i spróbuj ponownie.";
    }
}