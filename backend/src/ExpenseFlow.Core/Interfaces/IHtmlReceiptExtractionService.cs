using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for extracting receipt data from HTML content using AI/LLM.
/// Processes HTML receipts from email clients (Amazon, Uber, airline confirmations, etc.)
/// and extracts vendor, date, amount, and line items.
/// </summary>
public interface IHtmlReceiptExtractionService
{
    /// <summary>
    /// Extracts receipt data from HTML content using AI.
    /// </summary>
    /// <param name="htmlContent">Raw HTML content from a receipt email</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Extracted receipt data with confidence scores</returns>
    Task<ReceiptExtractionResult> ExtractAsync(
        string htmlContent,
        CancellationToken ct = default);

    /// <summary>
    /// Extracts receipt data and returns metrics for logging and debugging.
    /// Use this method when you need to capture extraction metrics for operational monitoring.
    /// </summary>
    /// <param name="htmlContent">Raw HTML content from a receipt email</param>
    /// <param name="receiptId">Receipt ID for correlation in metrics</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of extraction result and metrics for logging</returns>
    Task<(ReceiptExtractionResult Result, HtmlExtractionMetricsDto Metrics)> ExtractWithMetricsAsync(
        string htmlContent,
        Guid receiptId,
        CancellationToken ct = default);
}
