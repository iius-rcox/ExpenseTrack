using System.Diagnostics;
using ExpenseFlow.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Weekly job that reduces confidence of vendor aliases unused for 6+ months.
/// Helps maintain alias quality by deprioritizing stale entries.
/// </summary>
public class AliasConfidenceDecayJob : JobBase
{
    private readonly ExpenseFlowDbContext _context;
    private const string JobName = "AliasConfidenceDecay";
    private const int StaleMonths = 6;
    private const decimal DecayRate = 0.9m; // Reduce by 10%
    private const decimal MinConfidenceThreshold = 0.5m;

    public AliasConfidenceDecayJob(
        ExpenseFlowDbContext context,
        ILogger<AliasConfidenceDecayJob> logger)
        : base(logger)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the confidence decay job.
    /// Finds aliases not matched in 6+ months with confidence > 0.5 and reduces by 10%.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        LogJobStart(JobName);

        try
        {
            var staleThreshold = DateTime.UtcNow.AddMonths(-StaleMonths);

            // Find stale aliases with confidence above minimum threshold
            var staleAliases = await _context.VendorAliases
                .Where(a => a.LastMatchedAt < staleThreshold && a.Confidence > MinConfidenceThreshold)
                .ToListAsync(cancellationToken);

            if (!staleAliases.Any())
            {
                Logger.LogInformation("No stale aliases found for decay");
                stopwatch.Stop();
                LogJobComplete(JobName, stopwatch.Elapsed);
                return;
            }

            Logger.LogInformation("Found {Count} stale aliases for confidence decay", staleAliases.Count);

            foreach (var alias in staleAliases)
            {
                var originalConfidence = alias.Confidence;
                alias.Confidence *= DecayRate;

                Logger.LogDebug(
                    "Decayed alias {AliasId} ({CanonicalName}): {Original:F2} -> {New:F2}",
                    alias.Id,
                    alias.CanonicalName,
                    originalConfidence,
                    alias.Confidence);
            }

            await _context.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();
            Logger.LogInformation(
                "Decayed confidence for {Count} stale aliases in {Duration}ms",
                staleAliases.Count,
                stopwatch.ElapsedMilliseconds);

            LogJobComplete(JobName, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogJobFailed(JobName, ex);
            throw;
        }
    }
}
