using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of ITransactionRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly ExpenseFlowDbContext _context;

    public TransactionRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Escapes special characters in ILIKE patterns to prevent SQL injection.
    /// PostgreSQL ILIKE special chars: %, _, \ (backslash is the escape character)
    /// </summary>
    private static string EscapeILikePattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Escape backslash first (it's the escape character), then % and _
        return input
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
    }

    public async Task<Transaction?> GetByIdAsync(Guid userId, Guid transactionId)
    {
        return await _context.Transactions
            .Include(t => t.Import)
            .FirstOrDefaultAsync(t => t.Id == transactionId && t.UserId == userId);
    }

    public async Task<(List<Transaction> Transactions, int TotalCount, int UnmatchedCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        List<string>? matchStatus = null,
        Guid? importId = null,
        string? search = null,
        string? sortBy = null,
        string? sortOrder = null,
        decimal? minAmount = null,
        decimal? maxAmount = null,
        bool? hasPendingPrediction = null)
    {
        var query = _context.Transactions.Where(t => t.UserId == userId);

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.TransactionDate <= endDate.Value);
        }

        // Apply match status filter (supports multiple values with OR logic)
        if (matchStatus is { Count: > 0 })
        {
            // Map frontend status strings to query conditions
            var hasMatched = matchStatus.Contains("matched", StringComparer.OrdinalIgnoreCase);
            var hasPending = matchStatus.Contains("pending", StringComparer.OrdinalIgnoreCase);
            var hasUnmatched = matchStatus.Contains("unmatched", StringComparer.OrdinalIgnoreCase);

            if (hasMatched || hasPending || hasUnmatched)
            {
                query = query.Where(t =>
                    // Matched: MatchStatus == Matched (2)
                    (hasMatched && t.MatchStatus == Shared.Enums.MatchStatus.Matched) ||
                    // Pending/Proposed: MatchStatus == Proposed (1)
                    (hasPending && t.MatchStatus == Shared.Enums.MatchStatus.Proposed) ||
                    // Unmatched: MatchStatus == Unmatched (0) AND not in a group
                    (hasUnmatched && t.MatchStatus == Shared.Enums.MatchStatus.Unmatched && t.GroupId == null));
            }
        }

        if (importId.HasValue)
        {
            query = query.Where(t => t.ImportId == importId.Value);
        }

        // Apply text search on description (case-insensitive)
        // Escape ILIKE special characters to prevent pattern injection
        if (!string.IsNullOrWhiteSpace(search))
        {
            var escapedSearch = EscapeILikePattern(search);
            query = query.Where(t => EF.Functions.ILike(t.Description, $"%{escapedSearch}%"));
        }

        // Apply amount range filters
        if (minAmount.HasValue)
        {
            query = query.Where(t => t.Amount >= minAmount.Value);
        }

        if (maxAmount.HasValue)
        {
            query = query.Where(t => t.Amount <= maxAmount.Value);
        }

        // Apply pending prediction filter (Feature 023)
        if (hasPendingPrediction == true)
        {
            var transactionIdsWithPendingPredictions = _context.TransactionPredictions
                .Where(p => p.UserId == userId && p.Status == Shared.Enums.PredictionStatus.Pending)
                .Select(p => p.TransactionId);
            query = query.Where(t => transactionIdsWithPendingPredictions.Contains(t.Id));
        }

        var totalCount = await query.CountAsync();

        // Get unmatched count (for all user transactions, not filtered)
        // Exclude grouped transactions - their matching is handled at group level
        var unmatchedCount = await _context.Transactions
            .Where(t => t.UserId == userId && t.MatchedReceiptId == null && t.GroupId == null)
            .CountAsync();

        // Apply sorting - map frontend field names to entity properties
        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<Transaction> orderedQuery = sortBy?.ToLowerInvariant() switch
        {
            "amount" => isDescending
                ? query.OrderByDescending(t => t.Amount)
                : query.OrderBy(t => t.Amount),
            "description" or "merchant" => isDescending  // Map 'merchant' to Description (Fix #2)
                ? query.OrderByDescending(t => t.Description)
                : query.OrderBy(t => t.Description),
            _ => isDescending
                ? query.OrderByDescending(t => t.TransactionDate)
                : query.OrderBy(t => t.TransactionDate)
        };

        // Secondary sort by CreatedAt for stable ordering
        orderedQuery = isDescending
            ? orderedQuery.ThenByDescending(t => t.CreatedAt)
            : orderedQuery.ThenBy(t => t.CreatedAt);

        var transactions = await orderedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (transactions, totalCount, unmatchedCount);
    }

    public async Task<bool> ExistsByDuplicateHashAsync(Guid userId, string duplicateHash)
    {
        return await _context.Transactions
            .AnyAsync(t => t.UserId == userId && t.DuplicateHash == duplicateHash);
    }

    public async Task AddRangeAsync(IEnumerable<Transaction> transactions)
    {
        await _context.Transactions.AddRangeAsync(transactions);
    }

    public async Task DeleteAsync(Transaction transaction)
    {
        _context.Transactions.Remove(transaction);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<List<Transaction>> GetUnmatchedByPeriodAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate)
    {
        return await _context.Transactions
            .Where(t => t.UserId == userId)
            .Where(t => t.MatchedReceiptId == null)
            // Exclude transactions that are part of a group - their matching is handled at group level
            .Where(t => t.GroupId == null)
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            // CRITICAL: Only include reimbursable transactions (have prediction that's not rejected)
            .Where(t => _context.TransactionPredictions.Any(p => p.TransactionId == t.Id && p.Status != PredictionStatus.Rejected))
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<TransactionStatistics> GetFilterSuggestionsDataAsync(Guid userId)
    {
        var userTransactions = _context.Transactions.Where(t => t.UserId == userId);

        // Get total count and date range
        var totalCount = await userTransactions.CountAsync();

        if (totalCount == 0)
        {
            return new TransactionStatistics { TotalTransactions = 0 };
        }

        // Get date range
        var earliestDate = await userTransactions.MinAsync(t => t.TransactionDate);
        var latestDate = await userTransactions.MaxAsync(t => t.TransactionDate);

        // Get top merchants by transaction count (normalize description to extract merchant-like patterns)
        var topMerchants = await userTransactions
            .GroupBy(t => t.Description)
            .Select(g => new MerchantStats
            {
                Merchant = g.Key,
                TransactionCount = g.Count(),
                TotalAmount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(m => m.TransactionCount)
            .Take(10)
            .ToListAsync();

        // Get match status breakdown
        var matchStats = new MatchStatusStats
        {
            MatchedCount = await userTransactions.CountAsync(t => t.MatchStatus == Shared.Enums.MatchStatus.Matched),
            PendingCount = await userTransactions.CountAsync(t => t.MatchStatus == Shared.Enums.MatchStatus.Proposed),
            UnmatchedCount = await userTransactions.CountAsync(t => t.MatchStatus == Shared.Enums.MatchStatus.Unmatched && t.GroupId == null)
        };

        // Get amount statistics
        var amountStats = new AmountStats
        {
            MinAmount = await userTransactions.MinAsync(t => t.Amount),
            MaxAmount = await userTransactions.MaxAsync(t => t.Amount),
            AverageAmount = await userTransactions.AverageAsync(t => t.Amount),
            HighValueCount = await userTransactions.CountAsync(t => t.Amount >= 100),
            LowValueCount = await userTransactions.CountAsync(t => t.Amount < 25)
        };

        // Calculate recent periods for suggestions
        var today = DateOnly.FromDateTime(DateTime.Today);
        var periods = new List<DateRangeStats>();

        // This week
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var thisWeekCount = await userTransactions
            .CountAsync(t => t.TransactionDate >= weekStart && t.TransactionDate <= today);
        if (thisWeekCount > 0)
        {
            periods.Add(new DateRangeStats
            {
                PeriodName = "This Week",
                StartDate = weekStart,
                EndDate = today,
                TransactionCount = thisWeekCount
            });
        }

        // This month
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var thisMonthCount = await userTransactions
            .CountAsync(t => t.TransactionDate >= monthStart && t.TransactionDate <= today);
        if (thisMonthCount > 0)
        {
            periods.Add(new DateRangeStats
            {
                PeriodName = "This Month",
                StartDate = monthStart,
                EndDate = today,
                TransactionCount = thisMonthCount
            });
        }

        // Last 30 days
        var thirtyDaysAgo = today.AddDays(-30);
        var last30DaysCount = await userTransactions
            .CountAsync(t => t.TransactionDate >= thirtyDaysAgo && t.TransactionDate <= today);
        if (last30DaysCount > 0)
        {
            periods.Add(new DateRangeStats
            {
                PeriodName = "Last 30 Days",
                StartDate = thirtyDaysAgo,
                EndDate = today,
                TransactionCount = last30DaysCount
            });
        }

        return new TransactionStatistics
        {
            TotalTransactions = totalCount,
            EarliestDate = earliestDate,
            LatestDate = latestDate,
            TopMerchants = topMerchants,
            MatchStats = matchStats,
            AmountStats = amountStats,
            RecentPeriods = periods
        };
    }
}
