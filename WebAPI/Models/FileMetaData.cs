using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AyanamisTower.WebAPI.Models;

/// <summary>
/// Represents the metadata associated with a file stored in the system.
/// This includes information about the file's identity, ownership, storage details,
/// and server-side encryption parameters.
/// </summary>
/// <remarks>
/// This class is typically used as an Entity Framework Core entity to store file metadata in a database.
/// </remarks>
public class FileMetadata
{
    /// <summary>
    /// Gets or sets the unique identifier for the file metadata record.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who uploaded the file.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the navigation property to the ApplicationUser who owns this file.
    /// </summary>
    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    /// <summary>
    /// Gets or sets the original name of the file as uploaded by the user.
    /// Maximum length is 260 characters.
    /// </summary>
    [Required]
    [MaxLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name used to store the file on the server (e.g., a GUID).
    /// Maximum length is 100 characters.
    /// </summary>
    [Required]
    [MaxLength(100)] // e.g., GUID + .enc
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the stored (potentially encrypted) file in bytes.
    /// </summary>
    [Required]
    public long FileSize { get; set; } // Size of the *encrypted* file

    /// <summary>
    /// Gets or sets the date and time when the file was uploaded.
    /// </summary>
    [Required]
    public DateTime UploadTimestamp { get; set; }

    /// <summary>
    /// Gets or sets the MIME content type of the original file (e.g., "image/jpeg").
    /// Maximum length is 255 characters. Can be null.
    /// </summary>
    [MaxLength(255)]
    public string? ContentType { get; set; }

    /// <summary>
    /// Gets or sets the path (relative or absolute) where the file is stored on the server.
    /// Maximum length is 512 characters.
    /// </summary>
    [Required]
    [MaxLength(512)]
    public string StoredPath { get; set; } = string.Empty;

    // --- New fields for Server-Side Encryption ---

    /// <summary>
    /// Gets or sets the unique AES encryption key generated for this file,
    /// itself encrypted using a master key and stored as a Base64 string.
    /// </summary>
    [Required]
    public string EncryptedFileKey { get; set; } = string.Empty; // Store encrypted key

    /// <summary>
    /// Gets or sets the Initialization Vector (IV) used for AES encryption, stored as a Base64 string.
    /// The IV is required for decryption and can typically be stored in plaintext.
    /// Maximum length is 24 characters (Base64 representation of a 16-byte/128-bit IV).
    /// </summary>
    [Required]
    [MaxLength(24)] // Base64 length for 16-byte IV (128-bit block size)
    public string EncryptionIV { get; set; } = string.Empty; // Store IV

    // Consider adding fields like EncryptionAlgorithm ("AES-256-CBC", "AES-256-GCM") if you might change algorithms later.
}
