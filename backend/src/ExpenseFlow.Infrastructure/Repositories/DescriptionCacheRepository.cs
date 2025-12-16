using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IDescriptionCacheRepository.
/// </summary>
public class DescriptionCacheRepository : IDescriptionCacheRepository
{
    private readonly ExpenseFlowDbContext _context;

    public DescriptionCacheRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<DescriptionCache?> GetByHashAsync(string hash)
    {
        return await _context.DescriptionCaches
            .FirstOrDefaultAsync(d => d.RawDescriptionHash == hash);
    }

    public async Task AddAsync(DescriptionCache entry)
    {
        await _context.DescriptionCaches.AddAsync(entry);
    }

    public async Task IncrementHitCountAsync(Guid id)
    {
        await _context.DescriptionCaches
            .Where(d => d.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(d => d.HitCount, d => d.HitCount + 1)
                .SetProperty(d => d.LastAccessedAt, DateTime.UtcNow));
    }

    public async Task<(int TotalEntries, long TotalHits)> GetStatsAsync()
    {
        var stats = await _context.DescriptionCaches
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalEntries = g.Count(),
                TotalHits = g.Sum(d => (long)d.HitCount)
            })
            .FirstOrDefaultAsync();

        return stats != null
            ? (stats.TotalEntries, stats.TotalHits)
            : (0, 0);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
