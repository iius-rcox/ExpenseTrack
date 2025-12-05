using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for reference data operations.
/// </summary>
public interface IReferenceDataService
{
    /// <summary>
    /// Gets all GL accounts, optionally filtered by active status.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active accounts.</param>
    /// <returns>List of GL accounts.</returns>
    Task<IReadOnlyList<GLAccount>> GetGLAccountsAsync(bool activeOnly = true);

    /// <summary>
    /// Gets all departments, optionally filtered by active status.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active departments.</param>
    /// <returns>List of departments.</returns>
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly = true);

    /// <summary>
    /// Gets all projects, optionally filtered by active status.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active projects.</param>
    /// <returns>List of projects.</returns>
    Task<IReadOnlyList<Project>> GetProjectsAsync(bool activeOnly = true);

    /// <summary>
    /// Syncs all reference data from external source.
    /// </summary>
    /// <returns>Number of records updated.</returns>
    Task<(int GLAccounts, int Departments, int Projects)> SyncAllAsync();
}
