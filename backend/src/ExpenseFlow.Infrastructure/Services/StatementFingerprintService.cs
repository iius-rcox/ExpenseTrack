using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for statement fingerprint operations.
/// Supports both user-specific and system-wide fingerprints.
/// </summary>
public class StatementFingerprintService : IStatementFingerprintService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<StatementFingerprintService> _logger;

    public StatementFingerprintService(ExpenseFlowDbContext dbContext, ILogger<StatementFingerprintService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StatementFingerprint?> GetByUserAndHashAsync(Guid userId, string headerHash)
    {
        // First check user-specific fingerprint
        var userFingerprint = await _dbContext.StatementFingerprints
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.HeaderHash == headerHash);

        if (userFingerprint != null)
            return userFingerprint;

        // Fall back to system fingerprint
        return await _dbContext.StatementFingerprints
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == null && s.HeaderHash == headerHash);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StatementFingerprint>> GetByHashAsync(Guid userId, string headerHash)
    {
        // Get all fingerprints matching the hash (user-specific + system)
        // Order: user fingerprints first, then system fingerprints
        var fingerprints = await _dbContext.StatementFingerprints
            .AsNoTracking()
            .Where(s => s.HeaderHash == headerHash && (s.UserId == userId || s.UserId == null))
            .OrderByDescending(s => s.UserId != null) // User fingerprints first
            .ThenByDescending(s => s.HitCount) // Most used first
            .ToListAsync();

        return fingerprints;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StatementFingerprint>> GetByUserAsync(Guid userId)
    {
        // Get user-specific fingerprints + system fingerprints
        return await _dbContext.StatementFingerprints
            .AsNoTracking()
            .Where(s => s.UserId == userId || s.UserId == null)
            .OrderByDescending(s => s.UserId != null) // User fingerprints first
            .ThenByDescending(s => s.HitCount)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<StatementFingerprint> AddOrUpdateAsync(StatementFingerprint fingerprint)
    {
        var existing = await _dbContext.StatementFingerprints
            .FirstOrDefaultAsync(s => s.UserId == fingerprint.UserId && s.HeaderHash == fingerprint.HeaderHash);

        if (existing is not null)
        {
            existing.SourceName = fingerprint.SourceName;
            existing.ColumnMapping = fingerprint.ColumnMapping;
            existing.DateFormat = fingerprint.DateFormat;
            existing.AmountSign = fingerprint.AmountSign;
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Updated statement fingerprint {FingerprintId} for user {UserId}",
                existing.Id, fingerprint.UserId);
            return existing;
        }

        _dbContext.StatementFingerprints.Add(fingerprint);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Added statement fingerprint for user {UserId} with source {SourceName}",
            fingerprint.UserId, fingerprint.SourceName);
        return fingerprint;
    }

    /// <inheritdoc />
    public async Task RecordHitAsync(Guid fingerprintId)
    {
        var fingerprint = await _dbContext.StatementFingerprints.FindAsync(fingerprintId);
        if (fingerprint != null)
        {
            fingerprint.HitCount++;
            fingerprint.LastUsedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Recorded hit for fingerprint {FingerprintId}, total hits: {HitCount}",
                fingerprintId, fingerprint.HitCount);
        }
    }

    /// <inheritdoc />
    public async Task<(int TotalEntries, int TotalHits)> GetStatsAsync()
    {
        var stats = await _dbContext.StatementFingerprints
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalEntries = g.Count(),
                TotalHits = g.Sum(f => f.HitCount)
            })
            .FirstOrDefaultAsync();

        return stats != null ? (stats.TotalEntries, stats.TotalHits) : (0, 0);
    }
}
