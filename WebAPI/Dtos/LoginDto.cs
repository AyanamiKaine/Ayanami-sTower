using System.ComponentModel.DataAnnotations;

namespace AyanamisTower.WebAPI.Dtos;

/// <summary>
/// Data transfer object for user login credentials.
/// </summary>
public class LoginDto
{
    /// <summary>
    /// Gets or sets the email address used for authentication.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password used for authentication.
    /// </summary>
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
