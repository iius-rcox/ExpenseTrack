using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for aggregated cache statistics.
/// </summary>
public class CacheStatsService : ICacheStatsService
{
    private readonly IDescriptionCacheService _descriptionCacheService;
    private readonly IVendorAliasService _vendorAliasService;
    private readonly IStatementFingerprintService _statementFingerprintService;
    private readonly IExpenseEmbeddingService _expenseEmbeddingService;
    private readonly ExpenseFlowDbContext _dbContext;

    public CacheStatsService(
        IDescriptionCacheService descriptionCacheService,
        IVendorAliasService vendorAliasService,
        IStatementFingerprintService statementFingerprintService,
        IExpenseEmbeddingService expenseEmbeddingService,
        ExpenseFlowDbContext dbContext)
    {
        _descriptionCacheService = descriptionCacheService;
        _vendorAliasService = vendorAliasService;
        _statementFingerprintService = statementFingerprintService;
        _expenseEmbeddingService = expenseEmbeddingService;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<CacheStatsResponse> GetAllStatsAsync()
    {
        var descriptionStats = await _descriptionCacheService.GetStatsAsync();
        var vendorStats = await _vendorAliasService.GetStatsAsync();
        var fingerprintStats = await _statementFingerprintService.GetStatsAsync();
        var embeddingStats = await _expenseEmbeddingService.GetStatsAsync();

        // Split patterns stats
        var splitPatternCount = await _dbContext.SplitPatterns.CountAsync();
        var splitPatternUsage = await _dbContext.SplitPatterns.SumAsync(s => s.UsageCount);

        return new CacheStatsResponse
        {
            DescriptionCache = new CacheTableStats
            {
                TotalEntries = descriptionStats.TotalEntries,
                TotalHits = descriptionStats.TotalHits
            },
            VendorAliases = new CacheTableStats
            {
                TotalEntries = vendorStats.TotalEntries,
                TotalHits = vendorStats.TotalHits
            },
            StatementFingerprints = new CacheTableStats
            {
                TotalEntries = fingerprintStats.TotalEntries,
                TotalHits = fingerprintStats.TotalHits
            },
            ExpenseEmbeddings = new CacheTableStats
            {
                TotalEntries = embeddingStats.TotalEntries,
                TotalHits = embeddingStats.TotalHits
            },
            SplitPatterns = new CacheTableStats
            {
                TotalEntries = splitPatternCount,
                TotalHits = splitPatternUsage
            }
        };
    }
}
