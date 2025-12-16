using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for detecting and managing travel periods from receipts.
/// </summary>
public interface ITravelDetectionService
{
    /// <summary>
    /// Detects travel period from a processed receipt.
    /// </summary>
    /// <param name="receipt">The receipt to analyze.</param>
    /// <returns>Detection result with created/updated travel period.</returns>
    Task<TravelDetectionResultDto> DetectFromReceiptAsync(Receipt receipt);

    /// <summary>
    /// Gets all travel periods for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <returns>Paginated list of travel periods.</returns>
    Task<TravelPeriodListResponseDto> GetTravelPeriodsAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? startDate = null,
        DateOnly? endDate = null);

    /// <summary>
    /// Gets a specific travel period by ID.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="travelPeriodId">Travel period ID.</param>
    /// <returns>Travel period details or null if not found.</returns>
    Task<TravelPeriodDetailDto?> GetTravelPeriodAsync(Guid userId, Guid travelPeriodId);

    /// <summary>
    /// Creates a manual travel period.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="request">Creation request.</param>
    /// <returns>Created travel period.</returns>
    Task<TravelPeriodDetailDto> CreateTravelPeriodAsync(Guid userId, CreateTravelPeriodRequestDto request);

    /// <summary>
    /// Updates an existing travel period.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="travelPeriodId">Travel period ID.</param>
    /// <param name="request">Update request.</param>
    /// <returns>Updated travel period or null if not found.</returns>
    Task<TravelPeriodDetailDto?> UpdateTravelPeriodAsync(Guid userId, Guid travelPeriodId, UpdateTravelPeriodRequestDto request);

    /// <summary>
    /// Deletes a travel period.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="travelPeriodId">Travel period ID.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteTravelPeriodAsync(Guid userId, Guid travelPeriodId);

    /// <summary>
    /// Gets expenses within a travel period.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="travelPeriodId">Travel period ID.</param>
    /// <returns>List of expenses within the period.</returns>
    Task<TravelExpenseListResponseDto> GetTravelExpensesAsync(Guid userId, Guid travelPeriodId);

    /// <summary>
    /// Gets the suggested GL code for a date (66300 if within travel period).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="date">Date to check.</param>
    /// <returns>Suggested GL code or null if not within travel period.</returns>
    Task<string?> GetSuggestedGLCodeForDateAsync(Guid userId, DateOnly date);

    /// <summary>
    /// Checks if a date falls within any travel period.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="date">Date to check.</param>
    /// <returns>True if within a travel period.</returns>
    Task<bool> IsWithinTravelPeriodAsync(Guid userId, DateOnly date);

    /// <summary>
    /// Gets a timeline view of travel periods with linked expenses.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="includeExpenses">Whether to include expense details (default true).</param>
    /// <returns>Timeline response with periods and expenses.</returns>
    Task<TravelTimelineResponseDto> GetTimelineAsync(
        Guid userId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool includeExpenses = true);
}
