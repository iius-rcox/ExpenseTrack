using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents an uploaded receipt document with OCR extraction results.
/// </summary>
public class Receipt : BaseEntity
{
    /// <summary>Owner of the receipt (FK to Users)</summary>
    public Guid UserId { get; set; }

    /// <summary>Full URL to blob in Azure Storage</summary>
    public string BlobUrl { get; set; } = null!;

    /// <summary>URL to 200x200 thumbnail</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Original uploaded filename</summary>
    public string OriginalFilename { get; set; } = null!;

    /// <summary>MIME type (image/jpeg, application/pdf, etc.)</summary>
    public string ContentType { get; set; } = null!;

    /// <summary>File size in bytes</summary>
    public long FileSize { get; set; }

    /// <summary>Processing status</summary>
    public ReceiptStatus Status { get; set; } = ReceiptStatus.Uploaded;

    /// <summary>Extracted vendor/merchant name</summary>
    public string? VendorExtracted { get; set; }

    /// <summary>Extracted transaction date</summary>
    public DateOnly? DateExtracted { get; set; }

    /// <summary>Extracted total amount</summary>
    public decimal? AmountExtracted { get; set; }

    /// <summary>Extracted tax amount</summary>
    public decimal? TaxExtracted { get; set; }

    /// <summary>Currency code (ISO 4217)</summary>
    public string Currency { get; set; } = "USD";

    /// <summary>Array of extracted line items (JSONB)</summary>
    public List<ReceiptLineItem>? LineItems { get; set; }

    /// <summary>Field-level confidence scores (JSONB)</summary>
    public Dictionary<string, double>? ConfidenceScores { get; set; }

    /// <summary>Error description if extraction failed</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Number of extraction retry attempts</summary>
    public int RetryCount { get; set; }

    /// <summary>Number of pages in document</summary>
    public int PageCount { get; set; } = 1;

    /// <summary>Upload timestamp</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Extraction completion timestamp</summary>
    public DateTime? ProcessedAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}
