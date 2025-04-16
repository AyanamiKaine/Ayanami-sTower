using System.ComponentModel.DataAnnotations;
namespace StellaLearning.Dtos;

/// <summary>
/// Data transfer object for requesting metadata file generation.
/// </summary>
public class GenerateMetadataFileRequestDto
{
    /// <summary>
    /// Gets or sets the unique identifier of the file for which metadata should be generated.
    /// </summary>
    [Required]
    public Guid FileId { get; set; }
    
    /// <summary>
    /// Gets or sets the maximum number of tags to generate for the file.
    /// </summary>
    public int MaxTags { get; set; } = 4;
}