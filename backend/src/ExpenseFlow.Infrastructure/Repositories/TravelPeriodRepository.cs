using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of ITravelPeriodRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class TravelPeriodRepository : ITravelPeriodRepository
{
    private readonly ExpenseFlowDbContext _context;

    public TravelPeriodRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<TravelPeriod?> GetByIdAsync(Guid userId, Guid travelPeriodId)
    {
        return await _context.TravelPeriods
            .Include(t => t.SourceReceipt)
            .FirstOrDefaultAsync(t => t.Id == travelPeriodId && t.UserId == userId);
    }

    public async Task<(List<TravelPeriod> TravelPeriods, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var query = _context.TravelPeriods.Where(t => t.UserId == userId);

        // Apply filters
        if (startDate.HasValue)
        {
            query = query.Where(t => t.EndDate >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(t => t.StartDate <= endDate.Value);
        }

        var totalCount = await query.CountAsync();

        var travelPeriods = await query
            .OrderByDescending(t => t.StartDate)
            .ThenByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (travelPeriods, totalCount);
    }

    public async Task<List<TravelPeriod>> GetOverlappingAsync(Guid userId, DateOnly startDate, DateOnly endDate)
    {
        // Two periods overlap if: period.StartDate <= rangeEnd AND period.EndDate >= rangeStart
        return await _context.TravelPeriods
            .Where(t => t.UserId == userId &&
                        t.StartDate <= endDate &&
                        t.EndDate >= startDate)
            .OrderBy(t => t.StartDate)
            .ToListAsync();
    }

    public async Task<TravelPeriod?> GetByDateAsync(Guid userId, DateOnly date)
    {
        return await _context.TravelPeriods
            .FirstOrDefaultAsync(t => t.UserId == userId &&
                                      t.StartDate <= date &&
                                      t.EndDate >= date);
    }

    public async Task AddAsync(TravelPeriod travelPeriod)
    {
        await _context.TravelPeriods.AddAsync(travelPeriod);
    }

    public Task UpdateAsync(TravelPeriod travelPeriod)
    {
        travelPeriod.UpdatedAt = DateTime.UtcNow;
        _context.TravelPeriods.Update(travelPeriod);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TravelPeriod travelPeriod)
    {
        _context.TravelPeriods.Remove(travelPeriod);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
