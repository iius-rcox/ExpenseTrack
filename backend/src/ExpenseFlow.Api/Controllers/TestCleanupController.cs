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
        "imports"
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
            // 1. Delete matches first (references receipts and transactions)
            // 2. Delete receipts (and their blobs)
            // 3. Delete transactions
            // 4. Delete statement imports

            // 1. Delete matches
            if (cleanAll || entityTypes.Contains("matches", StringComparer.OrdinalIgnoreCase))
            {
                response.DeletedCounts.Matches = await DeleteMatchesAsync(user.Id, createdAfter);
                _logger.LogInformation("Deleted {Count} matches", response.DeletedCounts.Matches);
            }

            // 2. Delete receipts (and their blobs)
            if (cleanAll || entityTypes.Contains("receipts", StringComparer.OrdinalIgnoreCase))
            {
                var (receiptsDeleted, blobsDeleted, blobWarnings) = await DeleteReceiptsAsync(user.Id, createdAfter);
                response.DeletedCounts.Receipts = receiptsDeleted;
                response.DeletedCounts.BlobsDeleted = blobsDeleted;
                warnings.AddRange(blobWarnings);
                _logger.LogInformation("Deleted {ReceiptCount} receipts and {BlobCount} blobs", receiptsDeleted, blobsDeleted);
            }

            // 3. Delete transactions
            if (cleanAll || entityTypes.Contains("transactions", StringComparer.OrdinalIgnoreCase))
            {
                response.DeletedCounts.Transactions = await DeleteTransactionsAsync(user.Id, createdAfter);
                _logger.LogInformation("Deleted {Count} transactions", response.DeletedCounts.Transactions);
            }

            // 4. Delete statement imports
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
}
#endif
