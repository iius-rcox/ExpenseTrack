using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of ISubscriptionRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly ExpenseFlowDbContext _context;

    public SubscriptionRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<DetectedSubscription?> GetByIdAsync(Guid userId, Guid subscriptionId)
    {
        return await _context.DetectedSubscriptions
            .Include(s => s.VendorAlias)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);
    }

    public async Task<(List<DetectedSubscription> Subscriptions, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        SubscriptionStatus? status = null)
    {
        var query = _context.DetectedSubscriptions
            .Include(s => s.VendorAlias)
            .Where(s => s.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var subscriptions = await query
            .OrderByDescending(s => s.LastSeenDate)
            .ThenBy(s => s.VendorName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (subscriptions, totalCount);
    }

    public async Task<DetectedSubscription?> GetByVendorNameAsync(Guid userId, string vendorName)
    {
        return await _context.DetectedSubscriptions
            .Include(s => s.VendorAlias)
            .FirstOrDefaultAsync(s => s.UserId == userId &&
                                      s.VendorName.ToUpper() == vendorName.ToUpper());
    }

    public async Task<List<DetectedSubscription>> GetByStatusesAsync(Guid userId, params SubscriptionStatus[] statuses)
    {
        return await _context.DetectedSubscriptions
            .Include(s => s.VendorAlias)
            .Where(s => s.UserId == userId && statuses.Contains(s.Status))
            .OrderBy(s => s.Status)
            .ThenByDescending(s => s.LastSeenDate)
            .ToListAsync();
    }

    public async Task<List<DetectedSubscription>> GetExpectedByDateAsync(DateOnly expectedBeforeDate)
    {
        return await _context.DetectedSubscriptions
            .Include(s => s.User)
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.ExpectedNextDate.HasValue &&
                        s.ExpectedNextDate.Value <= expectedBeforeDate)
            .ToListAsync();
    }

    public async Task AddAsync(DetectedSubscription subscription)
    {
        await _context.DetectedSubscriptions.AddAsync(subscription);
    }

    public Task UpdateAsync(DetectedSubscription subscription)
    {
        subscription.UpdatedAt = DateTime.UtcNow;
        _context.DetectedSubscriptions.Update(subscription);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(DetectedSubscription subscription)
    {
        _context.DetectedSubscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    public async Task<List<KnownSubscriptionVendor>> GetKnownVendorsAsync()
    {
        return await _context.KnownSubscriptionVendors
            .Where(v => v.IsActive)
            .OrderBy(v => v.DisplayName)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
