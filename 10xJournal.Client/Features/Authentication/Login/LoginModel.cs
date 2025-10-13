using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.Authentication.Login;

/// <summary>
/// View model used for capturing login credentials.
/// </summary>
public class LoginModel
{
    /// <summary>
    /// Email address used for authentication.
    /// </summary>
    [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Proszę podać poprawny adres e-mail.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Password associated with the user account.
    /// </summary>
    [Required(ErrorMessage = "Hasło jest wymagane.")]
    public string Password { get; set; } = string.Empty;
}
