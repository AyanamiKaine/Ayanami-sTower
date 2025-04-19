using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations; // For validation attributes

/// <summary>
/// Represents the data transfer object for uploading a new version of a file.
/// </summary>
/// <remarks>
/// This DTO includes the version number, an optional change log, and the file itself.
/// </remarks>
public class UploadVersionDto
{
    /// <summary>
    /// Gets or sets the version number for the upload.
    /// This field is required and has a maximum length of 50 characters.
    /// </summary>
    /// <value>The version string.</value>
    [Required(ErrorMessage = "Version number is required.")]
    [StringLength(50)] // Example validation
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional change log associated with this version.
    /// The maximum length is 2000 characters.
    /// </summary>
    /// <value>The change log text, or null if not provided.</value>
    [StringLength(2000, ErrorMessage = "Change log cannot exceed 2000 characters.")]
    public string ChangeLog { get; set; } = string.Empty; // Make it nullable if optional

    /// <summary>
    /// Gets or sets the file being uploaded.
    /// This field is required. File type and size validation might be applied separately.
    /// </summary>
    /// <value>The uploaded file.</value>
    [Required(ErrorMessage = "A file must be provided.")]
    // You might add custom validation for file type/size here if needed
    public required IFormFile File { get; set; }
}
