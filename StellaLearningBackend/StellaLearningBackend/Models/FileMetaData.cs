using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Required for [ForeignKey]

namespace StellaLearningBackend.Models // Use your Models namespace
{
    public class FileMetadata
    {
        [Key] // Primary Key
        public Guid Id
        {
            get; set;
        }

        [Required]
        public string UserId { get; set; } = string.Empty; // Foreign key to ApplicationUser

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User
        {
            get; set;
        } // Navigation property

        [Required]
        [MaxLength(260)] // Max path length considerations
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)] // e.g., GUID + extension
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        public long FileSize
        {
            get; set;
        } // Size in bytes

        [Required]
        public DateTime UploadTimestamp
        {
            get; set;
        }

        [MaxLength(255)] // Standard max length for MIME types
        public string? ContentType
        {
            get; set;
        } // Optional: Store the MIME type sent by client

        [Required]
        [MaxLength(512)] // Adjust size as needed for your path structure
        public string StoredPath { get; set; } = string.Empty; // Store the full or relative path where the file is saved
    }
}
