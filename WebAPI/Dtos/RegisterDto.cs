using System.ComponentModel.DataAnnotations;

namespace AyanamisTower.WebAPI.Dtos;

/// <summary>
/// Data transfer object for user registration requests.
/// </summary>
public class RegisterDto
{
    /// <summary>
    /// The email address of the user registering for the system.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The password chosen by the user during registration.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 8)] // Example length, adjust as needed
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation of the password to ensure it was entered correctly.
    /// </summary>
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}