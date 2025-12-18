using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for cache warming operations including historical data import and cache statistics.
/// </summary>
[Authorize]
public class CacheWarmingController : ApiControllerBase
{
    private readonly ICacheWarmingService _cacheWarmingService;
    private readonly IUserService _userService;
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<CacheWarmingController> _logger;
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public CacheWarmingController(
        ICacheWarmingService cacheWarmingService,
        IUserService userService,
        ExpenseFlowDbContext dbContext,
        ILogger<CacheWarmingController> logger)
    {
        _cacheWarmingService = cacheWarmingService;
        _userService = userService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Uploads historical expense data for cache warming.
    /// </summary>
    /// <param name="file">Excel file (.xlsx) containing historical expense data</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Created import job details</returns>
    [HttpPost("import")]
    [RequestSizeLimit(MaxFileSize)]
    [ProducesResponseType(typeof(ImportJobResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<ImportJobResponse>> UploadHistoricalData(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "No file provided",
                Detail = "Please upload an Excel file containing historical expense data."
            });
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid file format",
                Detail = "Only Excel (.xlsx) files are supported."
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        using var stream = file.OpenReadStream();
        var job = await _cacheWarmingService.ImportHistoricalDataAsync(stream, file.FileName, user.Id);

        _logger.LogInformation("Import job {JobId} created for user {UserId}", job.Id, user.Id);

        return AcceptedAtAction(
            nameof(GetImportJob),
            new { jobId = job.Id },
            MapToResponse(job));
    }

    /// <summary>
    /// Gets a paginated list of import jobs for the current user.
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (default 20, max 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of import jobs</returns>
    [HttpGet("jobs")]
    [ProducesResponseType(typeof(ImportJobListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ImportJobListResponse>> ListImportJobs(
        [FromQuery] ImportJobStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var user = await _userService.GetOrCreateUserAsync(User);

        var (jobs, totalCount) = await _cacheWarmingService.GetImportJobsAsync(
            user.Id, status, page, pageSize);

        return Ok(new ImportJobListResponse(
            jobs.Select(MapToResponse).ToList(),
            totalCount,
            page,
            pageSize));
    }

    /// <summary>
    /// Gets details of a specific import job.
    /// </summary>
    /// <param name="jobId">Import job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Import job details</returns>
    [HttpGet("jobs/{jobId:guid}")]
    [ProducesResponseType(typeof(ImportJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportJobResponse>> GetImportJob(
        Guid jobId,
        CancellationToken ct)
    {
        var job = await _cacheWarmingService.GetImportJobAsync(jobId);

        if (job == null)
        {
            return NotFound();
        }

        return Ok(MapToResponse(job));
    }

    /// <summary>
    /// Cancels a pending or processing import job.
    /// </summary>
    /// <param name="jobId">Import job ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>No content if cancelled, bad request if not cancellable</returns>
    [HttpDelete("jobs/{jobId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelImportJob(Guid jobId, CancellationToken ct)
    {
        var job = await _cacheWarmingService.GetImportJobAsync(jobId);

        if (job == null)
        {
            return NotFound();
        }

        var cancelled = await _cacheWarmingService.CancelImportJobAsync(jobId);

        if (!cancelled)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Cannot cancel job",
                Detail = $"Job is in {job.Status} status and cannot be cancelled."
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Gets error details for an import job.
    /// </summary>
    /// <param name="jobId">Import job ID</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Errors per page (default 50, max 100)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of import errors</returns>
    [HttpGet("jobs/{jobId:guid}/errors")]
    [ProducesResponseType(typeof(ImportErrorListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportErrorListResponse>> GetImportJobErrors(
        Guid jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var job = await _cacheWarmingService.GetImportJobAsync(jobId);

        if (job == null)
        {
            return NotFound();
        }

        var (errors, totalCount) = await _cacheWarmingService.GetImportJobErrorsAsync(
            jobId, page, pageSize);

        return Ok(new ImportErrorListResponse(
            errors.Select(e => new ImportErrorDto(e.LineNumber, e.ErrorMessage, e.RawData)).ToList(),
            totalCount,
            page,
            pageSize));
    }

    /// <summary>
    /// Gets overall cache statistics.
    /// </summary>
    /// <param name="period">Period for hit rate calculation (YYYY-MM format)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cache statistics</returns>
    [HttpGet("/api/cache/statistics")]
    [ProducesResponseType(typeof(CacheWarmingStatsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CacheWarmingStatsResponse>> GetCacheStatistics(
        [FromQuery] string? period,
        CancellationToken ct)
    {
        var targetPeriod = period ?? DateTime.UtcNow.ToString("yyyy-MM");

        // Calculate statistics from TierUsageLog
        var startDate = DateTime.ParseExact(targetPeriod + "-01", "yyyy-MM-dd", null);
        var endDate = startDate.AddMonths(1);

        var usageLogs = await _dbContext.TierUsageLogs
            .Where(l => l.CreatedAt >= startDate && l.CreatedAt < endDate)
            .ToListAsync(ct);

        var totalOps = usageLogs.Count;
        var tier1Hits = usageLogs.Count(l => l.TierUsed == 1);
        var tier2Hits = usageLogs.Count(l => l.TierUsed == 2);
        var tier3Hits = usageLogs.Count(l => l.TierUsed >= 3);

        var overall = new CacheWarmingStatsDto(
            TotalOperations: totalOps,
            Tier1Hits: tier1Hits,
            Tier2Hits: tier2Hits,
            Tier3Hits: tier3Hits,
            Tier1HitRate: totalOps > 0 ? (decimal)tier1Hits / totalOps : 0,
            Tier2HitRate: totalOps > 0 ? (decimal)tier2Hits / totalOps : 0,
            Tier3HitRate: totalOps > 0 ? (decimal)tier3Hits / totalOps : 0,
            EstimatedMonthlyCost: CalculateEstimatedCost(usageLogs),
            AvgResponseTimeMs: usageLogs.Count > 0 ? (int)usageLogs.Average(l => l.ResponseTimeMs) : 0,
            BelowTarget: (totalOps > 0 ? (decimal)tier1Hits / totalOps : 0) >= 0.5m
        );

        return Ok(new CacheWarmingStatsResponse(targetPeriod, overall, null));
    }

    /// <summary>
    /// Gets cache warming summary with counts by source.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Cache warming summary</returns>
    [HttpGet("/api/cache/statistics/warming-summary")]
    [ProducesResponseType(typeof(CacheWarmingSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CacheWarmingSummaryResponse>> GetWarmingSummary(CancellationToken ct)
    {
        // Get last completed import job
        var lastJob = await _dbContext.ImportJobs
            .Where(j => j.Status == ImportJobStatus.Completed)
            .OrderByDescending(j => j.CompletedAt)
            .FirstOrDefaultAsync(ct);

        // Count description cache entries
        var descriptionTotal = await _dbContext.DescriptionCaches.CountAsync(ct);
        var descriptionFromImport = lastJob?.CachedDescriptions ?? 0;

        // Count vendor aliases
        var aliasTotal = await _dbContext.VendorAliases.CountAsync(ct);
        var aliasFromImport = lastJob?.CreatedAliases ?? 0;

        // Count expense embeddings
        var embeddingTotal = await _dbContext.ExpenseEmbeddings.CountAsync(ct);
        var embeddingFromImport = lastJob?.GeneratedEmbeddings ?? 0;

        // Calculate expected hit rate (simplified estimate)
        var expectedHitRate = descriptionTotal > 0 ? Math.Min(0.7m, 0.3m + (descriptionTotal / 1000m) * 0.4m) : 0m;

        return Ok(new CacheWarmingSummaryResponse(
            new CacheCountBySourceDto(descriptionTotal, descriptionFromImport, descriptionTotal - descriptionFromImport),
            new CacheCountBySourceDto(aliasTotal, aliasFromImport, aliasTotal - aliasFromImport),
            new CacheCountBySourceDto(embeddingTotal, embeddingFromImport, embeddingTotal - embeddingFromImport),
            expectedHitRate,
            lastJob != null ? MapToResponse(lastJob) : null
        ));
    }

    private static ImportJobResponse MapToResponse(ImportJob job)
    {
        var percentComplete = job.TotalRecords > 0
            ? (double)job.ProcessedRecords / job.TotalRecords * 100
            : 0;

        return new ImportJobResponse(
            job.Id,
            job.Status.ToString(),
            job.SourceFileName,
            job.StartedAt,
            job.CompletedAt,
            new ImportProgressDto(
                job.TotalRecords,
                job.ProcessedRecords,
                job.CachedDescriptions,
                job.CreatedAliases,
                job.GeneratedEmbeddings,
                job.SkippedRecords,
                percentComplete
            )
        );
    }

    private static decimal? CalculateEstimatedCost(List<TierUsageLog> logs)
    {
        if (logs.Count == 0) return null;

        // Cost estimation based on tier usage
        // Tier 1: $0 (cache)
        // Tier 2: ~$0.00002 (embedding)
        // Tier 3: ~$0.0003 (GPT-4o-mini)
        // Tier 4: ~$0.01 (GPT-4o)
        var tier2Count = logs.Count(l => l.TierUsed == 2);
        var tier3Count = logs.Count(l => l.TierUsed == 3);
        var tier4Count = logs.Count(l => l.TierUsed >= 4);

        return (tier2Count * 0.00002m) + (tier3Count * 0.0003m) + (tier4Count * 0.01m);
    }
}
