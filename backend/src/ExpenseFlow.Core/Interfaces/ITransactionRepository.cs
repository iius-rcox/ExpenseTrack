using ExpenseFlow.Core.Entities;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for Transaction entity operations.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Gets a transaction by ID for a specific user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="transactionId">Transaction ID.</param>
    /// <returns>Transaction if found, null otherwise.</returns>
    Task<Transaction?> GetByIdAsync(Guid userId, Guid transactionId);

    /// <summary>
    /// Gets paginated transactions for a user with optional filters.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="matchStatus">Optional match status filter (supports: matched, pending, unmatched, missing-receipt).</param>
    /// <param name="importId">Optional import batch filter.</param>
    /// <param name="search">Optional text search on description (case-insensitive).</param>
    /// <param name="sortBy">Field to sort by (date, amount, description, merchant). Defaults to date.</param>
    /// <param name="sortOrder">Sort direction (asc or desc). Defaults to desc.</param>
    /// <param name="minAmount">Optional minimum amount filter.</param>
    /// <param name="maxAmount">Optional maximum amount filter.</param>
    /// <param name="hasPendingPrediction">Optional filter for transactions with pending expense predictions.</param>
    /// <returns>Tuple of transactions list, total count, and unmatched count.</returns>
    Task<(List<Transaction> Transactions, int TotalCount, int UnmatchedCount)> GetPagedAsync(
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
        bool? hasPendingPrediction = null);

    /// <summary>
    /// Checks if a duplicate transaction exists.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="duplicateHash">Duplicate detection hash.</param>
    /// <returns>True if duplicate exists.</returns>
    Task<bool> ExistsByDuplicateHashAsync(Guid userId, string duplicateHash);

    /// <summary>
    /// Adds multiple transactions in a batch.
    /// </summary>
    /// <param name="transactions">Transactions to add.</param>
    Task AddRangeAsync(IEnumerable<Transaction> transactions);

    /// <summary>
    /// Deletes a transaction.
    /// </summary>
    /// <param name="transaction">Transaction to delete.</param>
    Task DeleteAsync(Transaction transaction);

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();

    /// <summary>
    /// Gets unmatched transactions for a user within a date range (for draft report generation).
    /// Returns transactions that do not have a matched receipt.
    /// </summary>
    /// <param name="userId">User ID for row-level security</param>
    /// <param name="startDate">Start of the period (inclusive)</param>
    /// <param name="endDate">End of the period (inclusive)</param>
    /// <returns>List of unmatched transactions</returns>
    Task<List<Transaction>> GetUnmatchedByPeriodAsync(Guid userId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets transaction statistics for filter suggestions.
    /// Analyzes transaction data to provide intelligent filter suggestions.
    /// </summary>
    /// <param name="userId">User ID for row-level security.</param>
    /// <returns>Statistics object containing filter suggestion data.</returns>
    Task<TransactionStatistics> GetFilterSuggestionsDataAsync(Guid userId);
}

/// <summary>
/// Transaction statistics for generating filter suggestions.
/// </summary>
public class TransactionStatistics
{
    /// <summary>
    /// Total number of transactions for the user.
    /// </summary>
    public int TotalTransactions { get; set; }

    /// <summary>
    /// Earliest transaction date.
    /// </summary>
    public DateOnly? EarliestDate { get; set; }

    /// <summary>
    /// Most recent transaction date.
    /// </summary>
    public DateOnly? LatestDate { get; set; }

    /// <summary>
    /// Top merchants by transaction count.
    /// </summary>
    public List<MerchantStats> TopMerchants { get; set; } = new();

    /// <summary>
    /// Match status breakdown.
    /// </summary>
    public MatchStatusStats MatchStats { get; set; } = new();

    /// <summary>
    /// Amount distribution statistics.
    /// </summary>
    public AmountStats AmountStats { get; set; } = new();

    /// <summary>
    /// Recent activity periods.
    /// </summary>
    public List<DateRangeStats> RecentPeriods { get; set; } = new();
}

/// <summary>
/// Statistics for a merchant.
/// </summary>
public class MerchantStats
{
    public string Merchant { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Match status distribution.
/// </summary>
public class MatchStatusStats
{
    public int MatchedCount { get; set; }
    public int PendingCount { get; set; }
    public int UnmatchedCount { get; set; }
}

/// <summary>
/// Amount distribution statistics.
/// </summary>
public class AmountStats
{
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public decimal AverageAmount { get; set; }
    public int HighValueCount { get; set; } // $100+
    public int LowValueCount { get; set; } // <$25
}

/// <summary>
/// Statistics for a date range period.
/// </summary>
public class DateRangeStats
{
    public string PeriodName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TransactionCount { get; set; }
}
