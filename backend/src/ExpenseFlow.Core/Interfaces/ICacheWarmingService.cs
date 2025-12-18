using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for cache warming operations including historical data import.
/// </summary>
public interface ICacheWarmingService
{
    /// <summary>
    /// Imports historical expense data from an Excel file to populate caches.
    /// </summary>
    /// <param name="fileStream">The Excel file stream.</param>
    /// <param name="fileName">Original file name.</param>
    /// <param name="userId">User initiating the import.</param>
    /// <returns>The created import job.</returns>
    Task<ImportJob> ImportHistoricalDataAsync(Stream fileStream, string fileName, Guid userId);

    /// <summary>
    /// Gets details of a specific import job.
    /// </summary>
    /// <param name="jobId">Import job ID.</param>
    /// <returns>The import job or null if not found.</returns>
    Task<ImportJob?> GetImportJobAsync(Guid jobId);

    /// <summary>
    /// Gets a paginated list of import jobs for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Items per page.</param>
    /// <returns>Paginated list of import jobs and total count.</returns>
    Task<(List<ImportJob> Jobs, int TotalCount)> GetImportJobsAsync(
        Guid userId,
        ImportJobStatus? status,
        int page,
        int pageSize);

    /// <summary>
    /// Cancels a pending or processing import job.
    /// </summary>
    /// <param name="jobId">Import job ID.</param>
    /// <returns>True if cancelled, false if not cancellable.</returns>
    Task<bool> CancelImportJobAsync(Guid jobId);

    /// <summary>
    /// Gets paginated error details for an import job.
    /// </summary>
    /// <param name="jobId">Import job ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Errors per page.</param>
    /// <returns>List of errors and total count.</returns>
    Task<(List<ImportError> Errors, int TotalCount)> GetImportJobErrorsAsync(
        Guid jobId,
        int page,
        int pageSize);
}

/// <summary>
/// Represents an individual import error.
/// </summary>
public record ImportError(int LineNumber, string ErrorMessage, string? RawData);
