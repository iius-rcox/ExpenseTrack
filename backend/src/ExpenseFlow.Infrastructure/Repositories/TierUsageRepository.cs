using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of ITierUsageRepository.
/// </summary>
public class TierUsageRepository : ITierUsageRepository
{
    private readonly ExpenseFlowDbContext _context;

    public TierUsageRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(TierUsageLog log)
    {
        await _context.TierUsageLogs.AddAsync(log);
    }

    public async Task<IReadOnlyList<TierUsageAggregate>> GetAggregateAsync(
        DateTime startDate,
        DateTime endDate,
        string? operationType = null)
    {
        var query = _context.TierUsageLogs
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate);

        if (!string.IsNullOrEmpty(operationType))
        {
            query = query.Where(t => t.OperationType == operationType);
        }

        var aggregates = await query
            .GroupBy(t => new { t.OperationType, t.TierUsed })
            .Select(g => new TierUsageAggregate(
                g.Key.OperationType,
                g.Key.TierUsed,
                g.Count(),
                g.Average(t => t.Confidence),
                (int?)g.Average(t => t.ResponseTimeMs)))
            .ToListAsync();

        return aggregates;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<VendorTier3Usage>> GetVendorTier3UsageAsync(
        DateTime startDate,
        DateTime endDate,
        int minTier3Count = 5)
    {
        // Query Tier 3 usage logs joined with their transactions to get vendor descriptions
        var candidates = await _context.TierUsageLogs
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .Where(t => t.TierUsed == 3)
            .Where(t => t.TransactionId != null)
            .Join(
                _context.Transactions,
                log => log.TransactionId,
                txn => txn.Id,
                (log, txn) => new { log, txn })
            .GroupBy(x => x.txn.Description)
            .Where(g => g.Count() >= minTier3Count)
            .Select(g => new VendorTier3Usage(
                g.Key,
                g.Count()))
            .OrderByDescending(v => v.Tier3Count)
            .Take(20)
            .ToListAsync();

        return candidates;
    }
}
