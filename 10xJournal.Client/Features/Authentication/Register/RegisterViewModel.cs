using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.Authentication.Register;

/// <summary>
/// Represents the state of the register form and client-side validation rules.
/// </summary>
public sealed class RegisterViewModel
{
    [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Proszę podać poprawny adres e-mail.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hasło jest wymagane.")]
    [MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]
    [Compare(nameof(Password), ErrorMessage = "Hasła nie są identyczne.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
