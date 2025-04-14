using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StellaLearningBackend.Models // Use your Models namespace
{
    public class FileMetadata
    {
        [Key]
        public Guid Id
        {
            get; set;
        }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User
        {
            get; set;
        }

        [Required]
        [MaxLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)] // e.g., GUID + .enc
        public string StoredFileName { get; set; } = string.Empty;

        [Required]
        public long FileSize
        {
            get; set;
        } // Size of the *encrypted* file

        [Required]
        public DateTime UploadTimestamp
        {
            get; set;
        }

        [MaxLength(255)]
        public string? ContentType
        {
            get; set;
        }

        [Required]
        [MaxLength(512)]
        public string StoredPath { get; set; } = string.Empty;

        // --- New fields for Server-Side Encryption ---

        /// <summary>
        /// The unique AES encryption key generated for this file,
        /// itself encrypted using the master key and stored as a Base64 string.
        /// </summary>
        [Required]
        public string EncryptedFileKey { get; set; } = string.Empty; // Store encrypted key

        /// <summary>
        /// The Initialization Vector (IV) used for AES encryption, stored as a Base64 string.
        /// IV can typically be stored in plaintext alongside the ciphertext.
        /// </summary>
        [Required]
        [MaxLength(24)] // Base64 length for 16-byte IV (128-bit block size)
        public string EncryptionIV { get; set; } = string.Empty; // Store IV

        // Consider adding fields like EncryptionAlgorithm ("AES-256-CBC", "AES-256-GCM") if you might change algorithms later.
    }
}
