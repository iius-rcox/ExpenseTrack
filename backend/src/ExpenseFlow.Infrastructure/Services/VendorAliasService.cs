using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for vendor alias operations.
/// </summary>
public class VendorAliasService : IVendorAliasService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<VendorAliasService> _logger;

    public VendorAliasService(ExpenseFlowDbContext dbContext, ILogger<VendorAliasService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<VendorAlias?> FindMatchingAliasAsync(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        // Database-side pattern matching using PostgreSQL ILIKE
        // This checks if the description contains the alias pattern (case-insensitive)
        // Pattern: description ILIKE '%' || alias_pattern || '%'
        return await _dbContext.VendorAliases
            .AsNoTracking()
            .Where(v => EF.Functions.ILike(description, "%" + v.AliasPattern + "%"))
            .OrderByDescending(v => v.Confidence)
            .ThenByDescending(v => v.MatchCount)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<VendorAlias?> FindMatchingAliasAsync(string description, params VendorCategory[] categories)
    {
        if (string.IsNullOrWhiteSpace(description) || categories.Length == 0)
            return null;

        var normalizedDescription = description.ToUpperInvariant();

        var aliases = await _dbContext.VendorAliases
            .AsNoTracking()
            .Where(v => categories.Contains(v.Category))
            .OrderByDescending(v => v.Confidence)
            .ThenByDescending(v => v.MatchCount)
            .ToListAsync();

        foreach (var alias in aliases)
        {
            if (normalizedDescription.Contains(alias.AliasPattern.ToUpperInvariant()))
            {
                return alias;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<VendorAlias?> GetByCanonicalNameAsync(string canonicalName)
    {
        return await _dbContext.VendorAliases
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.CanonicalName == canonicalName);
    }

    /// <inheritdoc />
    public async Task<VendorAlias> AddOrUpdateAsync(VendorAlias alias)
    {
        var existing = await _dbContext.VendorAliases
            .FirstOrDefaultAsync(v => v.CanonicalName == alias.CanonicalName && v.AliasPattern == alias.AliasPattern);

        if (existing is not null)
        {
            existing.DisplayName = alias.DisplayName;
            existing.DefaultGLCode = alias.DefaultGLCode;
            existing.DefaultDepartment = alias.DefaultDepartment;
            existing.Confidence = alias.Confidence;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        _dbContext.VendorAliases.Add(alias);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Added vendor alias {CanonicalName} with pattern {Pattern}",
            alias.CanonicalName, alias.AliasPattern);
        return alias;
    }

    /// <inheritdoc />
    public async Task RecordMatchAsync(Guid aliasId)
    {
        var alias = await _dbContext.VendorAliases.FindAsync(aliasId);
        if (alias is not null)
        {
            alias.MatchCount++;
            alias.LastMatchedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
        }
    }

    /// <inheritdoc />
    public async Task<(int TotalEntries, int TotalHits)> GetStatsAsync()
    {
        var totalEntries = await _dbContext.VendorAliases.CountAsync();
        var totalHits = await _dbContext.VendorAliases.SumAsync(v => v.MatchCount);
        return (totalEntries, totalHits);
    }

    /// <inheritdoc />
    public async Task<VendorAlias?> GetByVendorNameAsync(string vendorName)
    {
        if (string.IsNullOrWhiteSpace(vendorName))
        {
            return null;
        }

        // First try exact match on CanonicalName
        var exact = await _dbContext.VendorAliases
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.CanonicalName == vendorName);

        if (exact != null)
        {
            return exact;
        }

        // Fall back to pattern matching
        return await FindMatchingAliasAsync(vendorName);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(VendorAlias alias)
    {
        var existing = await _dbContext.VendorAliases.FindAsync(alias.Id);
        if (existing is not null)
        {
            existing.DisplayName = alias.DisplayName;
            existing.DefaultGLCode = alias.DefaultGLCode;
            existing.DefaultDepartment = alias.DefaultDepartment;
            existing.GLConfirmCount = alias.GLConfirmCount;
            existing.DeptConfirmCount = alias.DeptConfirmCount;
            existing.Confidence = alias.Confidence;
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Updated vendor alias {CanonicalName}: GL={GLCode}, Dept={Dept}",
                alias.CanonicalName, alias.DefaultGLCode, alias.DefaultDepartment);
        }
    }
}
