// Controllers/UpdateController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StellaLearning.Dtos;
using StellaLearningBackend.Data;
using StellaLearningBackend.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.StaticFiles; // Required for content types
using System.Net;
using System.ComponentModel.DataAnnotations; // Required for HttpStatusCode

namespace StellaLearningBackend.Controllers;
[Route("api/[controller]")]
[ApiController]
public class UpdateController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UpdateController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment; // To get ContentRootPath
    private readonly string _appVersionStorageBasePath;

    public UpdateController(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<UpdateController> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;

        // Read the base path for app versions from configuration
        _appVersionStorageBasePath = _configuration["StorageSettings:AppVersionBasePath"]
            ?? Path.Combine("App_Data", "AppVersions"); // Default fallback

        // Ensure the base directory exists (similar to StorageController)
        var absoluteStorageBasePath = Path.Combine(_webHostEnvironment.ContentRootPath, _appVersionStorageBasePath);
        if (!Directory.Exists(absoluteStorageBasePath))
        {
            try
            {
                Directory.CreateDirectory(absoluteStorageBasePath);
                _logger.LogInformation("Base app version storage directory created at {Path}", absoluteStorageBasePath);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to create base app version storage directory at {Path}. Update features may fail.", absoluteStorageBasePath);
                // Consider if the application should halt or just log the error.
                // Throwing here would prevent the controller from being constructed if the dir fails.
                // throw;
            }
        }
    }

    // --- Client Endpoint: Check for Latest Version ---
    // GET: api/update/latest?platform=Windows&architecture=x64
    [HttpGet("latest")]
    [AllowAnonymous] // Or [Authorize] if only logged-in users can check
    [ProducesResponseType(typeof(LatestVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLatestVersion([FromQuery] string? platform = "Any", [FromQuery] string? architecture = "Any")
    {
        _logger.LogInformation("Checking latest version for Platform: {Platform}, Architecture: {Architecture}", platform ?? "Any", architecture ?? "Any");

        try
        {
            // Query for the latest version matching the criteria
            var latestVersion = await _context.ApplicationVersions
                .Where(v => v.IsLatest &&
                            (v.Platform == platform || v.Platform == "Any" || platform == "Any") &&
                            (v.Architecture == architecture || v.Architecture == "Any" || architecture == "Any"))
                .OrderByDescending(v => v.ReleaseDate) // Should only be one 'IsLatest', but order just in case
                .FirstOrDefaultAsync();

            if (latestVersion == null)
            {
                _logger.LogWarning("No latest version found for Platform: {Platform}, Architecture: {Architecture}", platform ?? "Any", architecture ?? "Any");
                return NotFound(new { Message = "No update information found for the specified criteria." });
            }

            // Construct the absolute download URL dynamically
            // Important: This URL needs to be accessible by the client.
            // Ensure your routing and server setup allows GET requests to api/update/download/{id}
            var downloadUrl = Url.Action(nameof(DownloadVersion), // Use nameof for refactoring safety
                                         "Update", // Controller name without "Controller" suffix
                                         new { versionId = latestVersion.Id }, // Route parameters
                                         Request.Scheme, // http or https
                                         Request.Host.ToString()); // Server host

            if (string.IsNullOrEmpty(downloadUrl))
            {
                _logger.LogError("Could not generate download URL for Version ID: {VersionId}", latestVersion.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to construct download URL." });
            }


            var dto = new LatestVersionDto
            {
                Id = latestVersion.Id,
                VersionNumber = latestVersion.VersionNumber,
                ReleaseDate = latestVersion.ReleaseDate,
                ReleaseNotes = latestVersion.ReleaseNotes,
                FileSize = latestVersion.FileSize,
                Checksum = latestVersion.Checksum,
                DownloadUrl = downloadUrl, // Use the generated URL
                Platform = latestVersion.Platform,
                Architecture = latestVersion.Architecture
            };

            _logger.LogInformation("Found latest version: {VersionNumber} (ID: {VersionId})", dto.VersionNumber, dto.Id);
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest version information for Platform: {Platform}, Architecture: {Architecture}", platform ?? "Any", architecture ?? "Any");
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while checking for updates." });
        }
    }

    // --- Client Endpoint: Download Specific Version ---
    // GET: api/update/download/{versionId}
    [HttpGet("download/{versionId:guid}")]
    [AllowAnonymous] // Or [Authorize] if download requires login
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadVersion(Guid versionId)
    {
        _logger.LogInformation("Download requested for Version ID: {VersionId}", versionId);

        try
        {
            var versionInfo = await _context.ApplicationVersions
                .AsNoTracking() // Read-only operation
                .FirstOrDefaultAsync(v => v.Id == versionId);

            if (versionInfo == null)
            {
                _logger.LogWarning("Download failed: Version ID {VersionId} not found.", versionId);
                return NotFound(new { Message = "The requested application version was not found." });
            }

            // Construct the FULL physical path to the stored file
            var absoluteStoredPath = Path.Combine(_webHostEnvironment.ContentRootPath, versionInfo.StoredPath);

            if (!System.IO.File.Exists(absoluteStoredPath))
            {
                _logger.LogError("Download failed: Physical file not found at {Path} for Version ID {VersionId}.", absoluteStoredPath, versionId);
                // Don't reveal path details in the response
                return NotFound(new { Message = "The application package file is missing on the server." });
            }

            // Determine content type (e.g., application/zip, application/octet-stream)
            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(versionInfo.OriginalFileName, out var contentType))
            {
                contentType = "application/octet-stream"; // Default binary type
            }

            _logger.LogInformation("Streaming file {FileName} ({FileSize} bytes) from {Path} for Version ID {VersionId}",
                versionInfo.OriginalFileName, versionInfo.FileSize, absoluteStoredPath, versionId);

            // Return the file stream. PhysicalFile handles efficient streaming.
            return PhysicalFile(absoluteStoredPath, contentType, versionInfo.OriginalFileName); // Use original name for download prompt
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during download for Version ID: {VersionId}", versionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while processing the download request." });
        }
    }

    // --- Admin Endpoint: Upload New Version ---
    // POST: api/update/upload
    [HttpPost("upload")]
    [Authorize(Roles = "Admin")] // IMPORTANT: Secure this endpoint! Only Admins can upload.
    [ProducesResponseType(typeof(ApplicationVersion), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadVersion(
        [FromForm, Required] string versionNumber,
        [FromForm] string? releaseNotes,
        [FromForm, Required] IFormFile appPackage,
        [FromForm] string platform = "Any",
        [FromForm] string architecture = "Any")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Get admin user ID for logging/audit
        _logger.LogInformation("Admin User ID {UserId} attempting to upload Version: {VersionNumber}, Platform: {Platform}, Arch: {Architecture}, File: {FileName}, Size: {FileSize}",
            userId ?? "N/A", versionNumber, platform, architecture, appPackage.FileName, appPackage.Length);


        // --- Input Validation ---
        if (appPackage == null || appPackage.Length == 0)
        {
            return BadRequest(new { Message = "No application package file provided or file is empty." });
        }
        // Add more validation for versionNumber format if needed (e.g., Semantic Versioning regex)


        // Check if this version already exists for the platform/architecture
        bool versionExists = await _context.ApplicationVersions.AnyAsync(v =>
            v.VersionNumber == versionNumber &&
            v.Platform == platform &&
            v.Architecture == architecture);

        if (versionExists)
        {
            _logger.LogWarning("Upload failed: Version {VersionNumber} for Platform {Platform}, Arch {Architecture} already exists.", versionNumber, platform, architecture);
            // Using Conflict (409) might be more appropriate than BadRequest (400)
            return Conflict(new { Message = $"Version '{versionNumber}' for platform '{platform}' and architecture '{architecture}' already exists." });
        }

        // --- Storage ---
        var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(appPackage.FileName)}"; // Unique name + original extension
                                                                                          // Store within platform/architecture subfolders? Optional, but can help organization.
                                                                                          // var targetDir = Path.Combine(_webHostEnvironment.ContentRootPath, _appVersionStorageBasePath, platform, architecture);
        var targetDir = Path.Combine(_webHostEnvironment.ContentRootPath, _appVersionStorageBasePath); // Simpler: store all in base path
        var relativeStoredPath = Path.Combine(_appVersionStorageBasePath, uniqueFileName); // Path to store in DB
        var absoluteStoredPath = Path.Combine(targetDir, uniqueFileName); // Full path for saving

        // Ensure target directory exists
        if (!Directory.Exists(targetDir))
        {
            try { Directory.CreateDirectory(targetDir); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create target storage directory for upload: {Directory}", targetDir);
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Could not create storage directory on server." });
            }
        }

        string calculatedChecksum = string.Empty;
        long actualFileSize = 0;

        // Use a transaction to ensure DB and file operations are linked (optional but safer)
        // using var transaction = await _context.Database.BeginTransactionAsync(); // Uncomment if using transaction

        try
        {
            // Save the file physically
            _logger.LogInformation("Saving uploaded package to: {Path}", absoluteStoredPath);
            using (var stream = new FileStream(absoluteStoredPath, FileMode.Create))
            {
                await appPackage.CopyToAsync(stream);
            }
            _logger.LogInformation("Successfully saved package.");

            actualFileSize = new FileInfo(absoluteStoredPath).Length; // Get size after saving

            // Calculate Checksum (SHA-256) AFTER saving the file
            _logger.LogInformation("Calculating SHA-256 checksum for: {Path}", absoluteStoredPath);
            using (var fileStream = System.IO.File.OpenRead(absoluteStoredPath))
            {
                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = await sha256.ComputeHashAsync(fileStream);
                    // Convert byte array to hex string (lowercase)
                    calculatedChecksum = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
            _logger.LogInformation("Calculated checksum: {Checksum}", calculatedChecksum);


            // --- Update Database ---
            // 1. Find any existing 'latest' version for this platform/arch and mark it as not latest
            var currentLatestList = await _context.ApplicationVersions
                .Where(v => v.IsLatest && v.Platform == platform && v.Architecture == architecture)
                .ToListAsync();

            foreach (var oldLatest in currentLatestList)
            {
                _logger.LogInformation("Marking previous version {VersionNumber} (ID: {VersionId}) as not latest.", oldLatest.VersionNumber, oldLatest.Id);
                oldLatest.IsLatest = false;
            }

            // 2. Create the new version record
            var newVersion = new ApplicationVersion
            {
                Id = Guid.NewGuid(),
                VersionNumber = versionNumber,
                ReleaseDate = DateTime.UtcNow,
                ReleaseNotes = releaseNotes,
                OriginalFileName = appPackage.FileName,
                StoredFileName = uniqueFileName,
                StoredPath = relativeStoredPath, // Store the relative path
                FileSize = actualFileSize,
                Checksum = calculatedChecksum,
                IsLatest = true, // This new version is the latest
                Platform = platform,
                Architecture = architecture,
                UploadedByUserId = userId,
                RecordCreatedTimestamp = DateTime.UtcNow
            };

            _context.ApplicationVersions.Add(newVersion);

            // 3. Save changes
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully saved new ApplicationVersion record (ID: {VersionId}) to database.", newVersion.Id);

            // await transaction.CommitAsync(); // Uncomment if using transaction

            // Return 201 Created with the details of the new version
            return CreatedAtAction(nameof(DownloadVersion), new { versionId = newVersion.Id }, newVersion);

        }
        catch (DbUpdateException dbEx) // Catch specific DB errors
        {
            // await transaction.RollbackAsync(); // Uncomment if using transaction
            _logger.LogError(dbEx, "Database error occurred while saving new version {VersionNumber}.", versionNumber);
            CleanupFailedUpload(absoluteStoredPath); // Attempt to delete the orphaned file
                                                     // Check for unique constraint violation
            if (dbEx.InnerException?.Message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                return Conflict(new { Message = $"Version '{versionNumber}' for platform '{platform}'/'{architecture}' might already exist (database constraint)." });
            }
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "A database error occurred while saving the version information." });
        }
        catch (IOException ioEx)
        {
            // await transaction.RollbackAsync(); // Uncomment if using transaction
            _logger.LogError(ioEx, "IO error occurred during version upload {VersionNumber} to path {Path}.", versionNumber, absoluteStoredPath);
            CleanupFailedUpload(absoluteStoredPath);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An error occurred while saving the application package file." });
        }
        catch (Exception ex)
        {
            // await transaction.RollbackAsync(); // Uncomment if using transaction
            _logger.LogError(ex, "Unexpected error during version upload {VersionNumber}.", versionNumber);
            CleanupFailedUpload(absoluteStoredPath); // Attempt to delete the potentially partially saved file
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An unexpected error occurred during the upload process." });
        }
    }

    // --- Helper to clean up partially uploaded files on failure ---
    private void CleanupFailedUpload(string filePath)
    {
        if (System.IO.File.Exists(filePath))
        {
            try
            {
                System.IO.File.Delete(filePath);
                _logger.LogWarning("Cleaned up partially uploaded/failed file: {FilePath}", filePath);
            }
            catch (Exception cleanupEx)
            {
                // Log failure to delete, but don't prevent error response
                _logger.LogWarning(cleanupEx, "Failed to cleanup partial/failed file {FilePath}", filePath);
            }
        }
    }
}