using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.Authentication.UpdatePassword;

/// <summary>
/// Represents the data model for updating a user's password.
/// Used when user clicks the reset link from their email.
/// </summary>
public sealed class UpdatePasswordModel
{
    /// <summary>
    /// The new password the user wants to set.
    /// </summary>
    [Required(ErrorMessage = "Nowe hasło jest wymagane")]
    [MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the new password to prevent typos.
    /// </summary>
    [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
    [Compare(nameof(NewPassword), ErrorMessage = "Hasła nie są identyczne")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
