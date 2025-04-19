using System.ComponentModel.DataAnnotations;
namespace AyanamisTower.WebAPI.Dtos;

/// <summary>
/// Represents a request for generating tags for an image.
/// </summary>
public class GenerateImageTagsRequestDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the image file for which tags will be generated.
    /// </summary>
    [Required]
    public Guid FileId { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of tags to generate. Default value is 4.
    /// </summary>
    public int MaxTags { get; set; } = 4;
}