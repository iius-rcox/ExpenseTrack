namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Represents a consolidated receipt PDF export response.
/// </summary>
public class ReceiptPdfDto
{
    /// <summary>
    /// Suggested filename for the download (e.g., "2025-01-receipts.pdf").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// MIME content type for PDF files.
    /// </summary>
    public string ContentType { get; set; } = "application/pdf";

    /// <summary>
    /// Serialized PDF file contents.
    /// </summary>
    public byte[] FileContents { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Total number of pages in the PDF (includes receipts and placeholders).
    /// </summary>
    public int PageCount { get; set; }

    /// <summary>
    /// Number of placeholder pages generated for missing receipts.
    /// </summary>
    public int PlaceholderCount { get; set; }
}
