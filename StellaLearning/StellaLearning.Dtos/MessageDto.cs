namespace StellaLearning.Dtos;

/// <summary>
/// Generic DTO for simple message responses from the backend (e.g., registration success).
/// </summary>
public class MessageDto
{
    /// <summary>
    /// Gets or sets the message content.
    /// </summary>
    public string? Message { get; set; }
}