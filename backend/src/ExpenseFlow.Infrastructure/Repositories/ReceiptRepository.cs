using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IReceiptRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class ReceiptRepository : IReceiptRepository
{
    private readonly ExpenseFlowDbContext _context;

    public ReceiptRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<Receipt> AddAsync(Receipt receipt)
    {
        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync();
        return receipt;
    }

    public async Task<Receipt?> GetByIdAsync(Guid id, Guid userId)
    {
        return await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
    }

    public async Task<Receipt?> GetByIdAsync(Guid id)
    {
        return await _context.Receipts.FindAsync(id);
    }

    public async Task<Receipt> UpdateAsync(Receipt receipt)
    {
        _context.Receipts.Update(receipt);
        await _context.SaveChangesAsync();
        return receipt;
    }

    public async Task<bool> DeleteAsync(Guid id, Guid userId)
    {
        var receipt = await GetByIdAsync(id, userId);
        if (receipt == null)
        {
            return false;
        }

        _context.Receipts.Remove(receipt);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(List<Receipt> Items, int TotalCount)> GetPagedAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 20,
        ReceiptStatus? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.Receipts
            .Where(r => r.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<Dictionary<ReceiptStatus, int>> GetStatusCountsAsync(Guid userId)
    {
        return await _context.Receipts
            .Where(r => r.UserId == userId)
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }
}
