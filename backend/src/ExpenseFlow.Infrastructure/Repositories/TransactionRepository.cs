using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
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
        bool? matched = null,
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

        if (matched.HasValue)
        {
            query = matched.Value
                ? query.Where(t => t.MatchedReceiptId != null)
                : query.Where(t => t.MatchedReceiptId == null);
        }

        if (importId.HasValue)
        {
            query = query.Where(t => t.ImportId == importId.Value);
        }

        // Apply text search on description (case-insensitive)
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => EF.Functions.ILike(t.Description, $"%{search}%"));
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
                .Where(p => p.UserId == userId && p.Status == ExpenseFlow.Shared.Enums.PredictionStatus.Pending)
                .Select(p => p.TransactionId);
            query = query.Where(t => transactionIdsWithPendingPredictions.Contains(t.Id));
        }

        var totalCount = await query.CountAsync();

        // Get unmatched count (for all user transactions, not filtered)
        var unmatchedCount = await _context.Transactions
            .Where(t => t.UserId == userId && t.MatchedReceiptId == null)
            .CountAsync();

        // Apply sorting
        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        IOrderedQueryable<Transaction> orderedQuery = sortBy?.ToLowerInvariant() switch
        {
            "amount" => isDescending
                ? query.OrderByDescending(t => t.Amount)
                : query.OrderBy(t => t.Amount),
            "description" => isDescending
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
            .Where(t => t.TransactionDate >= startDate && t.TransactionDate <= endDate)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();
    }
}
