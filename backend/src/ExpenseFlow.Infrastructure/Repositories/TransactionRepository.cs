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
        Guid? importId = null)
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

        var totalCount = await query.CountAsync();

        // Get unmatched count (for all user transactions, not filtered)
        var unmatchedCount = await _context.Transactions
            .Where(t => t.UserId == userId && t.MatchedReceiptId == null)
            .CountAsync();

        var transactions = await query
            .OrderByDescending(t => t.TransactionDate)
            .ThenByDescending(t => t.CreatedAt)
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
}
