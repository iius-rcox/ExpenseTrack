namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for exporting analytics data to various file formats.
/// </summary>
public interface IAnalyticsExportService
{
    /// <summary>
    /// Exports analytics data for the specified date range and sections.
    /// </summary>
    /// <param name="userId">User ID for data ownership verification</param>
    /// <param name="startDate">Start date of the export range</param>
    /// <param name="endDate">End date of the export range</param>
    /// <param name="format">Export format: "csv" or "xlsx"</param>
    /// <param name="sections">Sections to include: trends, categories, vendors, transactions</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple containing file bytes, content type, and suggested filename</returns>
    Task<(byte[] FileBytes, string ContentType, string FileName)> ExportAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        string format,
        IReadOnlyList<string> sections,
        CancellationToken ct = default);
}
