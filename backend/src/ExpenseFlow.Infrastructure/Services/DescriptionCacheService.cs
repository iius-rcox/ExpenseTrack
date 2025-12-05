using System.Security.Cryptography;
using System.Text;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for description cache operations.
/// </summary>
public class DescriptionCacheService : IDescriptionCacheService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<DescriptionCacheService> _logger;

    public DescriptionCacheService(ExpenseFlowDbContext dbContext, ILogger<DescriptionCacheService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<DescriptionCache?> GetByHashAsync(string rawDescriptionHash)
    {
        var entry = await _dbContext.DescriptionCaches
            .FirstOrDefaultAsync(d => d.RawDescriptionHash == rawDescriptionHash);

        if (entry is not null)
        {
            // Increment hit count
            entry.HitCount++;
            await _dbContext.SaveChangesAsync();
        }

        return entry;
    }

    /// <inheritdoc />
    public async Task<DescriptionCache> AddOrUpdateAsync(string rawDescription, string normalizedDescription)
    {
        var hash = ComputeHash(rawDescription);
        var existing = await _dbContext.DescriptionCaches
            .FirstOrDefaultAsync(d => d.RawDescriptionHash == hash);

        if (existing is not null)
        {
            existing.HitCount++;
            existing.NormalizedDescription = normalizedDescription;
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        var entry = new DescriptionCache
        {
            RawDescriptionHash = hash,
            RawDescription = rawDescription,
            NormalizedDescription = normalizedDescription,
            HitCount = 0
        };

        _dbContext.DescriptionCaches.Add(entry);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Added description cache entry with hash {Hash}", hash);
        return entry;
    }

    /// <inheritdoc />
    public async Task<(int TotalEntries, int TotalHits)> GetStatsAsync()
    {
        var totalEntries = await _dbContext.DescriptionCaches.CountAsync();
        var totalHits = await _dbContext.DescriptionCaches.SumAsync(d => d.HitCount);
        return (totalEntries, totalHits);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
