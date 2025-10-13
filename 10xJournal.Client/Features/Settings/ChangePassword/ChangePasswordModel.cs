using System.ComponentModel.DataAnnotations;

namespace _10xJournal.Client.Features.Settings.ChangePassword;

/// <summary>
/// Model for changing password in settings.
/// </summary>
public class ChangePasswordModel
{
    [Required(ErrorMessage = "Obecne hasło jest wymagane.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nowe hasło jest wymagane.")]
    [MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potwierdzenie hasła jest wymagane.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Hasła muszą być identyczne.")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
