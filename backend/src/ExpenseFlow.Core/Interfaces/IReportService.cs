using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for expense report operations including draft generation and editing.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generates a draft expense report for a specific period.
    /// Includes matched receipt-transaction pairs and unmatched transactions.
    /// Pre-populates GL codes and departments using tiered categorization.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="period">Period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Generated draft report with all lines</returns>
    Task<ExpenseReportDto> GenerateDraftAsync(Guid userId, string period, CancellationToken ct = default);

    /// <summary>
    /// Gets a report by ID with all expense lines.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report with lines if found and belongs to user, null otherwise</returns>
    Task<ExpenseReportDto?> GetByIdAsync(Guid userId, Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Gets paginated list of reports for a user.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="period">Optional period filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated response with report summaries</returns>
    Task<ReportListResponse> GetListAsync(
        Guid userId,
        ReportStatus? status,
        string? period,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an expense line within a report.
    /// Triggers learning loop when user modifies GL code or department.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID</param>
    /// <param name="lineId">Line ID to update</param>
    /// <param name="request">Update request with changed fields</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated expense line, or null if not found</returns>
    Task<ExpenseLineDto?> UpdateLineAsync(
        Guid userId,
        Guid reportId,
        Guid lineId,
        UpdateLineRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a report.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted, false if not found or not owned by user</returns>
    Task<bool> DeleteAsync(Guid userId, Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a draft report already exists for the user and period.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="period">Period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Existing draft report ID if found, null otherwise</returns>
    Task<Guid?> GetExistingDraftIdAsync(Guid userId, string period, CancellationToken ct = default);
}
