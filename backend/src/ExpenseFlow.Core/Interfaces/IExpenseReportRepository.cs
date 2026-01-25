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

    /// <summary>
    /// Adds an expense line to a report.
    /// </summary>
    /// <param name="line">Line to add</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Added line with generated ID</returns>
    Task<ExpenseLine> AddLineAsync(ExpenseLine line, CancellationToken ct = default);

    /// <summary>
    /// Removes an expense line and its child allocations from a report.
    /// If the line is a split parent, all child allocations are also removed (cascade).
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="lineId">Line ID to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Tuple of (success, childCount) - childCount is the number of child lines removed if parent was split</returns>
    Task<(bool Success, int ChildCount)> RemoveLineAsync(Guid reportId, Guid lineId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a transaction is already on any active (non-deleted) report for the user.
    /// Used to enforce transaction exclusivity.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="transactionId">Transaction ID to check</param>
    /// <param name="excludeReportId">Optional report ID to exclude from check (for moves)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if transaction is on another report</returns>
    Task<bool> IsTransactionOnAnyReportAsync(Guid userId, Guid transactionId, Guid? excludeReportId = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the set of transaction IDs that are already on any active (non-deleted) report for the user.
    /// Used to efficiently filter available transactions without N+1 queries.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="transactionIds">Collection of transaction IDs to check</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>HashSet of transaction IDs that are already on reports</returns>
    Task<HashSet<Guid>> GetTransactionIdsOnReportsAsync(Guid userId, IEnumerable<Guid> transactionIds, CancellationToken ct = default);

    /// <summary>
    /// Gets the maximum line order for a report (for appending new lines).
    /// </summary>
    /// <param name="reportId">Report ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Maximum line order, or 0 if no lines exist</returns>
    Task<int> GetMaxLineOrderAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Gets child allocation lines for a split parent line.
    /// </summary>
    /// <param name="parentLineId">Parent line ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of child allocation lines</returns>
    Task<List<ExpenseLine>> GetChildAllocationsAsync(Guid parentLineId, CancellationToken ct = default);

    /// <summary>
    /// Deletes a single expense line by ID.
    /// Does not cascade to children - use for individual child allocation deletion.
    /// </summary>
    /// <param name="lineId">Line ID to delete</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteLineAsync(Guid lineId, CancellationToken ct = default);
}
