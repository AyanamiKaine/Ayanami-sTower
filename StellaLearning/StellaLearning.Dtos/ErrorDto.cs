namespace StellaLearning.Dtos;

/// <summary>
/// DTO for backend error responses (like validation errors).
/// </summary>
public class ErrorDto
{
    /// <summary>
    /// Gets or sets the main error message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the list of specific error messages.
    /// </summary>
    public List<string>? Errors { get; set; } // Assuming backend sends a list of error strings
}
