
namespace StellaLearning.Dtos;
/// <summary>
/// Represents information about the latest version of the application for update purposes.
/// </summary>
/// <remarks>
/// This DTO (Data Transfer Object) contains all necessary information to identify,
/// verify, and download a specific software version.
/// </remarks>
public class LatestVersionDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the version.
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Gets or sets the version number in semantic versioning format (e.g., "1.0.0").
    /// </summary>
    public required string VersionNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the date when this version was released.
    /// </summary>
    public DateTime ReleaseDate { get; set; }
    
    /// <summary>
    /// Gets or sets the release notes describing changes in this version.
    /// </summary>
    public string? ReleaseNotes { get; set; }
    
    /// <summary>
    /// Gets or sets the size of the update file in bytes.
    /// </summary>
    public long FileSize { get; set; }
    
    /// <summary>
    /// Gets or sets the SHA-256 checksum of the update file for verification purposes.
    /// </summary>
    public required string Checksum { get; set; }
    
    /// <summary>
    /// Gets or sets the dynamically generated URL where the update can be downloaded.
    /// </summary>
    public required string DownloadUrl { get; set; }
    
    /// <summary>
    /// Gets or sets the target platform for this update (e.g., "Windows", "macOS", "Linux").
    /// </summary>
    public string? Platform { get; set; }
    
    /// <summary>
    /// Gets or sets the target CPU architecture for this update (e.g., "x64", "arm64").
    /// </summary>
    public string? Architecture { get; set; }
}