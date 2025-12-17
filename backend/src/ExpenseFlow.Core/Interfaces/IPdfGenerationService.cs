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
}
