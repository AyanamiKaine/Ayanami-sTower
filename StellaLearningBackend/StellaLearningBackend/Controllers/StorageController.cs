using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography; // Required for encryption
using System.Text; // Required for Encoding
using System.Threading.Tasks;
using StellaLearningBackend.Models;
using StellaLearningBackend.Data;
using StellaLearning.Dtos;

namespace StellaLearningBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StorageController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StorageController> _logger;
        private readonly string _storageBasePath;
        private readonly byte[] _masterKey; // Master key for encrypting per-file keys

        public StorageController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<StorageController> logger)
        {
            _userManager = userManager;
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _storageBasePath = _configuration["StorageSettings:BasePath"] ?? Path.Combine("App_Data", "UserUploads");

            // --- Master Key Handling ---
            // IMPORTANT: Retrieve this key securely (User Secrets, Env Vars, Key Vault).
            // DO NOT store the actual key in appsettings.json for production.
            // This example reads it from configuration for simplicity, assuming it's managed securely.
            var masterKeyString = _configuration["StorageSettings:MasterEncryptionKey"];
            if (string.IsNullOrEmpty(masterKeyString) || masterKeyString.Length < 32) // Example check
            {
                _logger.LogCritical("MasterEncryptionKey is missing or too short in configuration. Ensure it is set securely.");
                // Throw an exception or handle appropriately to prevent startup without a valid key.
                throw new InvalidOperationException("MasterEncryptionKey is not configured correctly.");
            }
            // Ensure the key is the correct size for AES (e.g., 256 bits / 32 bytes)
            // This example assumes the key in config is Base64 encoded or a string that needs hashing.
            // Option 1: If key in config is Base64 encoded 32-byte key:
            // _masterKey = Convert.FromBase64String(masterKeyString);
            // Option 2: Derive a 32-byte key from the config string using a KDF (e.g., SHA256 - less ideal but simple demo)
            _masterKey = SHA256.HashData(Encoding.UTF8.GetBytes(masterKeyString)); // Creates a 32-byte hash
                                                                                   // Clear the temporary string variable
                                                                                   // --- End Master Key Handling ---


            if (!Directory.Exists(_storageBasePath))
            {
                try
                {
                    Directory.CreateDirectory(_storageBasePath);
                    _logger.LogInformation("Base storage directory created at {Path}", Path.GetFullPath(_storageBasePath));
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Failed to create base storage directory at {Path}. Halting operations.", Path.GetFullPath(_storageBasePath));
                    // Depending on requirements, might need to prevent the controller from functioning
                    throw; // Re-throw to indicate critical failure
                }
            }
        }

        // POST: api/storage/upload
        // Renamed endpoint slightly for clarity
        [HttpPost("uploadAndEncrypt")]
        [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        // [RequestSizeLimit(100 * 1024 * 1024)] // Optional: 100 MB limit
        public async Task<IActionResult> UploadAndEncryptFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload attempt failed: No file provided.");
                return BadRequest(new
                {
                    Message = "No file provided or file is empty."
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Upload failed: User ID claim not found in token.");
                return Unauthorized(new
                {
                    Message = "Unable to identify user from token."
                });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogError("Upload failed: User with ID {UserId} not found in database.", userId);
                return Unauthorized(new
                {
                    Message = "User not found."
                });
            }

            _logger.LogInformation("Upload (for encryption) request received from User ID: {UserId}, Email: {Email}, File: {FileName}, Size: {FileSize} bytes",
               userId, user.Email, file.FileName, file.Length);

            var userStoragePath = Path.Combine(_storageBasePath, userId);
            if (!Directory.Exists(userStoragePath))
            {
                try
                {
                    Directory.CreateDirectory(userStoragePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create storage directory for user {UserId}", userId);
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        Message = "Could not create user storage directory."
                    });
                }
            }

            var uniqueFileName = $"{Guid.NewGuid()}.enc"; // Store encrypted files with .enc extension
            var filePath = Path.Combine(userStoragePath, uniqueFileName);

            byte[]? fileEncryptionKey = null;
            byte[]? iv= null;
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
                    encryptedKeyForDb = EncryptData(_masterKey, fileEncryptionKey); // Simple helper needed

                    _logger.LogInformation("Generated unique AES key and IV for file {OriginalFileName}", file.FileName);

                    using var destinationStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
                    using var cryptoStream = new CryptoStream(destinationStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                    _logger.LogInformation("Starting encryption process for {OriginalFileName} to {FilePath}", file.FileName, filePath);
                    await file.CopyToAsync(cryptoStream);

                    // Dispose cryptoStream (flushes data), then destinationStream
                    // CryptoStream must be flushed upon completion, which happens implicitly when disposed.
                }
                _logger.LogInformation("Successfully encrypted and saved file for user {UserId} to {FilePath}", userId, filePath);

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
                    EncryptionIV = Convert.ToBase64String(iv) // Store IV as Base64 string
                };

                _context.FileMetadataEntries.Add(fileMetadata);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved encrypted file metadata to database for StoredFileName: {StoredFileName}", uniqueFileName);

                // --- Return Success Response ---
                return Ok(new UploadResponseDto // Use the same DTO, or create a new one if needed
                {
                    Message = "File uploaded and encrypted successfully.",
                    FileId = fileMetadata.Id,
                    StoredFileName = uniqueFileName,
                    OriginalFileName = file.FileName,
                    FileSize = fileMetadata.FileSize, // Size of encrypted file
                    UploadTimestamp = fileMetadata.UploadTimestamp
                });
            }
            catch (CryptographicException cryptoEx)
            {
                _logger.LogError(cryptoEx, "Cryptography error during file encryption for user {UserId}, file {FileName}", userId, file.FileName);
                CleanupFailedUpload(filePath);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "A cryptography error occurred during file processing."
                });
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "IO Error saving encrypted file {FilePath} for user {UserId}.", filePath, userId);
                CleanupFailedUpload(filePath);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while saving the encrypted file."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during file encryption/upload for user {UserId}, file {FileName}", userId, file.FileName);
                CleanupFailedUpload(filePath);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An unexpected error occurred during file upload."
                });
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

        // --- Helper Method for Simple Symmetric Encryption (AES-GCM preferred, but CBC shown for simplicity) ---
        // NOTE: This is a basic example. Consider using AES-GCM for authenticated encryption.
        // This helper encrypts the per-file key using the master key.
        private static byte[] EncryptData(byte[] key, byte[] dataToEncrypt)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV(); // Generate a unique IV for this encryption operation
            var iv = aes.IV;

            using var memoryStream = new MemoryStream();
            // Prepend the IV to the ciphertext for easy retrieval during decryption
            memoryStream.Write(iv, 0, iv.Length);
            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cryptoStream.Write(dataToEncrypt, 0, dataToEncrypt.Length);
                cryptoStream.FlushFinalBlock();
            }
            return memoryStream.ToArray();
        }

        // --- Helper Method for Decrypting the Per-File Key ---
        // Needed for the download/decryption endpoint (not implemented here)
        private static byte[] DecryptData(byte[] key, byte[] dataToDecrypt)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            // Read the IV from the beginning of the data
            byte[] iv = new byte[aes.BlockSize / 8];
            Array.Copy(dataToDecrypt, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                // Write the ciphertext (excluding the IV part)
                cryptoStream.Write(dataToDecrypt, iv.Length, dataToDecrypt.Length - iv.Length);
                cryptoStream.FlushFinalBlock();
            }
            return memoryStream.ToArray();
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
                    _logger.LogWarning(cleanupEx, "Failed to cleanup partial file {FilePath}", filePath);
                }
            }
        }

        // TODO: Implement a 'DownloadAndDecryptFile' endpoint.
        // This endpoint would:
        // 1. Get FileMetadata using the file ID.
        // 2. Retrieve the Base64 encoded EncryptedFileKey and EncryptionIV.
        // 3. Decode them back to byte arrays.
        // 4. Decrypt the EncryptedFileKey using the _masterKey and the DecryptData helper.
        // 5. Create an AES decryptor using the decrypted file key and the IV.
        // 6. Open a FileStream to the encrypted file on disk.
        // 7. Create a CryptoStream in Read mode wrapping the FileStream.
        // 8. Return a FileStreamResult, streaming the CryptoStream back to the client.
    }
}
