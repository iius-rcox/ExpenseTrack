namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Captures extraction metrics for HTML receipts for logging and debugging.
/// Used to support operational monitoring and the "flywheel" improvement process
/// where failed extractions inform prompt improvements.
/// </summary>
public record HtmlExtractionMetricsDto
{
    /// <summary>
    /// Receipt ID this extraction was performed for.
    /// </summary>
    public Guid ReceiptId { get; init; }

    /// <summary>
    /// Timestamp when extraction was performed.
    /// </summary>
    public DateTime ExtractedAt { get; init; }

    /// <summary>
    /// Total time spent on extraction processing.
    /// </summary>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Whether the extraction succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Overall confidence score for the extraction (0.0 - 1.0).
    /// </summary>
    public double? OverallConfidence { get; init; }

    /// <summary>
    /// Per-field confidence scores (e.g., "vendor" -> 0.95, "date" -> 0.87).
    /// </summary>
    public Dictionary<string, double> FieldConfidences { get; init; } = new();

    /// <summary>
    /// Size of the original HTML content in bytes.
    /// </summary>
    public int HtmlSizeBytes { get; init; }

    /// <summary>
    /// Length of extracted text content (after stripping HTML tags).
    /// </summary>
    public int TextContentLength { get; init; }

    /// <summary>
    /// Number of fields successfully extracted.
    /// </summary>
    public int FieldsExtracted { get; init; }

    /// <summary>
    /// Error message if extraction failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Error type/category for classification (e.g., "Timeout", "MalformedHtml", "NoReceiptData").
    /// </summary>
    public string? ErrorType { get; init; }

    /// <summary>
    /// Blob storage path where raw HTML is stored for failed/low-confidence extractions.
    /// </summary>
    public string? RawHtmlBlobPath { get; init; }

    /// <summary>
    /// The extraction prompt version used (from configuration).
    /// </summary>
    public string? PromptVersion { get; init; }
}
