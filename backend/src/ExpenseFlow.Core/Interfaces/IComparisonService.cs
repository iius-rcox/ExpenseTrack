using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for month-over-month spending comparison.
/// </summary>
public interface IComparisonService
{
    /// <summary>
    /// Compares spending between two periods for a user.
    /// Identifies new vendors, missing recurring charges, and significant changes.
    /// </summary>
    /// <param name="userId">User ID for filtering expense data</param>
    /// <param name="currentPeriod">Current period in YYYY-MM format</param>
    /// <param name="previousPeriod">Previous period in YYYY-MM format</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Comparison results with summary, new vendors, missing recurring, and significant changes</returns>
    Task<MonthlyComparisonDto> GetComparisonAsync(
        Guid userId,
        string currentPeriod,
        string previousPeriod,
        CancellationToken ct = default);
}
