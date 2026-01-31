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

    /// <summary>
    /// Escapes special characters in ILIKE patterns to prevent SQL injection.
    /// PostgreSQL ILIKE special chars: %, _, \ (backslash is the escape character)
    /// </summary>
    private static string EscapeILikePattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Escape backslash first (it's the escape character), then % and _
        return input
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
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
        MatchStatus? matchStatus = null,
        string? vendor = null,
        DateOnly? receiptDateFrom = null,
        DateOnly? receiptDateTo = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? sortBy = null,
        string? sortOrder = null)
    {
        var query = _context.Receipts
            .Where(r => r.UserId == userId);

        // Receipt status filter
        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        // Match status filter
        if (matchStatus.HasValue)
        {
            query = query.Where(r => r.MatchStatus == matchStatus.Value);
        }

        // Vendor search (case-insensitive)
        // Escape ILIKE special characters to prevent pattern injection
        if (!string.IsNullOrWhiteSpace(vendor))
        {
            var escapedVendor = EscapeILikePattern(vendor);
            query = query.Where(r => r.VendorExtracted != null &&
                EF.Functions.ILike(r.VendorExtracted, $"%{escapedVendor}%"));
        }

        // Receipt date filter (DateExtracted)
        if (receiptDateFrom.HasValue)
        {
            query = query.Where(r => r.DateExtracted >= receiptDateFrom.Value);
        }

        if (receiptDateTo.HasValue)
        {
            query = query.Where(r => r.DateExtracted <= receiptDateTo.Value);
        }

        // Upload date filter (CreatedAt)
        if (fromDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= toDate.Value);
        }

        var totalCount = await query.CountAsync();

        // Apply sorting
        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase) ||
                           string.IsNullOrEmpty(sortOrder);

        query = sortBy?.ToLowerInvariant() switch
        {
            "date" => isDescending
                ? query.OrderByDescending(r => r.DateExtracted)
                : query.OrderBy(r => r.DateExtracted),
            "amount" => isDescending
                ? query.OrderByDescending(r => r.AmountExtracted)
                : query.OrderBy(r => r.AmountExtracted),
            "vendor" => isDescending
                ? query.OrderByDescending(r => r.VendorExtracted)
                : query.OrderBy(r => r.VendorExtracted),
            _ => isDescending
                ? query.OrderByDescending(r => r.CreatedAt)
                : query.OrderBy(r => r.CreatedAt)
        };

        var items = await query
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

    public async Task<int> GetReceiptsWithoutThumbnailsCountAsync(List<string>? contentTypes = null)
    {
        var query = _context.Receipts
            .Where(r => r.ThumbnailUrl == null && r.BlobUrl != null);

        if (contentTypes != null && contentTypes.Count > 0)
        {
            query = query.Where(r => contentTypes.Contains(r.ContentType));
        }

        return await query.CountAsync();
    }

    public async Task<List<Receipt>> GetReceiptsWithoutThumbnailsAsync(int batchSize, List<string>? contentTypes = null)
    {
        var query = _context.Receipts
            .Where(r => r.ThumbnailUrl == null && r.BlobUrl != null);

        if (contentTypes != null && contentTypes.Count > 0)
        {
            query = query.Where(r => contentTypes.Contains(r.ContentType));
        }

        return await query
            .OrderBy(r => r.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<int> GetAllReceiptsWithBlobsCountAsync(List<string>? contentTypes = null)
    {
        var query = _context.Receipts
            .Where(r => r.BlobUrl != null);

        if (contentTypes != null && contentTypes.Count > 0)
        {
            query = query.Where(r => contentTypes.Contains(r.ContentType));
        }

        return await query.CountAsync();
    }

    public async Task<List<Receipt>> GetReceiptsForThumbnailRegenerationAsync(int batchSize, List<string>? contentTypes = null, int offset = 0)
    {
        var query = _context.Receipts
            .Where(r => r.BlobUrl != null);

        if (contentTypes != null && contentTypes.Count > 0)
        {
            query = query.Where(r => contentTypes.Contains(r.ContentType));
        }

        return await query
            .OrderBy(r => r.CreatedAt)
            .Skip(offset)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<Receipt?> FindByFileHashAsync(string fileHash, Guid userId)
    {
        if (string.IsNullOrEmpty(fileHash))
            return null;

        return await _context.Receipts
            .FirstOrDefaultAsync(r => r.UserId == userId && r.FileHash == fileHash);
    }

    public async Task<Receipt?> FindByContentHashAsync(string contentHash, Guid userId)
    {
        if (string.IsNullOrEmpty(contentHash))
            return null;

        return await _context.Receipts
            .FirstOrDefaultAsync(r => r.UserId == userId && r.ContentHash == contentHash);
    }

    public async Task<List<Receipt>> GetReceiptsWithoutFileHashAsync(int batchSize)
    {
        return await _context.Receipts
            .Where(r => r.FileHash == null && r.BlobUrl != null)
            .OrderBy(r => r.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }
}
