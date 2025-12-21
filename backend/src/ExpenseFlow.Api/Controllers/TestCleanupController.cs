#if DEBUG || STAGING
using System.Diagnostics;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for test data cleanup operations.
/// Only available in DEBUG and STAGING builds - NOT available in production.
/// </summary>
[Authorize]
[Route("api/test")]
[ApiController]
[Produces("application/json")]
public class TestCleanupController : ControllerBase
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly IUserService _userService;
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<TestCleanupController> _logger;

    private static readonly HashSet<string> ValidEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "receipts",
        "transactions",
        "matches",
        "imports",
        "reports",
        "embeddings",
        "tierusage",
        "travel"
    };

    public TestCleanupController(
        ExpenseFlowDbContext dbContext,
        IUserService userService,
        IBlobStorageService blobStorageService,
        ILogger<TestCleanupController> logger)
    {
        _dbContext = dbContext;
        _userService = userService;
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Cleans up test data for the authenticated user.
    /// </summary>
    /// <param name="request">Optional cleanup filters (entity types, timestamp)</param>
    /// <returns>Cleanup results including counts of deleted items</returns>
    [HttpPost("cleanup")]
    [ProducesResponseType(typeof(CleanupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CleanupResponse>> Cleanup([FromBody] CleanupRequest? request)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();

        try
        {
            // Get current user
            var user = await _userService.GetOrCreateUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new ProblemDetailsResponse
                {
                    Title = "Unauthorized",
                    Detail = "Valid authentication token required",
                    Status = 401
                });
            }

            _logger.LogInformation("Starting cleanup for user {UserId}", user.Id);

            // Validate entity types if specified
            var entityTypes = request?.EntityTypes ?? new List<string>();
            if (entityTypes.Any())
            {
                var invalidTypes = entityTypes.Where(t => !ValidEntityTypes.Contains(t)).ToList();
                if (invalidTypes.Any())
                {
                    return BadRequest(new ProblemDetailsResponse
                    {
                        Title = "Invalid entity types",
                        Detail = $"Invalid entity types: {string.Join(", ", invalidTypes)}. Valid types are: {string.Join(", ", ValidEntityTypes)}",
                        Status = 400
                    });
                }
            }

            var cleanAll = !entityTypes.Any();
            var createdAfter = request?.CreatedAfter;

            var response = new CleanupResponse
            {
                Success = true,
                DeletedCounts = new CleanupDeletedCounts()
            };

            // Order matters for foreign key constraints:
            // 1. Delete expense lines first (references reports, receipts, transactions)
            // 2. Delete expense reports (parent of expense lines)
            // 3. Delete tier usage logs (references transactions)
            // 4. Delete expense embeddings (references transactions)
            // 5. Delete travel periods (references receipts)
            // 6. Delete matches (references receipts and transactions)
            // 7. Clear Transaction.MatchedReceiptId (bidirectional FK not managed by EF)
            // 8. Delete receipts (and their blobs)
            // 9. Delete transactions
            // 10. Delete statement imports

            // 1. Delete expense lines (always - they reference everything)
            if (cleanAll || entityTypes.Contains("reports", StringComparer.OrdinalIgnoreCase))
            {
                var linesDeleted = await DeleteExpenseLinesAsync(user.Id, createdAfter);
                response.DeletedCounts.ExpenseLines = linesDeleted;
                _logger.LogInformation("Deleted {Count} expense lines", linesDeleted);
            }

            // 2. Delete expense reports
            if (cleanAll || entityTypes.Contains("reports", StringComparer.OrdinalIgnoreCase))
            {
                var reportsDeleted = await DeleteExpenseReportsAsync(user.Id, createdAfter);
                response.DeletedCounts.ExpenseReports = reportsDeleted;
                _logger.LogInformation("Deleted {Count} expense reports", reportsDeleted);
            }

            // 3. Delete tier usage logs
            if (cleanAll || entityTypes.Contains("tierusage", StringComparer.OrdinalIgnoreCase))
            {
                var tierLogsDeleted = await DeleteTierUsageLogsAsync(user.Id, createdAfter);
                response.DeletedCounts.TierUsageLogs = tierLogsDeleted;
                _logger.LogInformation("Deleted {Count} tier usage logs", tierLogsDeleted);
            }

            // 4. Delete expense embeddings
            if (cleanAll || entityTypes.Contains("embeddings", StringComparer.OrdinalIgnoreCase))
            {
                var embeddingsDeleted = await DeleteExpenseEmbeddingsAsync(user.Id, createdAfter);
                response.DeletedCounts.ExpenseEmbeddings = embeddingsDeleted;
                _logger.LogInformation("Deleted {Count} expense embeddings", embeddingsDeleted);
            }

            // 5. Delete travel periods
            if (cleanAll || entityTypes.Contains("travel", StringComparer.OrdinalIgnoreCase))
            {
                var travelDeleted = await DeleteTravelPeriodsAsync(user.Id, createdAfter);
                response.DeletedCounts.TravelPeriods = travelDeleted;
                _logger.LogInformation("Deleted {Count} travel periods", travelDeleted);
            }

            // 6. Delete matches
            if (cleanAll || entityTypes.Contains("matches", StringComparer.OrdinalIgnoreCase))
            {
                response.DeletedCounts.Matches = await DeleteMatchesAsync(user.Id, createdAfter);
                _logger.LogInformation("Deleted {Count} matches", response.DeletedCounts.Matches);
            }

            // 7. Clear Transaction.MatchedReceiptId before deleting receipts
            // This is needed because the bidirectional Receipt<->Transaction relationship
            // has a FK on Transaction.MatchedReceiptId that EF Core doesn't manage
            if (cleanAll || entityTypes.Contains("receipts", StringComparer.OrdinalIgnoreCase))
            {
                var clearedCount = await ClearTransactionMatchedReceiptIdsAsync(user.Id, createdAfter);
                _logger.LogInformation("Cleared {Count} transaction matched receipt references", clearedCount);
            }

            // 8. Delete receipts (and their blobs)
            if (cleanAll || entityTypes.Contains("receipts", StringComparer.OrdinalIgnoreCase))
            {
                var (receiptsDeleted, blobsDeleted, blobWarnings) = await DeleteReceiptsAsync(user.Id, createdAfter);
                response.DeletedCounts.Receipts = receiptsDeleted;
                response.DeletedCounts.BlobsDeleted = blobsDeleted;
                warnings.AddRange(blobWarnings);
                _logger.LogInformation("Deleted {ReceiptCount} receipts and {BlobCount} blobs", receiptsDeleted, blobsDeleted);
            }

            // 9. Delete transactions
            if (cleanAll || entityTypes.Contains("transactions", StringComparer.OrdinalIgnoreCase))
            {
                response.DeletedCounts.Transactions = await DeleteTransactionsAsync(user.Id, createdAfter);
                _logger.LogInformation("Deleted {Count} transactions", response.DeletedCounts.Transactions);
            }

            // 10. Delete statement imports
            if (cleanAll || entityTypes.Contains("imports", StringComparer.OrdinalIgnoreCase))
            {
                response.DeletedCounts.Imports = await DeleteImportsAsync(user.Id, createdAfter);
                _logger.LogInformation("Deleted {Count} statement imports", response.DeletedCounts.Imports);
            }

            stopwatch.Stop();
            response.DurationMs = stopwatch.ElapsedMilliseconds;

            if (warnings.Any())
            {
                response.Warnings = warnings;
            }

            _logger.LogInformation(
                "Cleanup completed in {Duration}ms: {Receipts} receipts, {Transactions} transactions, {Matches} matches, {Imports} imports, {Blobs} blobs",
                response.DurationMs,
                response.DeletedCounts.Receipts,
                response.DeletedCounts.Transactions,
                response.DeletedCounts.Matches,
                response.DeletedCounts.Imports,
                response.DeletedCounts.BlobsDeleted);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
            stopwatch.Stop();

            return StatusCode(500, new ProblemDetailsResponse
            {
                Title = "Cleanup failed",
                Detail = ex.Message,
                Status = 500
            });
        }
    }

    private async Task<int> DeleteMatchesAsync(Guid userId, DateTime? createdAfter)
    {
        var query = _dbContext.ReceiptTransactionMatches
            .Where(m => m.Receipt!.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(m => m.CreatedAt > createdAfter.Value);
        }

        var matches = await query.ToListAsync();
        _dbContext.ReceiptTransactionMatches.RemoveRange(matches);
        await _dbContext.SaveChangesAsync();

        return matches.Count;
    }

    private async Task<int> ClearTransactionMatchedReceiptIdsAsync(Guid userId, DateTime? createdAfter)
    {
        // Get all receipts that will be deleted
        var receiptQuery = _dbContext.Receipts.Where(r => r.UserId == userId);
        if (createdAfter.HasValue)
        {
            receiptQuery = receiptQuery.Where(r => r.CreatedAt > createdAfter.Value);
        }
        var receiptIds = await receiptQuery.Select(r => r.Id).ToListAsync();

        if (!receiptIds.Any())
        {
            return 0;
        }

        // Find all transactions that reference these receipts via MatchedReceiptId
        var transactions = await _dbContext.Transactions
            .Where(t => t.MatchedReceiptId.HasValue && receiptIds.Contains(t.MatchedReceiptId.Value))
            .ToListAsync();

        // Clear the MatchedReceiptId to avoid FK constraint violation
        foreach (var transaction in transactions)
        {
            transaction.MatchedReceiptId = null;
        }

        if (transactions.Any())
        {
            await _dbContext.SaveChangesAsync();
        }

        return transactions.Count;
    }

    private async Task<(int receiptsDeleted, int blobsDeleted, List<string> warnings)> DeleteReceiptsAsync(
        Guid userId, DateTime? createdAfter)
    {
        var warnings = new List<string>();

        var query = _dbContext.Receipts.Where(r => r.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(r => r.CreatedAt > createdAfter.Value);
        }

        var receipts = await query.ToListAsync();
        var blobsDeleted = 0;

        // Delete blobs first
        foreach (var receipt in receipts)
        {
            if (!string.IsNullOrEmpty(receipt.BlobUrl))
            {
                try
                {
                    await _blobStorageService.DeleteAsync(receipt.BlobUrl);
                    blobsDeleted++;
                }
                catch (Exception ex)
                {
                    var warning = $"Blob '{receipt.BlobUrl}' not found in storage: {ex.Message}";
                    warnings.Add(warning);
                    _logger.LogWarning("Failed to delete blob {BlobUrl}: {Error}", receipt.BlobUrl, ex.Message);
                }
            }
        }

        // Then delete database records
        _dbContext.Receipts.RemoveRange(receipts);
        await _dbContext.SaveChangesAsync();

        return (receipts.Count, blobsDeleted, warnings);
    }

    private async Task<int> DeleteTransactionsAsync(Guid userId, DateTime? createdAfter)
    {
        var query = _dbContext.Transactions.Where(t => t.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(t => t.CreatedAt > createdAfter.Value);
        }

        var transactions = await query.ToListAsync();
        _dbContext.Transactions.RemoveRange(transactions);
        await _dbContext.SaveChangesAsync();

        return transactions.Count;
    }

    private async Task<int> DeleteImportsAsync(Guid userId, DateTime? createdAfter)
    {
        var query = _dbContext.StatementImports.Where(i => i.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(i => i.CreatedAt > createdAfter.Value);
        }

        var imports = await query.ToListAsync();
        _dbContext.StatementImports.RemoveRange(imports);
        await _dbContext.SaveChangesAsync();

        return imports.Count;
    }

    private async Task<int> DeleteExpenseLinesAsync(Guid userId, DateTime? createdAfter)
    {
        // Get all expense lines for reports owned by this user
        var query = _dbContext.ExpenseLines
            .Where(el => el.Report.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(el => el.CreatedAt > createdAfter.Value);
        }

        var lines = await query.ToListAsync();
        _dbContext.ExpenseLines.RemoveRange(lines);
        await _dbContext.SaveChangesAsync();

        return lines.Count;
    }

    private async Task<int> DeleteExpenseReportsAsync(Guid userId, DateTime? createdAfter)
    {
        var query = _dbContext.ExpenseReports.Where(r => r.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(r => r.CreatedAt > createdAfter.Value);
        }

        var reports = await query.ToListAsync();
        _dbContext.ExpenseReports.RemoveRange(reports);
        await _dbContext.SaveChangesAsync();

        return reports.Count;
    }

    private async Task<int> DeleteTierUsageLogsAsync(Guid userId, DateTime? createdAfter)
    {
        var query = _dbContext.TierUsageLogs.Where(t => t.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(t => t.CreatedAt > createdAfter.Value);
        }

        var logs = await query.ToListAsync();
        _dbContext.TierUsageLogs.RemoveRange(logs);
        await _dbContext.SaveChangesAsync();

        return logs.Count;
    }

    private async Task<int> DeleteExpenseEmbeddingsAsync(Guid userId, DateTime? createdAfter)
    {
        // Use ExecuteDeleteAsync to avoid materializing vector columns
        // (pgvector types can't be deserialized as System.Object)
        var query = _dbContext.ExpenseEmbeddings.Where(e => e.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(e => e.CreatedAt > createdAfter.Value);
        }

        return await query.ExecuteDeleteAsync();
    }

    private async Task<int> DeleteTravelPeriodsAsync(Guid userId, DateTime? createdAfter)
    {
        var query = _dbContext.TravelPeriods.Where(t => t.UserId == userId);

        if (createdAfter.HasValue)
        {
            query = query.Where(t => t.CreatedAt > createdAfter.Value);
        }

        var periods = await query.ToListAsync();
        _dbContext.TravelPeriods.RemoveRange(periods);
        await _dbContext.SaveChangesAsync();

        return periods.Count;
    }
}
#endif
