using System.Diagnostics;
using ExpenseFlow.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Jobs;

/// <summary>
/// Monthly job that purges stale unverified embeddings older than 6 months.
/// Verified embeddings (ExpiresAt = null) are never deleted.
/// </summary>
public class EmbeddingCleanupJob : JobBase
{
    private readonly ExpenseFlowDbContext _context;
    private const string JobName = "EmbeddingCleanup";
    private const int BatchSize = 100;

    public EmbeddingCleanupJob(
        ExpenseFlowDbContext context,
        ILogger<EmbeddingCleanupJob> logger)
        : base(logger)
    {
        _context = context;
    }

    /// <summary>
    /// Executes the embedding cleanup job.
    /// Deletes unverified embeddings where ExpiresAt has passed.
    /// </summary>
    [AutomaticRetry(Attempts = 3, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
    public override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        LogJobStart(JobName);

        try
        {
            var totalDeleted = await PurgeStaleEmbeddingsAsync(cancellationToken);

            stopwatch.Stop();

            if (totalDeleted > 0)
            {
                Logger.LogInformation(
                    "Purged {Count} stale unverified embeddings in {Duration}ms",
                    totalDeleted,
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                Logger.LogInformation("No stale embeddings found for cleanup");
            }

            LogJobComplete(JobName, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LogJobFailed(JobName, ex);
            throw;
        }
    }

    /// <summary>
    /// Purges unverified embeddings older than 6 months.
    /// Embeddings with ExpiresAt = null (verified) are never deleted.
    /// </summary>
    /// <returns>Number of embeddings deleted.</returns>
    public async Task<int> PurgeStaleEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var totalDeleted = 0;

        // Process in batches to avoid memory issues with large datasets
        while (!cancellationToken.IsCancellationRequested)
        {
            // Find stale embeddings (ExpiresAt is not null AND has passed)
            var staleEmbeddings = await _context.ExpenseEmbeddings
                .Where(e => e.ExpiresAt != null && e.ExpiresAt < now)
                .Take(BatchSize)
                .ToListAsync(cancellationToken);

            if (!staleEmbeddings.Any())
            {
                break;
            }

            Logger.LogDebug("Deleting batch of {Count} stale embeddings", staleEmbeddings.Count);

            _context.ExpenseEmbeddings.RemoveRange(staleEmbeddings);
            await _context.SaveChangesAsync(cancellationToken);

            totalDeleted += staleEmbeddings.Count;

            // Small delay between batches to reduce database load
            if (staleEmbeddings.Count == BatchSize)
            {
                await Task.Delay(100, cancellationToken);
            }
        }

        return totalDeleted;
    }
}
