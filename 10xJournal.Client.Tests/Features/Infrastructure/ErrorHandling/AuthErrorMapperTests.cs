using _10xJournal.Client.Infrastructure.ErrorHandling;
using FluentAssertions;
using Supabase.Gotrue.Exceptions;
using Xunit;

namespace _10xJournal.Client.Tests.Features.Infrastructure.ErrorHandling;

/// <summary>
/// Unit tests for AuthErrorMapper - a pure function mapping Supabase errors to user-friendly Polish messages.
/// Tests critical error patterns and case-insensitive matching.
/// </summary>
public class AuthErrorMapperTests
{
    #region MapLoginError

    [Theory]
    [InlineData("Invalid login credentials")]
    [InlineData("invalid login")]
    [InlineData("INVALID LOGIN CREDENTIALS")]
    public void MapLoginError_WithInvalidCredentials_ReturnsPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapLoginError(exception);

        result.Should().Be("Nieprawidłowy e-mail lub hasło.");
    }

    [Theory]
    [InlineData("Email not confirmed")]
    [InlineData("email not confirmed")]
    public void MapLoginError_WithUnconfirmedEmail_ReturnsPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapLoginError(exception);

        result.Should().Be("Potwierdź adres e-mail, zanim spróbujesz się zalogować.");
    }

    [Theory]
    [InlineData("Rate limit exceeded")]
    [InlineData("Too many attempts")]
    [InlineData("rate limit")]
    public void MapLoginError_WithRateLimit_ReturnsPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapLoginError(exception);

        result.Should().Be("Zbyt wiele prób logowania. Spróbuj ponownie za kilka minut.");
    }

    [Theory]
    [InlineData("Network error")]
    [InlineData("Connection timeout")]
    [InlineData("network failure")]
    public void MapLoginError_WithNetworkError_ReturnsNetworkPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapLoginError(exception);

        result.Should().Be("Problem z połączeniem. Sprawdź internet i spróbuj ponownie.");
    }

    [Theory]
    [InlineData("Unknown database error")]
    [InlineData("")]
    [InlineData("Some random error")]
    public void MapLoginError_WithUnknownError_ReturnsGenericPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapLoginError(exception);

        result.Should().Be("Nie udało się zalogować. Spróbuj ponownie później.");
    }

    [Fact]
    public void MapLoginError_WithNullMessage_ReturnsGenericMessage()
    {
        var exception = new GotrueException(null);

        var result = AuthErrorMapper.MapLoginError(exception);

        result.Should().Be("Nie udało się zalogować. Spróbuj ponownie później.");
    }

    #endregion

    #region MapRegistrationError

    [Theory]
    [InlineData("User already registered")]
    [InlineData("Email already exists")]
    [InlineData("user already registered")]
    public void MapRegistrationError_WithUserExists_ReturnsPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapRegistrationError(exception);

        result.Should().Be("Użytkownik o tym adresie e-mail już istnieje.");
    }

    [Theory]
    [InlineData("Password is too weak")]
    [InlineData("Weak password provided")]
    public void MapRegistrationError_WithWeakPassword_ReturnsPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapRegistrationError(exception);

        result.Should().Be("Podane hasło jest zbyt słabe.");
    }

    [Theory]
    [InlineData("Email rate limit exceeded")]
    [InlineData("email rate limit")]
    public void MapRegistrationError_WithRateLimit_ReturnsPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapRegistrationError(exception);

        result.Should().Be("Osiągnięto limit rejestracji. Spróbuj ponownie za kilka minut.");
    }

    [Theory]
    [InlineData("Network error")]
    [InlineData("")]
    public void MapRegistrationError_WithUnknownError_ReturnsGenericPolishMessage(string errorMessage)
    {
        var exception = new GotrueException(errorMessage);

        var result = AuthErrorMapper.MapRegistrationError(exception);

        result.Should().Be("Nie udało się utworzyć konta. Sprawdź dane i spróbuj ponownie.");
    }

    [Fact]
    public void MapRegistrationError_WithNullMessage_ReturnsGenericMessage()
    {
        var exception = new GotrueException(null);

        var result = AuthErrorMapper.MapRegistrationError(exception);

        result.Should().Be("Nie udało się utworzyć konta. Sprawdź dane i spróbuj ponownie.");
    }

    [Fact]
    public void MapRegistrationError_WithPasswordButNotWeak_ReturnsGenericMessage()
    {
        // Both "Password" AND "weak" must be present
        var exception = new GotrueException("Password must contain special characters");

        var result = AuthErrorMapper.MapRegistrationError(exception);

        result.Should().Be("Nie udało się utworzyć konta. Sprawdź dane i spróbuj ponownie.");
    }

    #endregion
}
