using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for extracting data from receipts using Azure Document Intelligence.
/// Supports both receipt and invoice models with automatic fallback.
/// </summary>
public interface IDocumentIntelligenceService
{
    /// <summary>
    /// Analyzes a receipt image/document and extracts structured data using the receipt model.
    /// </summary>
    /// <param name="documentStream">Stream containing the receipt image or PDF</param>
    /// <param name="contentType">MIME type of the document</param>
    /// <returns>Extracted receipt data with confidence scores</returns>
    Task<ReceiptExtractionResult> AnalyzeReceiptAsync(Stream documentStream, string contentType);

    /// <summary>
    /// Analyzes a document using the invoice model for B2B invoices and formal billing documents.
    /// </summary>
    /// <param name="documentStream">Stream containing the invoice image or PDF</param>
    /// <param name="contentType">MIME type of the document</param>
    /// <returns>Extracted invoice data mapped to receipt result format</returns>
    Task<ReceiptExtractionResult> AnalyzeInvoiceAsync(Stream documentStream, string contentType);

    /// <summary>
    /// Analyzes a document with automatic fallback: tries receipt model first,
    /// then invoice model if critical fields are missing or have low confidence.
    /// </summary>
    /// <param name="documentStream">Stream containing the document (copied internally, seekability not required)</param>
    /// <param name="contentType">MIME type of the document</param>
    /// <param name="fallbackConfidenceThreshold">Minimum confidence to skip fallback (default: 0.5, range: 0.0-1.0)</param>
    /// <returns>Best extraction result, potentially merged from both models</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when fallbackConfidenceThreshold is not between 0.0 and 1.0</exception>
    Task<ReceiptExtractionResult> AnalyzeWithFallbackAsync(
        Stream documentStream,
        string contentType,
        double fallbackConfidenceThreshold = 0.5);
}

/// <summary>
/// Result of receipt analysis from Document Intelligence.
/// </summary>
public class ReceiptExtractionResult
{
    /// <summary>
    /// Vendor/merchant name extracted from receipt.
    /// </summary>
    public string? VendorName { get; set; }

    /// <summary>
    /// Transaction date extracted from receipt.
    /// </summary>
    public DateTime? TransactionDate { get; set; }

    /// <summary>
    /// Total amount extracted from receipt.
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Tax amount extracted from receipt.
    /// </summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR).
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Line items extracted from the receipt.
    /// </summary>
    public List<ReceiptLineItem> LineItems { get; set; } = new();

    /// <summary>
    /// Confidence scores for each extracted field.
    /// </summary>
    public Dictionary<string, double> ConfidenceScores { get; set; } = new();

    /// <summary>
    /// Number of pages in the document.
    /// </summary>
    public int PageCount { get; set; } = 1;

    /// <summary>
    /// Which extraction model(s) were used to produce this result.
    /// Possible values: "receipt", "invoice", "receipt+invoice" (merged)
    /// </summary>
    public string ExtractionModel { get; set; } = "receipt";

    /// <summary>
    /// Tracks which model provided each field when fallback was used.
    /// Key: field name, Value: "receipt" or "invoice"
    /// </summary>
    public Dictionary<string, string> FieldSources { get; set; } = new();

    /// <summary>
    /// Overall confidence score (average of all field confidences).
    /// </summary>
    public double OverallConfidence => ConfidenceScores.Count > 0
        ? ConfidenceScores.Values.Average()
        : 0;

    /// <summary>
    /// Indicates if extraction requires manual review based on confidence threshold.
    /// </summary>
    public bool RequiresReview(double confidenceThreshold)
    {
        return OverallConfidence < confidenceThreshold;
    }
}
