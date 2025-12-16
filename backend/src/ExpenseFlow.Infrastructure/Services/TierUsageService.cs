using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for logging and reporting tier usage metrics.
/// </summary>
public class TierUsageService : ITierUsageService
{
    private readonly ITierUsageRepository _tierUsageRepository;

    public TierUsageService(ITierUsageRepository tierUsageRepository)
    {
        _tierUsageRepository = tierUsageRepository;
    }

    public async Task LogUsageAsync(
        Guid userId,
        string operationType,
        int tierUsed,
        decimal? confidence = null,
        int? responseTimeMs = null,
        bool cacheHit = false,
        Guid? transactionId = null,
        CancellationToken cancellationToken = default)
    {
        var log = new TierUsageLog
        {
            UserId = userId,
            TransactionId = transactionId,
            OperationType = operationType,
            TierUsed = tierUsed,
            Confidence = confidence,
            ResponseTimeMs = responseTimeMs,
            CacheHit = cacheHit
        };

        await _tierUsageRepository.AddAsync(log);
        await _tierUsageRepository.SaveChangesAsync();
    }

    public async Task<TierUsageStatsDto> GetStatsAsync(
        DateTime startDate,
        DateTime endDate,
        string? operationType = null,
        CancellationToken cancellationToken = default)
    {
        var aggregates = await _tierUsageRepository.GetAggregateAsync(startDate, endDate, operationType);

        var totalCalls = aggregates.Sum(a => a.Count);
        var tier1Calls = aggregates.Where(a => a.TierUsed == 1).Sum(a => a.Count);
        var tier2Calls = aggregates.Where(a => a.TierUsed == 2).Sum(a => a.Count);
        var tier3Calls = aggregates.Where(a => a.TierUsed == 3).Sum(a => a.Count);

        // Cost calculation based on tier usage
        // Tier 1: Free (cache)
        // Tier 2: $0.00002/call (embedding similarity)
        // Tier 3: $0.0003-0.0005/call (AI inference) - using average $0.0004
        var estimatedCost = (tier2Calls * 0.00002m) + (tier3Calls * 0.0004m);

        return new TierUsageStatsDto
        {
            Period = new TierUsagePeriod
            {
                Start = startDate,
                End = endDate
            },
            Summary = new TierUsageSummary
            {
                TotalOperations = totalCalls,
                Tier1Count = tier1Calls,
                Tier2Count = tier2Calls,
                Tier3Count = tier3Calls,
                Tier1Percentage = totalCalls > 0 ? (decimal)tier1Calls / totalCalls * 100 : 0,
                Tier2Percentage = totalCalls > 0 ? (decimal)tier2Calls / totalCalls * 100 : 0,
                Tier3Percentage = totalCalls > 0 ? (decimal)tier3Calls / totalCalls * 100 : 0,
                EstimatedCost = estimatedCost
            },
            ByOperationType = aggregates
                .GroupBy(a => a.OperationType)
                .Select(g => new TierUsageByOperationType
                {
                    OperationType = g.Key,
                    TotalCount = g.Sum(a => a.Count),
                    Tier1Percentage = g.Sum(a => a.Count) > 0
                        ? (decimal)g.Where(a => a.TierUsed == 1).Sum(a => a.Count) / g.Sum(a => a.Count) * 100
                        : 0,
                    Tier2Percentage = g.Sum(a => a.Count) > 0
                        ? (decimal)g.Where(a => a.TierUsed == 2).Sum(a => a.Count) / g.Sum(a => a.Count) * 100
                        : 0,
                    Tier3Percentage = g.Sum(a => a.Count) > 0
                        ? (decimal)g.Where(a => a.TierUsed == 3).Sum(a => a.Count) / g.Sum(a => a.Count) * 100
                        : 0
                })
                .ToList(),
            VendorCandidates = await GetVendorCandidatesInternalAsync(startDate, endDate, cancellationToken)
        };
    }

    private async Task<List<VendorAliasCandidateDto>> GetVendorCandidatesInternalAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken)
    {
        var vendorTier3Usage = await _tierUsageRepository.GetVendorTier3UsageAsync(startDate, endDate, minTier3Count: 3);

        return vendorTier3Usage
            .Select(v => new VendorAliasCandidateDto
            {
                Vendor = v.VendorDescription,
                Tier3Count = v.Tier3Count,
                Recommendation = v.Tier3Count >= 10
                    ? "High priority - create vendor alias immediately"
                    : v.Tier3Count >= 5
                        ? "Medium priority - recommended for alias creation"
                        : "Consider creating vendor alias"
            })
            .ToList();
    }

    public async Task<IReadOnlyList<VendorAliasCandidate>> GetVendorAliasCandidatesAsync(
        DateTime startDate,
        DateTime endDate,
        int minTier3Count = 5,
        CancellationToken cancellationToken = default)
    {
        var vendorTier3Usage = await _tierUsageRepository.GetVendorTier3UsageAsync(startDate, endDate, minTier3Count);

        return vendorTier3Usage
            .Select(v => new VendorAliasCandidate(
                v.VendorDescription,
                v.Tier3Count,
                v.Tier3Count >= 10
                    ? "High priority - create vendor alias immediately"
                    : v.Tier3Count >= 5
                        ? "Medium priority - recommended for alias creation"
                        : "Consider creating vendor alias"))
            .ToList();
    }
}
