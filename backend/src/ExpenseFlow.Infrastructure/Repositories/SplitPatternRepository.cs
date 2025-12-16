using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of ISplitPatternRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class SplitPatternRepository : ISplitPatternRepository
{
    private readonly ExpenseFlowDbContext _context;

    public SplitPatternRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<SplitPattern?> GetByIdAsync(Guid userId, Guid patternId)
    {
        return await _context.SplitPatterns
            .Include(p => p.VendorAlias)
            .FirstOrDefaultAsync(p => p.Id == patternId && p.UserId == userId);
    }

    public async Task<(List<SplitPattern> Patterns, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        var query = _context.SplitPatterns
            .Include(p => p.VendorAlias)
            .Where(p => p.UserId == userId);

        var totalCount = await query.CountAsync();

        var patterns = await query
            .OrderByDescending(p => p.UsageCount)
            .ThenByDescending(p => p.LastUsedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (patterns, totalCount);
    }

    public async Task<List<SplitPattern>> GetByVendorAliasAsync(Guid userId, Guid vendorAliasId)
    {
        return await _context.SplitPatterns
            .Include(p => p.VendorAlias)
            .Where(p => p.UserId == userId && p.VendorAliasId == vendorAliasId)
            .OrderByDescending(p => p.UsageCount)
            .ThenByDescending(p => p.LastUsedAt)
            .ToListAsync();
    }

    public async Task<SplitPattern?> GetMostRecentByVendorAsync(Guid userId, Guid vendorAliasId)
    {
        return await _context.SplitPatterns
            .Include(p => p.VendorAlias)
            .Where(p => p.UserId == userId && p.VendorAliasId == vendorAliasId)
            .OrderByDescending(p => p.UsageCount)
            .ThenByDescending(p => p.LastUsedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<SplitPattern?> GetDefaultByVendorAsync(Guid userId, Guid vendorAliasId)
    {
        return await _context.SplitPatterns
            .Include(p => p.VendorAlias)
            .Where(p => p.UserId == userId && p.VendorAliasId == vendorAliasId && p.IsDefault)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(SplitPattern pattern)
    {
        await _context.SplitPatterns.AddAsync(pattern);
    }

    public Task UpdateAsync(SplitPattern pattern)
    {
        _context.SplitPatterns.Update(pattern);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(SplitPattern pattern)
    {
        _context.SplitPatterns.Remove(pattern);
        return Task.CompletedTask;
    }

    public Task IncrementUsageAsync(SplitPattern pattern)
    {
        pattern.UsageCount++;
        pattern.LastUsedAt = DateTime.UtcNow;
        _context.SplitPatterns.Update(pattern);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
