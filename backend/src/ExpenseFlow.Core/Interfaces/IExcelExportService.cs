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
}
