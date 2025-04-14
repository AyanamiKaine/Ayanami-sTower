using System.ComponentModel.DataAnnotations;

/// <summary>
/// DTO for generating metadata URL requests.
/// </summary>
public class GenerateMetadataUrlRequestDto
{
    /// <summary>
    /// The URL to generate metadata from.
    /// </summary>
    [Url] // Basic validation
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// The maximum number of tags to generate. Default is 5.
    /// </summary>
    public int MaxTags { get; set; } = 5;
}