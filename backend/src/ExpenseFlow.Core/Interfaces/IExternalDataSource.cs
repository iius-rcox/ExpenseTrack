using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Interface for external data source (SQL Server) operations.
/// </summary>
public interface IExternalDataSource
{
    /// <summary>
    /// Fetches all GL accounts from the external source.
    /// </summary>
    /// <returns>List of GL accounts.</returns>
    Task<IReadOnlyList<GLAccount>> GetGLAccountsAsync();

    /// <summary>
    /// Fetches all departments from the external source.
    /// </summary>
    /// <returns>List of departments.</returns>
    Task<IReadOnlyList<Department>> GetDepartmentsAsync();

    /// <summary>
    /// Fetches all projects from the external source.
    /// </summary>
    /// <returns>List of projects.</returns>
    Task<IReadOnlyList<Project>> GetProjectsAsync();

    /// <summary>
    /// Tests the connection to the external data source.
    /// </summary>
    /// <returns>True if connection is successful.</returns>
    Task<bool> TestConnectionAsync();
}
