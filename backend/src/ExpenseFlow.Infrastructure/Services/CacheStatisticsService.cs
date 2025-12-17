using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Configuration;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for aggregating cache tier usage statistics.
/// </summary>
public class CacheStatisticsService : ICacheStatisticsService
{
    private readonly ExpenseFlowDbContext _context;
    private readonly AnalyticsOptions _options;
    private readonly ILogger<CacheStatisticsService> _logger;

    public CacheStatisticsService(
        ExpenseFlowDbContext context,
        IOptions<AnalyticsOptions> options,
        ILogger<CacheStatisticsService> logger)
    {
        _context = context;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CacheStatisticsResponse> GetStatisticsAsync(
        Guid userId,
        string period,
        string? groupBy,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Getting cache statistics for user {UserId}, period {Period}, groupBy {GroupBy}",
            userId, period, groupBy);

        // Parse period into date range
        var (startDate, endDate) = ParsePeriodRange(period);

        // Query tier usage logs for the period
        var logsQuery = _context.TierUsageLogs
            .Where(l => l.UserId == userId &&
                       l.CreatedAt >= startDate.ToDateTime(TimeOnly.MinValue) &&
                       l.CreatedAt <= endDate.ToDateTime(TimeOnly.MaxValue));

        // Calculate overall statistics
        var overall = await CalculateOverallStatisticsAsync(logsQuery, ct);

        // Build response
        var response = new CacheStatisticsResponse
        {
            Period = period,
            Overall = overall
        };

        // Add grouped breakdown if requested
        if (groupBy?.ToLowerInvariant() == "operation")
        {
            response.ByOperation = await GetStatsByOperationAsync(logsQuery, ct);
        }
        else if (groupBy?.ToLowerInvariant() == "day")
        {
            // Daily breakdown could be added here if needed
            // For now, we just return by operation
            response.ByOperation = await GetStatsByOperationAsync(logsQuery, ct);
        }

        _logger.LogInformation(
            "Cache statistics for user {UserId}, period {Period}: {TotalOps} operations, {Tier1Rate}% Tier 1 hit rate",
            userId, period, overall.TotalOperations, overall.Tier1HitRate);

        return response;
    }

    private async Task<CacheStatisticsDto> CalculateOverallStatisticsAsync(
        IQueryable<Core.Entities.TierUsageLog> logsQuery,
        CancellationToken ct)
    {
        var stats = await logsQuery
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalOperations = g.Count(),
                Tier1Hits = g.Count(l => l.TierUsed == 1),
                Tier2Hits = g.Count(l => l.TierUsed == 2),
                Tier3Hits = g.Count(l => l.TierUsed == 3),
                AvgResponseTime = g.Average(l => (double?)l.ResponseTimeMs) ?? 0
            })
            .FirstOrDefaultAsync(ct);

        if (stats == null || stats.TotalOperations == 0)
        {
            return new CacheStatisticsDto
            {
                TotalOperations = 0,
                Tier1Hits = 0,
                Tier2Hits = 0,
                Tier3Hits = 0,
                Tier1HitRate = 0,
                Tier2HitRate = 0,
                Tier3HitRate = 0,
                EstimatedMonthlyCost = 0,
                AvgResponseTimeMs = 0,
                BelowTarget = false
            };
        }

        var tier1Rate = Math.Round((decimal)stats.Tier1Hits / stats.TotalOperations * 100, 2);
        var tier2Rate = Math.Round((decimal)stats.Tier2Hits / stats.TotalOperations * 100, 2);
        var tier3Rate = Math.Round((decimal)stats.Tier3Hits / stats.TotalOperations * 100, 2);

        // Calculate estimated monthly cost
        var tier2Cost = stats.Tier2Hits * _options.Tier2CostPerOperation;
        var tier3Cost = stats.Tier3Hits * _options.Tier3CostPerOperation;
        var estimatedCost = tier2Cost + tier3Cost;

        return new CacheStatisticsDto
        {
            TotalOperations = stats.TotalOperations,
            Tier1Hits = stats.Tier1Hits,
            Tier2Hits = stats.Tier2Hits,
            Tier3Hits = stats.Tier3Hits,
            Tier1HitRate = tier1Rate,
            Tier2HitRate = tier2Rate,
            Tier3HitRate = tier3Rate,
            EstimatedMonthlyCost = estimatedCost,
            AvgResponseTimeMs = (int)Math.Round(stats.AvgResponseTime),
            BelowTarget = tier1Rate < (decimal)_options.Tier1HitRateTarget
        };
    }

    private async Task<List<CacheStatsByOperationDto>> GetStatsByOperationAsync(
        IQueryable<Core.Entities.TierUsageLog> logsQuery,
        CancellationToken ct)
    {
        var statsByOperation = await logsQuery
            .GroupBy(l => l.OperationType)
            .Select(g => new
            {
                OperationType = g.Key,
                TotalCount = g.Count(),
                Tier1Hits = g.Count(l => l.TierUsed == 1),
                Tier2Hits = g.Count(l => l.TierUsed == 2),
                Tier3Hits = g.Count(l => l.TierUsed == 3)
            })
            .ToListAsync(ct);

        return statsByOperation
            .Select(s => new CacheStatsByOperationDto
            {
                OperationType = s.OperationType,
                Tier1Hits = s.Tier1Hits,
                Tier2Hits = s.Tier2Hits,
                Tier3Hits = s.Tier3Hits,
                Tier1HitRate = s.TotalCount > 0
                    ? Math.Round((decimal)s.Tier1Hits / s.TotalCount * 100, 2)
                    : 0
            })
            .OrderBy(s => s.OperationType)
            .ToList();
    }

    private static (DateOnly Start, DateOnly End) ParsePeriodRange(string period)
    {
        // Parse YYYY-MM format
        var parts = period.Split('-');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month))
        {
            throw new ArgumentException($"Invalid period format: {period}. Expected YYYY-MM.");
        }

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return (startDate, endDate);
    }
}
