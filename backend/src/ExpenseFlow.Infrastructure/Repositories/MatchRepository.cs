using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IMatchRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class MatchRepository : IMatchRepository
{
    private readonly ExpenseFlowDbContext _context;

    public MatchRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<ReceiptTransactionMatch> AddAsync(ReceiptTransactionMatch match)
    {
        _context.ReceiptTransactionMatches.Add(match);
        await _context.SaveChangesAsync();
        return match;
    }

    public async Task<List<ReceiptTransactionMatch>> AddRangeAsync(IEnumerable<ReceiptTransactionMatch> matches)
    {
        var matchList = matches.ToList();
        _context.ReceiptTransactionMatches.AddRange(matchList);
        await _context.SaveChangesAsync();
        return matchList;
    }

    public async Task<ReceiptTransactionMatch?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.ReceiptTransactionMatches
            .Include(m => m.Receipt)
            .Include(m => m.Transaction)
            .Include(m => m.TransactionGroup)
            .Include(m => m.MatchedVendorAlias)
            .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId);
    }

    public async Task<ReceiptTransactionMatch?> GetByReceiptIdAsync(Guid receiptId, Guid userId)
    {
        return await _context.ReceiptTransactionMatches
            .Include(m => m.Receipt)
            .Include(m => m.Transaction)
            .Include(m => m.TransactionGroup)
            .Include(m => m.MatchedVendorAlias)
            .FirstOrDefaultAsync(m => m.ReceiptId == receiptId && m.UserId == userId);
    }

    public async Task<ReceiptTransactionMatch?> GetByTransactionIdAsync(Guid transactionId, Guid userId)
    {
        return await _context.ReceiptTransactionMatches
            .Include(m => m.Receipt)
            .Include(m => m.Transaction)
            .Include(m => m.TransactionGroup)
            .Include(m => m.MatchedVendorAlias)
            .FirstOrDefaultAsync(m => m.TransactionId == transactionId && m.UserId == userId);
    }

    public async Task<(List<ReceiptTransactionMatch> Items, int TotalCount)> GetProposedByUserIdAsync(
        Guid userId,
        int page = 1,
        int pageSize = 20)
    {
        var query = _context.ReceiptTransactionMatches
            .Include(m => m.Receipt)
            .Include(m => m.Transaction)
            .Include(m => m.TransactionGroup)
            .Include(m => m.MatchedVendorAlias)
            .Where(m => m.UserId == userId && m.Status == MatchProposalStatus.Proposed);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(m => m.ConfidenceScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<ReceiptTransactionMatch> UpdateAsync(ReceiptTransactionMatch match)
    {
        _context.ReceiptTransactionMatches.Update(match);
        await _context.SaveChangesAsync();
        return match;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var match = await GetByIdAsync(id, userId);
        if (match == null)
        {
            return false;
        }

        _context.ReceiptTransactionMatches.Remove(match);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Dictionary<MatchStatus, int>> GetStatusCountsAsync(Guid userId)
    {
        // Map MatchProposalStatus to MatchStatus for the return type
        var proposalCounts = await _context.ReceiptTransactionMatches
            .Where(m => m.UserId == userId)
            .GroupBy(m => m.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        // Convert to MatchStatus dictionary
        var result = new Dictionary<MatchStatus, int>
        {
            { MatchStatus.Proposed, proposalCounts.GetValueOrDefault(MatchProposalStatus.Proposed, 0) },
            { MatchStatus.Matched, proposalCounts.GetValueOrDefault(MatchProposalStatus.Confirmed, 0) }
        };

        return result;
    }

    public async Task<decimal> GetAverageConfidenceAsync(Guid userId)
    {
        // Use database-side aggregation instead of loading all records into memory
        return await _context.ReceiptTransactionMatches
            .Where(m => m.UserId == userId && m.Status == MatchProposalStatus.Proposed)
            .AverageAsync(m => (decimal?)m.ConfidenceScore) ?? 0m;
    }

    public async Task<bool> HasConfirmedMatchForReceiptAsync(Guid receiptId, Guid? userId = null)
    {
        var query = _context.ReceiptTransactionMatches
            .Where(m => m.ReceiptId == receiptId && m.Status == MatchProposalStatus.Confirmed);

        // Apply user filter if provided (for row-level security)
        if (userId.HasValue)
        {
            query = query.Where(m => m.UserId == userId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<bool> HasConfirmedMatchForTransactionAsync(Guid transactionId, Guid? userId = null)
    {
        var query = _context.ReceiptTransactionMatches
            .Where(m => m.TransactionId == transactionId && m.Status == MatchProposalStatus.Confirmed);

        // Apply user filter if provided (for row-level security)
        if (userId.HasValue)
        {
            query = query.Where(m => m.UserId == userId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<ReceiptTransactionMatch>> GetConfirmedByPeriodAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate)
    {
        return await _context.ReceiptTransactionMatches
            .Include(m => m.Receipt)
            .Include(m => m.Transaction)
            .Include(m => m.TransactionGroup)
                .ThenInclude(g => g!.Transactions)
            .Include(m => m.MatchedVendorAlias)
            .Where(m => m.UserId == userId && m.Status == MatchProposalStatus.Confirmed)
            .Where(m =>
                // For transaction matches, filter by transaction date
                (m.TransactionId != null && m.Transaction!.TransactionDate >= startDate && m.Transaction!.TransactionDate <= endDate) ||
                // For group matches, filter by group display date
                (m.TransactionGroupId != null && m.TransactionGroup!.DisplayDate >= startDate && m.TransactionGroup!.DisplayDate <= endDate))
            .OrderBy(m => m.TransactionId != null ? m.Transaction!.TransactionDate : m.TransactionGroup!.DisplayDate)
            .ThenBy(m => m.CreatedAt)
            .ToListAsync();
    }
}
