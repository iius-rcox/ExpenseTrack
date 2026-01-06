using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for managing missing receipts.
/// Handles queries and updates for transactions marked as reimbursable but lacking matched receipts.
/// </summary>
public class MissingReceiptService : IMissingReceiptService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<MissingReceiptService> _logger;

    public MissingReceiptService(
        ExpenseFlowDbContext dbContext,
        ILogger<MissingReceiptService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MissingReceiptsListResponseDto> GetMissingReceiptsAsync(
        Guid userId,
        int page = 1,
        int pageSize = 25,
        string sortBy = "date",
        string sortOrder = "desc",
        bool includeDismissed = false,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting missing receipts for user {UserId}, page {Page}, pageSize {PageSize}, " +
            "sortBy {SortBy}, sortOrder {SortOrder}, includeDismissed {IncludeDismissed}",
            userId, page, pageSize, sortBy, sortOrder, includeDismissed);

        // Validate pagination
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Base query for missing receipts
        var query = GetMissingReceiptsQuery(userId, includeDismissed);

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Apply sorting
        var orderedQuery = ApplySorting(query, sortBy, sortOrder);

        // Apply pagination
        var items = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        _logger.LogDebug(
            "Returning {ItemCount} missing receipts for user {UserId} (page {Page} of {TotalPages}, total {TotalCount})",
            items.Count, userId, page, totalPages, totalCount);

        return new MissingReceiptsListResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages
        };
    }

    /// <inheritdoc />
    public async Task<MissingReceiptsWidgetDto> GetWidgetDataAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Getting missing receipts widget data for user {UserId}", userId);

        // Base query (exclude dismissed)
        var query = GetMissingReceiptsQuery(userId, includeDismissed: false);

        // Get count and top 3 by date descending
        var totalCount = await query.CountAsync(ct);
        var recentItems = await query
            .OrderByDescending(m => m.TransactionDate)
            .Take(3)
            .ToListAsync(ct);

        _logger.LogDebug(
            "Widget data for user {UserId}: {TotalCount} total, {RecentCount} recent items",
            userId, totalCount, recentItems.Count);

        return new MissingReceiptsWidgetDto
        {
            TotalCount = totalCount,
            RecentItems = recentItems
        };
    }

    /// <inheritdoc />
    public async Task<MissingReceiptSummaryDto?> UpdateReceiptUrlAsync(
        Guid userId,
        Guid transactionId,
        string? receiptUrl,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Updating receipt URL for transaction {TransactionId}, user {UserId}, URL: {HasUrl}",
            transactionId, userId, !string.IsNullOrEmpty(receiptUrl));

        // Find the transaction (must belong to user and be a missing receipt candidate)
        var transaction = await _dbContext.Transactions
            .Where(t => t.Id == transactionId && t.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (transaction == null)
        {
            _logger.LogWarning(
                "Transaction {TransactionId} not found for user {UserId}",
                transactionId, userId);
            return null;
        }

        // Update the URL
        transaction.ReceiptUrl = string.IsNullOrWhiteSpace(receiptUrl) ? null : receiptUrl;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated receipt URL for transaction {TransactionId}",
            transactionId);

        // Return updated summary
        return await GetTransactionSummaryAsync(userId, transactionId, ct);
    }

    /// <inheritdoc />
    public async Task<MissingReceiptSummaryDto?> DismissTransactionAsync(
        Guid userId,
        Guid transactionId,
        bool? dismiss,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Dismissing transaction {TransactionId} for user {UserId}, dismiss: {Dismiss}",
            transactionId, userId, dismiss);

        // Find the transaction
        var transaction = await _dbContext.Transactions
            .Where(t => t.Id == transactionId && t.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (transaction == null)
        {
            _logger.LogWarning(
                "Transaction {TransactionId} not found for user {UserId}",
                transactionId, userId);
            return null;
        }

        // Update the dismiss status (null/false to restore, true to dismiss)
        transaction.ReceiptDismissed = dismiss == true ? true : null;
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated dismiss status for transaction {TransactionId} to {Dismissed}",
            transactionId, transaction.ReceiptDismissed);

        // Return updated summary
        return await GetTransactionSummaryAsync(userId, transactionId, ct);
    }

    #region Private Methods

    /// <summary>
    /// Builds the base query for missing receipts.
    /// A transaction is a missing receipt if:
    /// 1. User owns the transaction
    /// 2. No matched receipt (MatchedReceiptId is null)
    /// 3. Not dismissed (unless includeDismissed is true)
    /// 4. Has a confirmed prediction (user override or AI-confirmed)
    /// </summary>
    private IQueryable<MissingReceiptSummaryDto> GetMissingReceiptsQuery(
        Guid userId,
        bool includeDismissed)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Subquery: Get the best prediction per transaction (prefer IsManualOverride=true)
        // Using GroupBy with only aggregate functions ensures proper SQL translation
        var bestPredictions = _dbContext.TransactionPredictions
            .Where(p => p.UserId == userId && p.Status == PredictionStatus.Confirmed)
            .GroupBy(p => p.TransactionId)
            .Select(g => new
            {
                TransactionId = g.Key,
                IsManualOverride = g.Max(p => p.IsManualOverride)
            });

        // Main query: Join transactions with their best prediction
        return from t in _dbContext.Transactions
               join bp in bestPredictions on t.Id equals bp.TransactionId
               where t.UserId == userId
                  && t.MatchedReceiptId == null
                  && (includeDismissed || t.ReceiptDismissed != true)
               select new MissingReceiptSummaryDto
               {
                   TransactionId = t.Id,
                   TransactionDate = t.TransactionDate,
                   Description = t.Description,
                   Amount = t.Amount,
                   DaysSinceTransaction = today.DayNumber - t.TransactionDate.DayNumber,
                   ReceiptUrl = t.ReceiptUrl,
                   IsDismissed = t.ReceiptDismissed == true,
                   Source = bp.IsManualOverride
                       ? ReimbursabilitySource.UserOverride
                       : ReimbursabilitySource.AIPrediction
               };
    }

    /// <summary>
    /// Applies sorting to the query.
    /// </summary>
    private static IQueryable<MissingReceiptSummaryDto> ApplySorting(
        IQueryable<MissingReceiptSummaryDto> query,
        string sortBy,
        string sortOrder)
    {
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return sortBy.ToLowerInvariant() switch
        {
            "amount" => isDescending
                ? query.OrderByDescending(m => m.Amount)
                : query.OrderBy(m => m.Amount),
            "vendor" => isDescending
                ? query.OrderByDescending(m => m.Description)
                : query.OrderBy(m => m.Description),
            _ => isDescending // "date" is default
                ? query.OrderByDescending(m => m.TransactionDate)
                : query.OrderBy(m => m.TransactionDate)
        };
    }

    /// <summary>
    /// Gets a single transaction summary by ID.
    /// Uses a single query with left join to avoid N+1 problem.
    /// </summary>
    private async Task<MissingReceiptSummaryDto?> GetTransactionSummaryAsync(
        Guid userId,
        Guid transactionId,
        CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Single query: Join transaction with its best prediction (if any)
        var result = await (
            from t in _dbContext.Transactions
            where t.Id == transactionId && t.UserId == userId
            // Left join with predictions, preferring IsManualOverride
            join p in _dbContext.TransactionPredictions
                .Where(p => p.Status == PredictionStatus.Confirmed)
                on new { TransactionId = t.Id, t.UserId } equals new { p.TransactionId, p.UserId }
                into predictions
            from p in predictions.DefaultIfEmpty()
            group p by new
            {
                t.Id,
                t.TransactionDate,
                t.Description,
                t.Amount,
                t.ReceiptUrl,
                t.ReceiptDismissed
            } into g
            select new MissingReceiptSummaryDto
            {
                TransactionId = g.Key.Id,
                TransactionDate = g.Key.TransactionDate,
                Description = g.Key.Description,
                Amount = g.Key.Amount,
                DaysSinceTransaction = today.DayNumber - g.Key.TransactionDate.DayNumber,
                ReceiptUrl = g.Key.ReceiptUrl,
                IsDismissed = g.Key.ReceiptDismissed == true,
                Source = g.Max(p => p != null && p.IsManualOverride)
                    ? ReimbursabilitySource.UserOverride
                    : ReimbursabilitySource.AIPrediction
            }
        ).FirstOrDefaultAsync(ct);

        return result;
    }

    #endregion
}
