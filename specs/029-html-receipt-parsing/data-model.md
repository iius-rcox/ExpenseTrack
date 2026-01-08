# Data Model: HTML Receipt Parsing

**Feature**: 029-html-receipt-parsing
**Date**: 2026-01-08

## Entity Changes

### Receipt (Existing - Extended)

The existing `Receipt` entity requires no schema changes. The `ContentType` field already supports arbitrary MIME types.

| Field | Type | Change | Notes |
|-------|------|--------|-------|
| ContentType | string | No change | Will now accept `text/html` in addition to existing image/PDF types |

**New Allowed ContentType Values**:
- `text/html` - HTML receipt files from email clients

### ReceiptExtractionResult (Existing - No Changes)

The existing `ReceiptExtractionResult` class (defined in `IDocumentIntelligenceService.cs`) is reused for HTML extraction output:

```csharp
public class ReceiptExtractionResult
{
    public string? VendorName { get; set; }
    public DateTime? TransactionDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? TaxAmount { get; set; }
    public string? Currency { get; set; }
    public List<ReceiptLineItem> LineItems { get; set; }
    public Dictionary<string, double> ConfidenceScores { get; set; }
    public int PageCount { get; set; }
    public double OverallConfidence => /* calculated */
}
```

## New DTOs

### HtmlExtractionMetricsDto

Captures extraction metrics for logging and debugging (FR-014).

```csharp
public record HtmlExtractionMetricsDto
{
    public Guid ReceiptId { get; init; }
    public DateTime ExtractedAt { get; init; }
    public TimeSpan ProcessingTime { get; init; }

    // Extraction results
    public bool Success { get; init; }
    public double? OverallConfidence { get; init; }
    public Dictionary<string, double> FieldConfidences { get; init; } = new();

    // Content info
    public int HtmlSizeBytes { get; init; }
    public int TextContentLength { get; init; }
    public int FieldsExtracted { get; init; }

    // Error tracking
    public string? ErrorMessage { get; init; }
    public string? ErrorType { get; init; }

    // For re-processing
    public string? RawHtmlBlobPath { get; init; }
    public string? PromptUsed { get; init; }
}
```

### HtmlExtractionRequestDto

Internal DTO for passing HTML content through the extraction pipeline.

```csharp
public record HtmlExtractionRequestDto
{
    public required string HtmlContent { get; init; }
    public string? SourceFilename { get; init; }
    public int ContentLengthBytes { get; init; }
}
```

## New Interfaces

### IHtmlReceiptExtractionService

```csharp
public interface IHtmlReceiptExtractionService
{
    /// <summary>
    /// Extracts receipt data from HTML content using AI.
    /// </summary>
    Task<ReceiptExtractionResult> ExtractAsync(
        string htmlContent,
        CancellationToken ct = default);

    /// <summary>
    /// Extracts receipt data and returns metrics for logging.
    /// </summary>
    Task<(ReceiptExtractionResult Result, HtmlExtractionMetricsDto Metrics)> ExtractWithMetricsAsync(
        string htmlContent,
        Guid receiptId,
        CancellationToken ct = default);
}
```

### IHtmlSanitizationService

```csharp
public interface IHtmlSanitizationService
{
    /// <summary>
    /// Sanitizes HTML content for safe display in browser.
    /// Removes scripts, event handlers, and external resource references.
    /// </summary>
    string Sanitize(string htmlContent);

    /// <summary>
    /// Extracts plain text from HTML for AI processing.
    /// </summary>
    string ExtractText(string htmlContent);
}
```

### IHtmlThumbnailService

```csharp
public interface IHtmlThumbnailService
{
    /// <summary>
    /// Generates a thumbnail image from HTML content.
    /// </summary>
    Task<Stream> GenerateThumbnailAsync(
        string htmlContent,
        int width = 200,
        int height = 200,
        CancellationToken ct = default);

    /// <summary>
    /// Checks if the service is available (Chromium installed).
    /// </summary>
    Task<bool> IsAvailableAsync();
}
```

## Configuration Schema

New configuration section in `appsettings.json`:

```json
{
  "ReceiptProcessing": {
    "AllowedContentTypes": [
      "image/jpeg",
      "image/png",
      "image/heic",
      "image/heif",
      "application/pdf",
      "text/html"
    ],
    "Html": {
      "MaxSizeBytes": 5242880,
      "ExtractionPromptVersion": "1.0",
      "ThumbnailViewportWidth": 800,
      "ThumbnailViewportHeight": 600,
      "StoreFailedExtractions": true,
      "ConfidenceThreshold": 0.60
    }
  }
}
```

## State Transitions

HTML receipts follow the same state machine as image/PDF receipts:

```
Uploaded → Processing → Ready | ReviewRequired | Error
```

| State | Condition |
|-------|-----------|
| Uploaded | HTML file saved to blob storage |
| Processing | AI extraction in progress |
| Ready | Extraction complete, confidence ≥ threshold |
| ReviewRequired | Extraction complete, confidence < threshold |
| Error | Extraction failed (malformed HTML, AI error, timeout) |

## Validation Rules

| Rule | Applied To | Validation |
|------|------------|------------|
| Max file size | Upload | ≤ 5MB |
| Content type | Upload | Must be `text/html` for .html/.htm files |
| Magic bytes | Upload | Must start with `<!DOCTYPE`, `<html`, or `<?xml` |
| Non-empty content | Extraction | HTML must contain text content |
| Required fields | Extraction result | At least vendor OR amount must be extracted |
