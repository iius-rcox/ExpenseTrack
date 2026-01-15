using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for Excel expense report export.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Generates an Excel file for an expense report matching the AP department template format.
    /// </summary>
    /// <param name="reportId">Report ID to export</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> GenerateExcelAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Generates an Excel file from edited expense line data without database persistence.
    /// Stateless export for the lightweight editable report workflow.
    /// </summary>
    /// <param name="request">Export request with period and edited lines</param>
    /// <param name="employeeName">User display name for header</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Excel file as byte array</returns>
    Task<byte[]> GenerateExcelFromPreviewAsync(
        ExportPreviewRequest request,
        string employeeName,
        CancellationToken ct = default);
}
