namespace AyanamisTower.WebAPI.Models;

/// <summary>
/// Represents a refresh token associated with a user, used for renewing access tokens.
/// </summary>
public class UserRefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier for the refresh token record. This serves as the primary key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user to whom this refresh token belongs. This is a foreign key referencing the ApplicationUser.
    /// </summary>
    public required string UserId { get; set; }

    /// <summary>
    /// Gets or sets the actual refresh token string. This value is used by the client to request a new access token.
    /// </summary>
    public required string Token { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when this refresh token expires and is no longer valid.
    /// </summary>
    public DateTime ExpiresUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time, in UTC, when this refresh token was created. Defaults to the current UTC time upon creation.
    /// </summary>
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether this refresh token has been revoked. A revoked token cannot be used to obtain new access tokens.
    /// Revocation can occur manually or automatically after the token has been used.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the navigation property back to the ApplicationUser associated with this refresh token.
    /// This allows for easy access to user details from the token entity, if needed.
    /// </summary>
    public ApplicationUser? User { get; set; }
}
