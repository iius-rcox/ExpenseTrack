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

    /// <summary>
    /// Validates a report before generation (finalization).
    /// Checks: at least one line, each line has category, amount > 0, and receipt.
    /// </summary>
    /// <param name="reportId">Report ID to validate</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Validation result with errors and warnings</returns>
    Task<ReportValidationResultDto> ValidateReportAsync(Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Finalizes a draft report, changing status to Generated.
    /// Report must pass validation first. After generation, report becomes immutable.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID to finalize</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with reportId, status, generatedAt timestamp, lineCount, and totalAmount</returns>
    Task<GenerateReportResponseDto> GenerateAsync(Guid userId, Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Submits a generated report for tracking/audit purposes.
    /// Report must be in Generated status.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID to submit</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Result with reportId, status, and submittedAt timestamp</returns>
    Task<SubmitReportResponseDto> SubmitAsync(Guid userId, Guid reportId, CancellationToken ct = default);

    /// <summary>
    /// Gets a preview of expense lines that would be included in a report for a given period.
    /// Does not create a report, only returns what would be included.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="period">Period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of expense line DTOs that would be in the report</returns>
    Task<List<ExpenseLineDto>> GetPreviewAsync(Guid userId, string period, CancellationToken ct = default);

    /// <summary>
    /// Adds a transaction as a new expense line to a report.
    /// Enforces transaction exclusivity - a transaction can only be on one report at a time.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID to add the line to</param>
    /// <param name="request">Request containing transaction ID and optional categorization</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created expense line DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown if report not found, not a draft, or transaction already on another report</exception>
    Task<ExpenseLineDto> AddLineAsync(Guid userId, Guid reportId, AddLineRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes an expense line from a report.
    /// If the line is a split parent, all child allocations are removed (cascade delete).
    /// The underlying transaction is returned to the available pool.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID</param>
    /// <param name="lineId">Line ID to remove</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if removed, false if not found</returns>
    /// <exception cref="InvalidOperationException">Thrown if report not found or not a draft</exception>
    Task<bool> RemoveLineAsync(Guid userId, Guid reportId, Guid lineId, CancellationToken ct = default);

    /// <summary>
    /// Gets transactions available to add to a report.
    /// Excludes transactions already on any active report for the user.
    /// Includes IsOutsidePeriod flag for transactions outside the report period.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID (to determine period and exclude existing lines)</param>
    /// <param name="search">Optional search term for filtering</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with available transactions and metadata</returns>
    Task<AvailableTransactionsResponse> GetAvailableTransactionsAsync(
        Guid userId,
        Guid reportId,
        string? search,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Batch updates multiple expense lines in a report.
    /// Used by the Save button to persist all dirty lines at once.
    /// CRITICAL: This operation keeps the report in Draft status - it does NOT finalize/lock the report.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID</param>
    /// <param name="request">Batch update request containing line updates</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with update counts and any failures</returns>
    /// <exception cref="InvalidOperationException">Thrown if report not found, not owned by user, or not in Draft status</exception>
    Task<BatchUpdateLinesResponse> BatchUpdateLinesAsync(
        Guid userId,
        Guid reportId,
        BatchUpdateLinesRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Unlocks a submitted report, returning it to Draft status for editing.
    /// Only Submitted reports can be unlocked. The submittedAt timestamp is cleared.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="reportId">Report ID to unlock</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Response with reportId, new status (Draft), and unlockedAt timestamp</returns>
    /// <exception cref="InvalidOperationException">Thrown if report not found, not owned by user, or not in Submitted status</exception>
    Task<UnlockReportResponseDto> UnlockAsync(Guid userId, Guid reportId, CancellationToken ct = default);
}
