// Models/ApplicationVersion.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StellaLearningBackend.Models
{
    public class ApplicationVersion
    {
        [Key]
        public Guid Id { get; set; } // Unique identifier for this version entry

        [Required]
        [MaxLength(50)] // Adjust length as needed for your versioning scheme (e.g., "1.2.3", "2024.4.14.1")
        public string VersionNumber { get; set; } = string.Empty;

        [Required]
        public DateTime ReleaseDate { get; set; } // UTC timestamp of release

        [MaxLength(4000)] // Or use [DataType(DataType.MultilineText)] if preferred
        public string? ReleaseNotes { get; set; } // Description of changes

        [Required]
        [MaxLength(260)] // Max path length often a consideration
        public string OriginalFileName { get; set; } = string.Empty; // e.g., "StellaApp-v1.2.3-win-x64.zip"

        [Required]
        [MaxLength(100)] // e.g., GUID + extension
        public string StoredFileName { get; set; } = string.Empty; // Unique name on disk

        [Required]
        [MaxLength(512)] // Store relative path from a base directory
        public string StoredPath { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; } // Size in bytes

        [Required]
        [MaxLength(64)] // SHA-256 hash is 64 hex characters
        public string Checksum { get; set; } = string.Empty; // SHA-256 hash of the file for integrity

        [Required]
        public bool IsLatest { get; set; } = false; // Flag to easily find the latest version

        // Optional: For platform-specific updates
        [MaxLength(50)]
        public string? Platform { get; set; } // e.g., "Windows", "Linux", "macOS", "Any"

        [MaxLength(50)]
        public string? Architecture { get; set; } // e.g., "x64", "arm64", "Any"

        // Timestamp of when this record was created in the DB
        public DateTime RecordCreatedTimestamp { get; set; } = DateTime.UtcNow;

        // Foreign key to the user who uploaded this version (optional, but good for auditing)
        public string? UploadedByUserId { get; set; }

        [ForeignKey("UploadedByUserId")]
        public virtual ApplicationUser? UploadedByUser { get; set; }
    }
}