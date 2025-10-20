using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.Settings.ChangePassword.Models;

/// <summary>
/// Model żądania zmiany hasła użytkownika.
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>
    /// Aktualne hasło użytkownika.
    /// Wymagane do weryfikacji tożsamości przed zmianą hasła.
    /// </summary>
    [Required(ErrorMessage = "Aktualne hasło jest wymagane")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// Nowe hasło użytkownika.
    /// Musi spełniać minimalne wymagania bezpieczeństwa.
    /// </summary>
    [Required(ErrorMessage = "Nowe hasło jest wymagane")]
    [MinLength(8, ErrorMessage = "Hasło musi mieć co najmniej 8 znaków")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", 
        ErrorMessage = "Hasło musi zawierać co najmniej jedną wielką literę, jedną małą literę i jedną cyfrę")]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Potwierdzenie nowego hasła.
    /// Musi być identyczne z NewPassword.
    /// </summary>
    [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane")]
    [Compare(nameof(NewPassword), ErrorMessage = "Hasła muszą być identyczne")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
