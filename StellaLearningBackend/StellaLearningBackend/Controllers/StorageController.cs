using Microsoft.AspNetCore.Authorization; // Required for [Authorize]
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration; // Required for IConfiguration
using Microsoft.Extensions.Logging;
using System;
using System.IO; // Required for Path and File operations
using System.Linq; // Required for LINQ methods like FirstOrDefault
using System.Security.Claims; // Required for ClaimsPrincipal and finding User ID
using System.Threading.Tasks;
using StellaLearningBackend.Models; // Your ApplicationUser model namespace
using StellaLearningBackend.Data; // Your ApplicationDbContext namespace
using StellaLearning.Dtos; // Your DTOs namespace (if creating response DTO)

namespace StellaLearningBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Ensures only authenticated users can access endpoints in this controller
    public class StorageController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context; // Inject DbContext if storing metadata
        private readonly IConfiguration _configuration;
        private readonly ILogger<StorageController> _logger;
        private readonly string _storageBasePath; // Base path for storing files

        public StorageController(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context, // Add DbContext if storing metadata
            IConfiguration configuration,
            ILogger<StorageController> logger)
        {
            _userManager = userManager;
            _context = context; // Assign DbContext
            _configuration = configuration;
            _logger = logger;

            // Get base storage path from configuration (appsettings.json)
            // Example: "StorageSettings:BasePath": "App_Data/UserUploads"
            _storageBasePath = _configuration["StorageSettings:BasePath"] ?? Path.Combine("App_Data", "UserUploads"); // Default path if not configured

            // Ensure the base directory exists
            if (!Directory.Exists(_storageBasePath))
            {
                try
                {
                    Directory.CreateDirectory(_storageBasePath);
                    _logger.LogInformation("Base storage directory created at {Path}", Path.GetFullPath(_storageBasePath));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create base storage directory at {Path}", Path.GetFullPath(_storageBasePath));
                    // Handle critical error - perhaps throw or prevent startup?
                    // For now, we'll let operations fail later if the path isn't writable.
                }
            }
        }

        // POST: api/storage/upload
        [HttpPost("upload")]
        [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)] // Define a DTO for the response
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        // Add RequestSizeLimit attribute if you want to enforce a specific limit for this endpoint
        // Example: [RequestSizeLimit(100 * 1024 * 1024)] // 100 MB limit
        public async Task<IActionResult> UploadEncryptedFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Upload attempt failed: No file provided.");
                return BadRequest(new
                {
                    Message = "No file provided or file is empty."
                });
            }

            // --- Get User ID ---
            // Recommended way: Use User.FindFirstValue
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // This should technically not happen if [Authorize] is working correctly,
                // but it's good practice to check.
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
                }); // Or InternalServerError?
            }

            _logger.LogInformation("Upload request received from User ID: {UserId}, Email: {Email}, File: {FileName}, Size: {FileSize} bytes",
                userId, user.Email, file.FileName, file.Length);

            // --- Prepare Storage Path ---
            var userStoragePath = Path.Combine(_storageBasePath, userId);
            if (!Directory.Exists(userStoragePath))
            {
                try
                {
                    Directory.CreateDirectory(userStoragePath);
                    _logger.LogInformation("Created storage directory for user {UserId} at {Path}", userId, userStoragePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create storage directory for user {UserId} at {Path}", userId, userStoragePath);
                    return StatusCode(StatusCodes.Status500InternalServerError, new
                    {
                        Message = "Could not create user storage directory."
                    });
                }
            }

            // --- Generate Secure Filename & Save File ---
            // Use a unique identifier (GUID) + a consistent extension for stored encrypted files
            var uniqueFileName = $"{Guid.NewGuid()}.zip.enc"; // Example extension
            var filePath = Path.Combine(userStoragePath, uniqueFileName);

            try
            {
                _logger.LogInformation("Attempting to save file for user {UserId} to {FilePath}", userId, filePath);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("Successfully saved encrypted file for user {UserId} to {FilePath}", userId, filePath);

                // --- (Optional) Store Metadata in Database ---
                var fileMetadata = new FileMetadata
                {
                    Id = Guid.NewGuid(), // Primary key for the metadata record
                    UserId = userId,
                    OriginalFileName = file.FileName, // Store the original name sent by client
                    StoredFileName = uniqueFileName, // Store the unique name used on the server
                    FileSize = file.Length,
                    UploadTimestamp = DateTime.UtcNow,
                    ContentType = file.ContentType, // Store content type if relevant
                    StoredPath = filePath // Store the full path or relative path based on needs
                };

                _context.FileMetadataEntries.Add(fileMetadata); // Assuming you have a DbSet named FileMetadataEntries
                await _context.SaveChangesAsync();
                _logger.LogInformation("Saved file metadata to database for StoredFileName: {StoredFileName}", uniqueFileName);


                // --- Return Success Response ---
                // Return relevant information, e.g., the stored filename or metadata ID
                return Ok(new UploadResponseDto
                {
                    Message = "File uploaded successfully.",
                    FileId = fileMetadata.Id, // ID of the metadata record
                    StoredFileName = uniqueFileName,
                    OriginalFileName = file.FileName,
                    FileSize = file.Length,
                    UploadTimestamp = fileMetadata.UploadTimestamp
                });
            }
            catch (IOException ioEx) // Catch specific IO exceptions
            {
                _logger.LogError(ioEx, "IO Error saving file {FilePath} for user {UserId}. Check disk space and permissions.", filePath, userId);
                // Clean up partially written file if possible/necessary
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception cleanupEx) { _logger.LogWarning(cleanupEx, "Failed to cleanup partial file {FilePath}", filePath); }
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An error occurred while saving the file. Insufficient disk space or permissions?"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error saving file {FilePath} for user {UserId}", filePath, userId);
                // Clean up partially written file
                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        System.IO.File.Delete(filePath);
                    }
                    catch (Exception cleanupEx) { _logger.LogWarning(cleanupEx, "Failed to cleanup partial file {FilePath}", filePath); }
                }
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An unexpected error occurred during file upload."
                });
            }
        }

        // TODO: Add endpoints for listing files, downloading files (streaming back the encrypted blob), deleting files, etc.
        // These would typically query the FileMetadata table.
    }
}
