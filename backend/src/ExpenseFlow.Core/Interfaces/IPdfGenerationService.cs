using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for PDF receipt consolidation and generation.
/// </summary>
public interface IPdfGenerationService
{
    /// <summary>
    /// Generates a consolidated PDF containing all receipts for an expense report.
    /// Includes placeholder pages for missing receipts with justification details.
    /// </summary>
    /// <param name="reportId">Report ID to export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PDF result with file contents, page count, and placeholder count</returns>
    Task<ReceiptPdfDto> GenerateReceiptPdfAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Generates a summary PDF table from edited expense line data without database persistence.
    /// Creates a simple expense table (no receipt images) for lightweight export workflow.
    /// </summary>
    /// <param name="request">Export request with period and edited lines</param>
    /// <param name="employeeName">User display name for header</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateSummaryPdfAsync(
        ExportPreviewRequest request,
        string employeeName,
        CancellationToken ct = default);

    /// <summary>
    /// Generates a complete PDF report containing both the itemized expense list
    /// and all associated receipt images in a single document.
    /// </summary>
    /// <param name="reportId">Report ID to export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>PDF result with file contents, page count, and placeholder count</returns>
    Task<ReceiptPdfDto> GenerateCompleteReportPdfAsync(Guid reportId, CancellationToken ct = default);
}
