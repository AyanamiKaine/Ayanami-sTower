using System;

namespace StellaLearning.Dtos // Use your DTOs namespace
{
    /// <summary>
    /// Represents the response after a successful file upload.
    /// </summary>
    public class UploadResponseDto
    {
        /// <summary>
        /// Gets or sets a success message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier for the stored file's metadata record.
        /// </summary>
        public Guid FileId { get; set; }

        /// <summary>
        /// Gets or sets the unique filename assigned on the server.
        /// </summary>
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the original filename provided by the client.
        /// </summary>
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the size of the uploaded file in bytes.
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the file was uploaded (UTC).
        /// </summary>
        public DateTime UploadTimestamp { get; set; }
    }
}

