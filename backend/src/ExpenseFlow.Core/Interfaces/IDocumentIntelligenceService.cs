using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for extracting data from receipts using Azure Document Intelligence.
/// </summary>
public interface IDocumentIntelligenceService
{
    /// <summary>
    /// Analyzes a receipt image/document and extracts structured data.
    /// </summary>
    /// <param name="documentStream">Stream containing the receipt image or PDF</param>
    /// <param name="contentType">MIME type of the document</param>
    /// <returns>Extracted receipt data with confidence scores</returns>
    Task<ReceiptExtractionResult> AnalyzeReceiptAsync(Stream documentStream, string contentType);
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
