using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StellaLearning.Dtos;
using StellaLearningBackend.API;
using StellaLearningBackend.Data;
using StellaLearningBackend.Models;


namespace StellaLearningBackend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class LargeLanguageModelController : ControllerBase
{
    private readonly LargeLanguageManager _llmManager;
    private readonly ILogger<LargeLanguageModelController> _logger;
    private readonly ApplicationDbContext _context; // Inject DbContext here too

    public LargeLanguageModelController(
        LargeLanguageManager llmManager,
        ApplicationDbContext context, // Injected singleton
        ILogger<LargeLanguageModelController> logger)
    {
        _llmManager = llmManager;
        _logger = logger;
        _context = context;
    }

    // POST: api/largelanguagemodel/generate-text
    [HttpPost("generate-text")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateText([FromBody] GenerateTextRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Received request to generate text.");
        var result = await _llmManager.GetTextResponseAsync(request.Prompt);

        if (result == null)
        {
            // Logged within the manager, maybe return 500 if it indicates an unexpected failure
            _logger.LogWarning("Text generation returned null for the given prompt.");
            // Decide appropriate status: 404 (Not Found), 400 (Bad Request?), or 500 (Internal Error)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to generate text response." });
        }

        return Ok(result); // Return the generated text directly
    }

    [HttpPost("generate-metadata/file")]
    [ProducesResponseType(typeof(ContentMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Added for ownership check
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateMetadataFromFile([FromBody] GenerateMetadataFileRequestDto request) // Updated DTO
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GenerateMetadataFromFile failed: User ID claim not found in token.");
            return Unauthorized(new { Message = "User identifier not found." });
        }

        // --- Verify Ownership and Get Metadata ---
        FileMetadata? fileMetadata;
        try
        {
            fileMetadata = await _context.FileMetadataEntries
                                         .AsNoTracking() // Read-only operation
                                         .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error looking up FileMetadata for ID {FileId}", request.FileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Database error verifying file." });
        }

        if (fileMetadata == null)
        {
            _logger.LogWarning("GenerateMetadataFromFile failed: File ID {FileId} not found for User ID {UserId}.", request.FileId, userId);
            return NotFound(new { Message = "File not found or you do not have permission to access it." });
        }
        // --- End Verification ---


        _logger.LogInformation("User {UserId} requesting metadata generation for File ID: {FileId} (Original: {OriginalFileName})", userId, request.FileId, fileMetadata.OriginalFileName);

        // Call the updated manager method, passing the verified metadata object
        var result = await _llmManager.GenerateMetaDataBasedOnFileAsync(fileMetadata, request.MaxTags);

        if (result == null)
        {
            _logger.LogWarning("Metadata generation returned null for File ID: {FileId}", request.FileId);
            // Manager logs specific errors (file system, AI error). Return 500 for processing failure.
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to generate metadata from file." });
        }

        return Ok(result);
    }

    // POST: api/largelanguagemodel/generate-tags/image
    [HttpPost("generate-tags/image")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)] // Added for ownership check
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateImageTags([FromBody] GenerateImageTagsRequestDto request) // Updated DTO
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("GenerateImageTags failed: User ID claim not found in token.");
            return Unauthorized(new { Message = "User identifier not found." });
        }

        // --- Verify Ownership and Get Metadata ---
        FileMetadata? fileMetadata;
        try
        {
            fileMetadata = await _context.FileMetadataEntries
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(f => f.Id == request.FileId && f.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database error looking up FileMetadata for ID {FileId}", request.FileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Database error verifying file." });
        }


        if (fileMetadata == null)
        {
            _logger.LogWarning("GenerateImageTags failed: File ID {FileId} not found for User ID {UserId}.", request.FileId, userId);
            return NotFound(new { Message = "Image file not found or you do not have permission to access it." });
        }
        // Basic check if it *looks* like an image content type - optional
        if (fileMetadata.ContentType == null || !fileMetadata.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("GenerateImageTags attempted on non-image content type '{ContentType}' for File ID {FileId}", fileMetadata.ContentType, request.FileId);
            return BadRequest(new { Message = "The specified file does not appear to be an image." });
        }
        // --- End Verification ---


        _logger.LogInformation("User {UserId} requesting image tag generation for File ID: {FileId} (Original: {OriginalFileName})", userId, request.FileId, fileMetadata.OriginalFileName);

        // Call the updated manager method
        var result = await _llmManager.GetImageTagsAsync(fileMetadata, request.MaxTags);

        if (result == null) // Defensive check
        {
            _logger.LogError("GenerateImageTagsAsync unexpectedly returned null for File ID {FileId}.", request.FileId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "An unexpected error occurred while generating image tags." });
        }

        // Return OK with the list (which might be empty if no tags were generated or an error occurred internally)
        return Ok(result);
    }


    // POST: api/largelanguagemodel/generate-metadata/url
    [HttpPost("generate-metadata/url")]
    [ProducesResponseType(typeof(ContentMetadata), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateMetadataFromUrl([FromBody] GenerateMetadataUrlRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("Received request to generate metadata from URL: {Url}", request.Url);
        // Use the renamed method from the refactored manager
        var result = await _llmManager.GenerateMetaDataFromUrlAsync(request.Url, request.MaxTags);

        if (result == null)
        {
            // Logged within the manager. Could be HTTP error fetching URL, parsing error, or AI error.
            _logger.LogWarning("Metadata generation from URL returned null for: {Url}", request.Url);
            // Return 500 as various issues could cause this.
            return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Failed to generate metadata from URL." });
        }

        return Ok(result);
    }

}