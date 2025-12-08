using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for StatementImport entity operations.
/// </summary>
public interface IStatementImportRepository
{
    /// <summary>
    /// Gets a statement import by ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="importId">Import ID.</param>
    /// <returns>StatementImport if found, null otherwise.</returns>
    Task<StatementImport?> GetByIdAsync(Guid userId, Guid importId);

    /// <summary>
    /// Gets paginated import history for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>Tuple of imports list and total count.</returns>
    Task<(List<StatementImport> Imports, int TotalCount)> GetPagedAsync(Guid userId, int page, int pageSize);

    /// <summary>
    /// Adds a new statement import record.
    /// </summary>
    /// <param name="import">Import record to add.</param>
    Task<StatementImport> AddAsync(StatementImport import);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
