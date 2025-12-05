using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for statement fingerprint operations.
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
        return await _dbContext.StatementFingerprints
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId && s.HeaderHash == headerHash);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StatementFingerprint>> GetByUserAsync(Guid userId)
    {
        return await _dbContext.StatementFingerprints
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
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
            return existing;
        }

        _dbContext.StatementFingerprints.Add(fingerprint);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Added statement fingerprint for user {UserId} with source {SourceName}",
            fingerprint.UserId, fingerprint.SourceName);
        return fingerprint;
    }

    /// <inheritdoc />
    public async Task<(int TotalEntries, int TotalHits)> GetStatsAsync()
    {
        var totalEntries = await _dbContext.StatementFingerprints.CountAsync();
        // Fingerprints don't track hits, return 0
        return (totalEntries, 0);
    }
}
