using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.Authentication.ResetPassword;

/// <summary>
/// Represents the data model for the reset password form.
/// Used for data binding and validation.
/// </summary>
public sealed class ResetPasswordModel
{
    /// <summary>
    /// Email address of the user who wants to reset their password.
    /// </summary>
    [Required(ErrorMessage = "Adres e-mail jest wymagany")]
    [EmailAddress(ErrorMessage = "Podaj prawidłowy adres e-mail")]
    [MaxLength(255, ErrorMessage = "Adres e-mail jest za długi")]
    public string Email { get; set; } = string.Empty;
}
