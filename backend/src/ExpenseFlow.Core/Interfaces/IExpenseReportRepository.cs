using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for ExpenseReport entity operations.
/// </summary>
public interface IExpenseReportRepository
{
    /// <summary>
    /// Gets a report by ID (without lines).
    /// </summary>
    /// <param name="id">Report ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report if found, null otherwise</returns>
    Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a report by ID with all expense lines.
    /// </summary>
    /// <param name="id">Report ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Report with lines if found, null otherwise</returns>
    Task<ExpenseReport?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets an active (non-deleted) draft by user and period.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="period">Period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Draft report if exists, null otherwise</returns>
    Task<ExpenseReport?> GetDraftByUserAndPeriodAsync(Guid userId, string period, CancellationToken ct = default);

    /// <summary>
    /// Gets reports for a user with optional filtering and pagination.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="period">Optional period filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of reports ordered by created date descending</returns>
    Task<List<ExpenseReport>> GetByUserAsync(Guid userId, ReportStatus? status, string? period, int page, int pageSize, CancellationToken ct = default);

    /// <summary>
    /// Gets count of reports for a user with optional filtering.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="status">Optional status filter</param>
    /// <param name="period">Optional period filter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Total count of matching reports</returns>
    Task<int> GetCountByUserAsync(Guid userId, ReportStatus? status, string? period, CancellationToken ct = default);

    /// <summary>
    /// Adds a new expense report to the database.
    /// </summary>
    /// <param name="report">Report to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Added report with generated ID</returns>
    Task<ExpenseReport> AddAsync(ExpenseReport report, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing expense report.
    /// </summary>
    /// <param name="report">Report to update</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateAsync(ExpenseReport report, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a report by ID.
    /// </summary>
    /// <param name="id">Report ID</param>
    /// <param name="ct">Cancellation token</param>
    Task SoftDeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets an expense line by ID within a report.
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="lineId">Line ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Expense line if found, null otherwise</returns>
    Task<ExpenseLine?> GetLineByIdAsync(Guid reportId, Guid lineId, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing expense line.
    /// </summary>
    /// <param name="line">Line to update</param>
    /// <param name="ct">Cancellation token</param>
    Task UpdateLineAsync(ExpenseLine line, CancellationToken ct = default);
}
