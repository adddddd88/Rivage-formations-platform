using System.ComponentModel.DataAnnotations;

namespace Rivage.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email requis"), EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mot de passe requis"), DataType(DataType.Password), Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Se souvenir de moi")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required, Display(Name = "Prénom"), StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, Display(Name = "Nom"), StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), StringLength(100, MinimumLength = 8), Display(Name = "Mot de passe")]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Les mots de passe ne correspondent pas."), Display(Name = "Confirmer")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Je confirme que mon adresse email est valide")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Veuillez confirmer votre email.")]
    public bool ConfirmEmailValid { get; set; }
}
