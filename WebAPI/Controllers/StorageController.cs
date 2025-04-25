using System.Security.Claims;
using System.Security.Cryptography; // Required for encryption
using AyanamisTower.WebAPI.Data;
using AyanamisTower.WebAPI.Dtos;
using AyanamisTower.WebAPI.Models;
using AyanamisTower.WebAPI.Util;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AyanamisTower.WebAPI.Controllers
{
    /// <summary>
    /// Controller for managing user file storage, including upload, download, listing, and deletion.
    /// Files are encrypted at rest using AES with per-file keys, which are themselves encrypted by a master key.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class StorageController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StorageController> _logger;
        private readonly string _storageBasePath;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly byte[] _masterKey; // Master key for encrypting per-file keys

        /// <summary>
        /// Initializes a new instance of the <see cref="StorageController"/> class.
        /// </summary>
        /// <param name="userManager">ASP.NET Core Identity user manager.</param>
        /// <param name="context">Database context for file metadata.</param>
        /// <param name="configuration">Application configuration.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="webHostEnvironment">Web hosting environment information.</param>
        /// <exception cref="InvalidOperationException">Thrown if the MasterEncryptionKey is missing or invalid in configuration.</exception>
        public StorageController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<StorageController> logger,
            IWebHostEnvironment webHostEnvironment
        )
        {
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _storageBasePath =
                _configuration["StorageSettings:BasePath"]
                ?? Path.Combine("App_Data", "UserUploads");

            // --- Master Key Handling ---
            // IMPORTANT: Retrieve this key securely (User Secrets, Env Vars, Key Vault).
            // DO NOT store the actual key in appsettings.json for production.
            // This example reads it from configuration for simplicity, assuming it's managed securely.
            var masterKeyBase64 = _configuration["StorageSettings:MasterEncryptionKey"];
            if (string.IsNullOrEmpty(masterKeyBase64))
            {
                _logger.LogCritical("MasterEncryptionKey (Base64) is missing in configuration.");
                throw new InvalidOperationException(
                    "MasterEncryptionKey is not configured correctly."
                );
            }

            try
            {
                _masterKey = Convert.FromBase64String(masterKeyBase64);
                if (_masterKey.Length != 32) // Check *byte* length AFTER decoding
                {
                    _logger.LogCritical(
                        "Decoded MasterEncryptionKey is not 32 bytes (256 bits) long."
                    );
                    throw new InvalidOperationException(
                        "MasterEncryptionKey must be a Base64 encoded 256-bit key."
                    );
                }
            }
            catch (FormatException ex)
            {
                _logger.LogCritical(ex, "MasterEncryptionKey is not a valid Base64 string.");
                throw new InvalidOperationException("MasterEncryptionKey is not valid Base64.", ex);
            }
            var absoluteStorageBasePath = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                _storageBasePath
            );
            if (!Directory.Exists(absoluteStorageBasePath))
            {
                try
                {
                    Directory.CreateDirectory(absoluteStorageBasePath);
                    _logger.LogInformation(
                        "Base storage directory created at {Path}",
                        absoluteStorageBasePath
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(
                        ex,
                        "Failed to create base storage directory at {Path}. Halting operations.",
                        absoluteStorageBasePath
                    );
                    throw;
                }
            }
        }

        // POST: api/storage/upload
        // Renamed endpoint slightly for clarity
        /// <summary>
        /// Uploads a file, encrypts it using AES with a unique key per file,
        /// encrypts the file key with a master key, and stores the encrypted file and its metadata.
        /// </summary>
        /// <param name="file">The file to upload via form data.</param>
        /// <returns>An <see cref="IActionResult"/> containing the result of the upload operation.</returns>
        /// <response code="200">File uploaded and encrypted successfully. Returns metadata about the stored file.</response>
        /// <response code="400">If no file is provided or the file is empty.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs during processing, encryption, or database interaction.</response>
        [HttpPost("uploadAndEncrypt")]
        [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [RequestSizeLimit(500 * 1024 * 1024)] // 500 MB limit
        public async Task<IActionResult> UploadAndEncryptFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload attempt failed: No file provided.");
                return BadRequest(new { Message = "No file provided or file is empty." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Upload failed: User ID claim not found in token.");
                return Unauthorized(new { Message = "Unable to identify user from token." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError(
                    "Upload failed: User with ID {UserId} not found in database.",
                    userId
                );
                return Unauthorized(new { Message = "User not found." });
            }

            _logger.LogInformation(
                "Upload (for encryption) request received from User ID: {UserId}, Email: {Email}, File: {FileName}, Size: {FileSize} bytes",
                userId,
                user.Email,
                file.FileName,
                file.Length
            );

            var userStoragePath = Path.Combine(_storageBasePath, userId);
            if (!Directory.Exists(userStoragePath))
            {
                try
                {
                    Directory.CreateDirectory(userStoragePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to create storage directory for user {UserId}",
                        userId
                    );
                    return StatusCode(
                        StatusCodes.Status500InternalServerError,
                        new { Message = "Could not create user storage directory." }
                    );
                }
            }

            var uniqueFileName = $"{Guid.NewGuid()}.enc"; // Store encrypted files with .enc extension
            var filePath = Path.Combine(userStoragePath, uniqueFileName);

            byte[]? fileEncryptionKey = null;
            byte[]? iv = null;
            byte[]? encryptedKeyForDb = null; // The per-file key, itself encrypted by the master key

            try
            {
                // --- Encryption ---
                using (var aes = Aes.Create())
                {
                    aes.GenerateKey(); // Generate a unique key for this file
                    aes.GenerateIV(); // Generate a unique IV for this file
                    fileEncryptionKey = aes.Key;
                    iv = aes.IV;

                    // Encrypt the generated file key using the master key (AES encryption)
                    encryptedKeyForDb = EncryptionHelper.EncryptData(_masterKey, fileEncryptionKey); // Simple helper needed

                    _logger.LogInformation(
                        "Generated unique AES key and IV for file {OriginalFileName}",
                        file.FileName
                    );

                    await using var destinationStream = new FileStream(
                        filePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None
                    );
                    await using var cryptoStream = new CryptoStream(
                        destinationStream,
                        aes.CreateEncryptor(),
                        CryptoStreamMode.Write
                    );
                    _logger.LogInformation(
                        "Starting encryption process for {OriginalFileName} to {FilePath}",
                        file.FileName,
                        filePath
                    );
                    await file.CopyToAsync(cryptoStream);

                    // Dispose cryptoStream (flushes data), then destinationStream
                    // CryptoStream must be flushed upon completion, which happens implicitly when disposed.
                }
                _logger.LogInformation(
                    "Successfully encrypted and saved file for user {UserId} to {FilePath}",
                    userId,
                    filePath
                );

                // --- Store Metadata ---
                var fileMetadata = new FileMetadata
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    OriginalFileName = file.FileName,
                    StoredFileName = uniqueFileName,
                    FileSize = new FileInfo(filePath).Length, // Get size of the *encrypted* file
                    UploadTimestamp = DateTime.UtcNow,
                    ContentType = file.ContentType,
                    StoredPath = filePath,
                    // Store the encrypted key and the IV (IV can be stored in plaintext)
                    EncryptedFileKey = Convert.ToBase64String(encryptedKeyForDb), // Store as Base64 string
                    EncryptionIV = Convert.ToBase64String(
                        iv
                    ) // Store IV as Base64 string
                    ,
                };

                _context.FileMetadataEntries.Add(fileMetadata);
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Saved encrypted file metadata to database for StoredFileName: {StoredFileName}",
                    uniqueFileName
                );

                // --- Return Success Response ---
                return Ok(
                    new UploadResponseDto // Use the same DTO, or create a new one if needed
                    {
                        Message = "File uploaded and encrypted successfully.",
                        FileId = fileMetadata.Id,
                        StoredFileName = uniqueFileName,
                        OriginalFileName = file.FileName,
                        FileSize = fileMetadata.FileSize, // Size of encrypted file
                        UploadTimestamp = fileMetadata.UploadTimestamp,
                    }
                );
            }
            catch (CryptographicException cryptoEx)
            {
                _logger.LogError(
                    cryptoEx,
                    "Cryptography error during file encryption for user {UserId}, file {FileName}",
                    userId,
                    file.FileName
                );
                CleanupFailedUpload(filePath);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "A cryptography error occurred during file processing." }
                );
            }
            catch (IOException ioEx)
            {
                _logger.LogError(
                    ioEx,
                    "IO Error saving encrypted file {FilePath} for user {UserId}.",
                    filePath,
                    userId
                );
                CleanupFailedUpload(filePath);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while saving the encrypted file." }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during file encryption/upload for user {UserId}, file {FileName}",
                    userId,
                    file.FileName
                );
                CleanupFailedUpload(filePath);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "An unexpected error occurred during file upload." }
                );
            }
            finally
            {
                // Securely clear sensitive byte arrays from memory (best effort)
                if (fileEncryptionKey != null)
                    Array.Clear(fileEncryptionKey, 0, fileEncryptionKey.Length);
                if (iv != null)
                    Array.Clear(iv, 0, iv.Length);
                // Note: _masterKey should be cleared on application shutdown if possible/necessary
            }
        }

        // --- Helper to clean up partially written files ---
        private void CleanupFailedUpload(string filePath)
        {
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogWarning("Cleaned up partially uploaded file: {FilePath}", filePath);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(
                        cleanupEx,
                        "Failed to cleanup partial file {FilePath}",
                        filePath
                    );
                }
            }
        }

        /// <summary>
        /// Retrieves a list of files uploaded by the currently authenticated user.
        /// </summary>
        /// <returns>An <see cref="IActionResult"/> containing a list of <see cref="FileListItemDto"/> objects or an error response.</returns>
        /// <response code="200">Returns the list of files for the user.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs while retrieving the file list.</response>
        [HttpGet("myfiles")]
        [ProducesResponseType(typeof(List<FileListItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ListMyFiles()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // This shouldn't happen if [Authorize] is working, but defense in depth
                _logger.LogWarning("ListMyFiles failed: User ID claim not found in token.");
                return Unauthorized(new { Message = "User identifier not found." });
            }

            _logger.LogInformation("User {UserId} requesting their file list.", userId);

            try
            {
                var files = await _context
                    .FileMetadataEntries.Where(f => f.UserId == userId)
                    .OrderByDescending(f => f.UploadTimestamp) // Show newest first
                    .Select(f => new FileListItemDto // Project to DTO
                    {
                        Id = f.Id,
                        OriginalFileName = f.OriginalFileName,
                        FileSize = f.FileSize,
                        ContentType = f.ContentType ?? "",
                        UploadTimestamp = f.UploadTimestamp,
                    })
                    .ToListAsync(); // Execute the query

                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving file list for user {UserId}", userId);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "An error occurred while retrieving the file list." }
                );
            }
        }

        // --- NEW: Delete a File ---
        // DELETE: api/storage/{fileId}
        /// <summary>
        /// Deletes a specific file belonging to the authenticated user, removing both the physical file and its metadata.
        /// </summary>
        /// <param name="fileId">The unique identifier (GUID) of the file to delete.</param>
        /// <returns>An <see cref="IActionResult"/> indicating the result of the deletion operation.</returns>
        /// <response code="204">Indicates the file was successfully deleted (both physical file and metadata).</response>
        /// <response code="401">If the user is not authenticated or the user ID cannot be determined from the token.</response>
        /// <response code="404">If a file with the specified <paramref name="fileId"/> is not found for the authenticated user.</response>
        /// <response code="500">If an internal server error occurs during the deletion process (e.g., database error, file system error).</response>
        [HttpDelete("{fileId:guid}")] // Route constraint for GUID
        [ProducesResponseType(StatusCodes.Status204NoContent)] // Success
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteFile(Guid fileId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("DeleteFile failed: User ID claim not found in token.");
                return Unauthorized(new { Message = "User identifier not found." });
            }

            _logger.LogInformation(
                "User {UserId} requesting deletion of File ID: {FileId}",
                userId,
                fileId
            );

            FileMetadata? fileMetadata;
            try
            {
                // Find the file making sure it belongs to the current user
                fileMetadata = await _context.FileMetadataEntries.FirstOrDefaultAsync(f =>
                    f.Id == fileId && f.UserId == userId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Database error looking up FileMetadata for deletion (File ID {FileId}, User ID {UserId})",
                    fileId,
                    userId
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Database error finding file." }
                );
            }

            if (fileMetadata == null)
            {
                _logger.LogWarning(
                    "DeleteFile failed: File ID {FileId} not found for User ID {UserId}.",
                    fileId,
                    userId
                );
                // Return 404 - don't reveal if file exists but belongs to someone else
                return NotFound(new { Message = "File not found." });
            }

            // --- Delete physical file ---
            var relativeStoredPath = fileMetadata.StoredPath;
            var absoluteStoredPath = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                relativeStoredPath
            );
            try
            {
                // Use absolute path for Exists and Delete
                if (System.IO.File.Exists(absoluteStoredPath))
                {
                    System.IO.File.Delete(absoluteStoredPath);
                    _logger.LogInformation(
                        "Successfully deleted physical file: {AbsoluteStoredPath}",
                        absoluteStoredPath
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Physical file not found at {AbsoluteStoredPath} during deletion for File ID {FileId}, but metadata exists.",
                        absoluteStoredPath,
                        fileId
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting physical file {AbsoluteStoredPath} for File ID {FileId}. Proceeding to delete metadata.",
                    absoluteStoredPath,
                    fileId
                );
            }

            // --- Delete metadata record ---
            try
            {
                _context.FileMetadataEntries.Remove(fileMetadata);
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Successfully deleted metadata for File ID: {FileId}",
                    fileId
                );

                // Return 204 No Content on successful deletion
                // TODO: Maybe we want to return a succesfull message instead.
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Database error deleting FileMetadata for File ID {FileId}",
                    fileId
                );
                // If DB delete fails, the file might still be gone from disk.
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Error deleting file metadata from database." }
                );
            }
        }

        // --- NEW: Download Encrypted File ---
        // GET: api/storage/{fileId}/download
        /// <summary>
        /// Downloads the encrypted file associated with the specified file ID for the authenticated user.
        /// The file is streamed directly without decryption on the server.
        /// </summary>
        /// <param name="fileId">The unique identifier (GUID) of the file to download.</param>
        /// <returns>An <see cref="IActionResult"/> representing the encrypted file stream or an error response.</returns>
        /// <response code="200">Returns the encrypted file as a stream.</response>
        /// <response code="401">If the user is not authenticated or the user ID cannot be determined from the token.</response>
        /// <response code="404">If a file with the specified <paramref name="fileId"/> is not found for the authenticated user, or the physical file is missing.</response>
        /// <response code="500">If an internal server error occurs during the download process (e.g., database error, file system error).</response>
        [HttpGet("{fileId:guid}/download")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadEncryptedFile(Guid fileId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning(
                    "DownloadEncryptedFile failed: User ID claim not found in token."
                );
                return Unauthorized(new { Message = "User identifier not found." });
            }

            _logger.LogInformation(
                "User {UserId} requesting download of File ID: {FileId}",
                userId,
                fileId
            );

            FileMetadata? fileMetadata;
            try
            {
                // Find the file making sure it belongs to the current user
                fileMetadata = await _context
                    .FileMetadataEntries.AsNoTracking() // Read-only is sufficient
                    .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Database error looking up FileMetadata for download (File ID {FileId}, User ID {UserId})",
                    fileId,
                    userId
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Database error finding file." }
                );
            }

            if (fileMetadata == null)
            {
                _logger.LogWarning(
                    "DownloadEncryptedFile failed: File ID {FileId} not found for User ID {UserId}.",
                    fileId,
                    userId
                );
                return NotFound(new { Message = "File not found." });
            }

            var relativeStoredPath = fileMetadata.StoredPath;
            var absoluteStoredPath = Path.Combine(
                _webHostEnvironment.ContentRootPath,
                relativeStoredPath
            );
            // ---

            // Check if the physical file actually exists using ABSOLUTE path
            if (!System.IO.File.Exists(absoluteStoredPath))
            {
                _logger.LogError(
                    "Physical file not found at {AbsoluteStoredPath} during download request for File ID {FileId}.",
                    absoluteStoredPath,
                    fileId
                );
                return NotFound(new { Message = "Stored file is missing." });
            }

            try
            {
                _logger.LogInformation(
                    "Streaming encrypted file {AbsoluteStoredPath} as download '{DownloadName}' with content type {ContentType}",
                    absoluteStoredPath,
                    fileMetadata.OriginalFileName,
                    fileMetadata.ContentType ?? "application/octet-stream"
                );

                // Use ABSOLUTE path for PhysicalFile
                return PhysicalFile(
                    absoluteStoredPath,
                    fileMetadata.ContentType ?? "application/octet-stream",
                    fileMetadata.OriginalFileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error preparing file download for File ID {FileId} from path {AbsoluteStoredPath}",
                    fileId,
                    absoluteStoredPath
                );
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "Error occurred while preparing the file for download." }
                );
            }
        }
    }
}
