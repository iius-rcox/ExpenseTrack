using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for TravelPeriod entity operations.
/// </summary>
public interface ITravelPeriodRepository
{
    /// <summary>
    /// Gets a travel period by ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="travelPeriodId">Travel period ID.</param>
    /// <returns>Travel period if found, null otherwise.</returns>
    Task<TravelPeriod?> GetByIdAsync(Guid userId, Guid travelPeriodId);

    /// <summary>
    /// Gets paginated travel periods for a user with optional date filters.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <returns>Tuple of travel periods list and total count.</returns>
    Task<(List<TravelPeriod> TravelPeriods, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? startDate = null,
        DateOnly? endDate = null);

    /// <summary>
    /// Gets travel periods that overlap with a date range.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="startDate">Range start date.</param>
    /// <param name="endDate">Range end date.</param>
    /// <returns>List of overlapping travel periods.</returns>
    Task<List<TravelPeriod>> GetOverlappingAsync(Guid userId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets the travel period active on a specific date.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="date">Date to check.</param>
    /// <returns>Travel period if found, null otherwise.</returns>
    Task<TravelPeriod?> GetByDateAsync(Guid userId, DateOnly date);

    /// <summary>
    /// Adds a new travel period.
    /// </summary>
    /// <param name="travelPeriod">Travel period to add.</param>
    Task AddAsync(TravelPeriod travelPeriod);

    /// <summary>
    /// Updates an existing travel period.
    /// </summary>
    /// <param name="travelPeriod">Travel period to update.</param>
    Task UpdateAsync(TravelPeriod travelPeriod);

    /// <summary>
    /// Deletes a travel period.
    /// </summary>
    /// <param name="travelPeriod">Travel period to delete.</param>
    Task DeleteAsync(TravelPeriod travelPeriod);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
