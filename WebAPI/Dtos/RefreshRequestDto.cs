using System.ComponentModel.DataAnnotations;

namespace AyanamisTower.WebAPI.Dtos;

/// <summary>
/// Represents a data transfer object for refresh token requests.
/// </summary>
public class RefreshRequestDto
{
    /// <summary>
    /// Gets or sets the refresh token;
    /// </summary>
    [Required]
    public required string RefreshToken { get; set; }
}