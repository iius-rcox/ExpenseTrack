using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IExpensePatternRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class ExpensePatternRepository : IExpensePatternRepository
{
    private readonly ExpenseFlowDbContext _context;

    public ExpensePatternRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<ExpensePattern?> GetByIdAsync(Guid userId, Guid patternId)
    {
        return await _context.ExpensePatterns
            .FirstOrDefaultAsync(p => p.Id == patternId && p.UserId == userId);
    }

    public async Task<ExpensePattern?> GetByNormalizedVendorAsync(Guid userId, string normalizedVendor)
    {
        return await _context.ExpensePatterns
            .FirstOrDefaultAsync(p => p.UserId == userId &&
                                      p.NormalizedVendor.ToUpper() == normalizedVendor.ToUpper());
    }

    public async Task<(List<ExpensePattern> Patterns, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        bool includeSuppressed = false)
    {
        var query = _context.ExpensePatterns
            .Where(p => p.UserId == userId);

        if (!includeSuppressed)
        {
            query = query.Where(p => !p.IsSuppressed);
        }

        var totalCount = await query.CountAsync();

        var patterns = await query
            .OrderByDescending(p => p.OccurrenceCount)
            .ThenByDescending(p => p.LastSeenAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (patterns, totalCount);
    }

    public async Task<List<ExpensePattern>> GetActiveAsync(Guid userId)
    {
        return await _context.ExpensePatterns
            .Where(p => p.UserId == userId && !p.IsSuppressed)
            .OrderByDescending(p => p.OccurrenceCount)
            .ToListAsync();
    }

    public async Task<List<ExpensePattern>> GetTopPatternsAsync(Guid userId, int limit = 10)
    {
        return await _context.ExpensePatterns
            .Where(p => p.UserId == userId && !p.IsSuppressed)
            .OrderByDescending(p => p.OccurrenceCount)
            .ThenByDescending(p => p.ConfirmCount)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<(int ActiveCount, int SuppressedCount)> GetCountsAsync(Guid userId)
    {
        var counts = await _context.ExpensePatterns
            .Where(p => p.UserId == userId)
            .GroupBy(p => p.IsSuppressed)
            .Select(g => new { IsSuppressed = g.Key, Count = g.Count() })
            .ToListAsync();

        var activeCount = counts.FirstOrDefault(c => !c.IsSuppressed)?.Count ?? 0;
        var suppressedCount = counts.FirstOrDefault(c => c.IsSuppressed)?.Count ?? 0;

        return (activeCount, suppressedCount);
    }

    public async Task<bool> ExistsAsync(Guid userId, string normalizedVendor)
    {
        return await _context.ExpensePatterns
            .AnyAsync(p => p.UserId == userId &&
                          p.NormalizedVendor.ToUpper() == normalizedVendor.ToUpper());
    }

    public async Task AddAsync(ExpensePattern pattern)
    {
        await _context.ExpensePatterns.AddAsync(pattern);
    }

    public async Task AddRangeAsync(IEnumerable<ExpensePattern> patterns)
    {
        await _context.ExpensePatterns.AddRangeAsync(patterns);
    }

    public Task UpdateAsync(ExpensePattern pattern)
    {
        pattern.UpdatedAt = DateTime.UtcNow;
        _context.ExpensePatterns.Update(pattern);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ExpensePattern pattern)
    {
        _context.ExpensePatterns.Remove(pattern);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
