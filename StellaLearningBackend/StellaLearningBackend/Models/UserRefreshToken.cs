namespace StellaLearningBackend.Models;

public class UserRefreshToken
{
    public int Id { get; set; } // Primary Key
    public required string UserId { get; set; } // Foreign Key to ApplicationUser
    public required string Token { get; set; } // The actual refresh token string
    public DateTime ExpiresUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false; // Flag to indicate if revoked manually or used

    public ApplicationUser? User { get; set; } // Navigation property
}