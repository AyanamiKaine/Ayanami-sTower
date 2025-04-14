namespace StellaLearning.Dtos;

/// <summary>
/// Represents the authentication response data transfer object.
/// </summary>
public class AuthResponseDto
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the authentication token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date and time of the token.
    /// </summary>
    public DateTime TokenExpiration { get; set; }
    /// <summary>
    /// Gets or sets the refresh token;
    /// </summary>
    public required string RefreshToken { get; set; }
}