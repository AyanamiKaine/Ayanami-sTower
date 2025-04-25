using System.ComponentModel.DataAnnotations;

namespace AyanamisTower.WebAPI.Dtos;

/// <summary>
/// Data Transfer Object for text generation requests.
/// </summary>
public class GenerateTextRequestDto
{
    /// <summary>
    /// The input text prompt for generating content.
    /// </summary>
    [Required]
    public string Prompt { get; set; } = string.Empty;
}
