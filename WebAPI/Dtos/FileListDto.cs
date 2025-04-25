namespace AyanamisTower.WebAPI.Dtos;

/// <summary>
/// Represents an item in a file list, containing metadata about an uploaded file.
/// </summary>
/// <remarks>
/// This DTO (Data Transfer Object) is used to transfer file metadata information
/// between different layers of the application.
/// </remarks>
public class FileListItemDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the file.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the original file name as it was uploaded.
    /// </summary>
    public required string OriginalFileName { get; set; }

    /// <summary>
    /// Gets or sets the size of the encrypted file in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the MIME content type of the file.
    /// </summary>
    public required string ContentType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the file was uploaded.
    /// </summary>
    public DateTime UploadTimestamp { get; set; }
}
