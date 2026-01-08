namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Internal DTO for passing HTML content through the extraction pipeline.
/// Used to carry metadata about the HTML file being processed.
/// </summary>
public record HtmlExtractionRequestDto
{
    /// <summary>
    /// The raw HTML content to extract receipt data from.
    /// </summary>
    public required string HtmlContent { get; init; }

    /// <summary>
    /// Original filename of the uploaded HTML file (for logging/debugging).
    /// </summary>
    public string? SourceFilename { get; init; }

    /// <summary>
    /// Size of the HTML content in bytes.
    /// </summary>
    public int ContentLengthBytes { get; init; }
}
