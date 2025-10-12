using System;

namespace _10xJournal.Client.Features.Authentication.Services;

/// <summary>
/// Central place for translating Supabase authentication errors into user-facing messages.
/// </summary>
public static class AuthErrorMapper
{
    public static string MapLoginError(Supabase.Gotrue.Exceptions.GotrueException exception)
    {
        var message = exception.Message ?? string.Empty;

        if (message.Contains("Invalid login credentials", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Invalid login", StringComparison.OrdinalIgnoreCase))
        {
            return "Nieprawidłowy e-mail lub hasło.";
        }

        if (message.Contains("Email not confirmed", StringComparison.OrdinalIgnoreCase))
        {
            return "Potwierdź adres e-mail, zanim spróbujesz się zalogować.";
        }

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
