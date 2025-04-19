// Models/ApplicationVersion.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AyanamisTower.WebAPI.Models
{
    /// <summary>
    /// Represents a specific version of an application, storing metadata required for distribution and updates.
    /// </summary>
    public class ApplicationVersion
    {
        /// <summary>
        /// Gets or sets the unique identifier for this application version record.
        /// </summary>
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the version number string (e.g., "1.2.3", "2024.4.14.1").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string VersionNumber { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Coordinated Universal Time (UTC) timestamp when this version was released.
        /// </summary>
        [Required]
        public DateTime ReleaseDate { get; set; }

        /// <summary>
        /// Gets or sets the optional release notes or description of changes for this version.
        /// </summary>
        [MaxLength(4000)]
        public string? ReleaseNotes { get; set; }

        /// <summary>
        /// Gets or sets the original filename of the application package as it was uploaded (e.g., "StellaApp-v1.2.3-win-x64.zip").
        /// </summary>
        [Required]
        [MaxLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique filename used to store the application package on the server or storage system.
        /// This often includes a unique identifier (like a GUID) to prevent naming conflicts.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the path (often relative to a configured base directory) where the application package file is stored.
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string StoredPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the application package file in bytes.
        /// </summary>
        [Required]
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the checksum (e.g., SHA-256 hash) of the application package file for integrity verification.
        /// </summary>
        [Required]
        [MaxLength(64)] // SHA-256 hash is 64 hex characters
        public string Checksum { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this version is the latest available version.
        /// Helps in quickly identifying the most recent release.
        /// </summary>
        [Required]
        public bool IsLatest { get; set; } = false;

        /// <summary>
        /// Gets or sets the optional target operating system platform for this version (e.g., "Windows", "Linux", "macOS", "Any").
        /// </summary>
        [MaxLength(50)]
        public string? Platform { get; set; }

        /// <summary>
        /// Gets or sets the optional target processor architecture for this version (e.g., "x64", "arm64", "Any").
        /// </summary>
        [MaxLength(50)]
        public string? Architecture { get; set; }

        /// <summary>
        /// Gets or sets the Coordinated Universal Time (UTC) timestamp when this version record was created in the database.
        /// </summary>
        public DateTime RecordCreatedTimestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the optional foreign key referencing the user who uploaded this application version.
        /// Useful for auditing purposes.
        /// </summary>
        public string? UploadedByUserId { get; set; }

        /// <summary>
        /// Gets or sets the optional navigation property to the ApplicationUser who uploaded this version.
        /// </summary>
        [ForeignKey("UploadedByUserId")]
        public virtual ApplicationUser? UploadedByUser { get; set; }
    }
}