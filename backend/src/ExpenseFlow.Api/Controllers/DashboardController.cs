using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for dashboard metrics and activity.
/// </summary>
[Authorize]
public class DashboardController : ApiControllerBase
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly IUserService _userService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        ExpenseFlowDbContext dbContext,
        IUserService userService,
        ILogger<DashboardController> logger)
    {
        _dbContext = dbContext;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard metrics for the current user.
    /// </summary>
    /// <returns>Dashboard metrics including counts and spending summary</returns>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(DashboardMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardMetricsDto>> GetMetrics()
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation("Dashboard metrics requested by user {UserId}", user.Id);

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateOnly(now.Year, now.Month, 1);
        var previousMonthStart = currentMonthStart.AddMonths(-1);

        // NOTE: DbContext is NOT thread-safe, so we must run queries sequentially.
        // Do not use Task.WhenAll with multiple queries on the same DbContext instance.

        // Pending receipts = Uploaded or Processing status
        var pendingReceiptsCount = await _dbContext.Receipts
            .Where(r => r.UserId == user.Id &&
                       (r.Status == ReceiptStatus.Uploaded || r.Status == ReceiptStatus.Processing))
            .CountAsync();

        // Unmatched transactions = no matched receipt AND not part of a group (groups are matched separately)
        var unmatchedTransactionsCount = await _dbContext.Transactions
            .Where(t => t.UserId == user.Id && t.MatchedReceiptId == null && t.GroupId == null)
            .CountAsync();

        // Pending matches = Proposed status
        var pendingMatchesCount = await _dbContext.ReceiptTransactionMatches
            .Where(m => m.UserId == user.Id && m.Status == MatchProposalStatus.Proposed)
            .CountAsync();

        // Draft reports
        var draftReportsCount = await _dbContext.ExpenseReports
            .Where(r => r.UserId == user.Id && r.Status == ReportStatus.Draft && !r.IsDeleted)
            .CountAsync();

        // Monthly spending (positive amounts are expenses in this schema)
        var currentMonth = await _dbContext.Transactions
            .Where(t => t.UserId == user.Id &&
                       t.TransactionDate >= currentMonthStart &&
                       t.Amount > 0)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        var previousMonth = await _dbContext.Transactions
            .Where(t => t.UserId == user.Id &&
                        t.TransactionDate >= previousMonthStart &&
                        t.TransactionDate < currentMonthStart &&
                        t.Amount > 0)
            .SumAsync(t => (decimal?)t.Amount) ?? 0;
        var percentChange = previousMonth > 0
            ? Math.Round((currentMonth - previousMonth) / previousMonth * 100, 1)
            : 0;

        var metrics = new DashboardMetricsDto
        {
            PendingReceiptsCount = pendingReceiptsCount,
            UnmatchedTransactionsCount = unmatchedTransactionsCount,
            PendingMatchesCount = pendingMatchesCount,
            DraftReportsCount = draftReportsCount,
            MonthlySpending = new MonthlySpendingDto
            {
                CurrentMonth = currentMonth,
                PreviousMonth = previousMonth,
                PercentChange = percentChange
            }
        };

        return Ok(metrics);
    }

    /// <summary>
    /// Get recent activity for the current user.
    /// </summary>
    /// <param name="limit">Maximum number of items to return (default 10, max 50)</param>
    /// <returns>List of recent activity items</returns>
    [HttpGet("activity")]
    [ProducesResponseType(typeof(List<RecentActivityItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<RecentActivityItemDto>>> GetRecentActivity([FromQuery] int limit = 10)
    {
        if (limit < 1) limit = 10;
        if (limit > 50) limit = 50;

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation("Recent activity requested by user {UserId}, limit {Limit}", user.Id, limit);

        var activities = new List<RecentActivityItemDto>();

        // Get recent receipts
        var recentReceipts = await _dbContext.Receipts
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new RecentActivityItemDto
            {
                Type = "receipt_uploaded",
                Title = "Receipt uploaded",
                Description = r.VendorExtracted ?? r.OriginalFilename,
                Timestamp = r.CreatedAt
            })
            .ToListAsync();
        activities.AddRange(recentReceipts);

        // Get recent statement imports
        var recentImports = await _dbContext.StatementImports
            .Where(i => i.UserId == user.Id)
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .Select(i => new RecentActivityItemDto
            {
                Type = "statement_imported",
                Title = "Statement imported",
                Description = $"{i.TransactionCount} transactions from {i.FileName}",
                Timestamp = i.CreatedAt
            })
            .ToListAsync();
        activities.AddRange(recentImports);

        // Get recent confirmed matches
        var recentMatches = await _dbContext.ReceiptTransactionMatches
            .Where(m => m.UserId == user.Id && m.Status == MatchProposalStatus.Confirmed)
            .OrderByDescending(m => m.ConfirmedAt)
            .Take(limit)
            .Include(m => m.Receipt)
            .Select(m => new RecentActivityItemDto
            {
                Type = "match_confirmed",
                Title = "Match confirmed",
                Description = m.Receipt.VendorExtracted ?? "Receipt matched",
                Timestamp = m.ConfirmedAt ?? m.CreatedAt
            })
            .ToListAsync();
        activities.AddRange(recentMatches);

        // Get recent reports
        var recentReports = await _dbContext.ExpenseReports
            .Where(r => r.UserId == user.Id && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new RecentActivityItemDto
            {
                Type = "report_generated",
                Title = "Report created",
                Description = $"Period {r.Period} - ${r.TotalAmount:N2}",
                Timestamp = r.CreatedAt
            })
            .ToListAsync();
        activities.AddRange(recentReports);

        // Sort by timestamp and take top N
        var result = activities
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Get pending actions requiring user review (match reviews, categorization approvals).
    /// </summary>
    /// <param name="limit">Maximum number of actions to return (default 10, max 50)</param>
    /// <returns>List of pending actions</returns>
    [HttpGet("actions")]
    [ProducesResponseType(typeof(List<PendingActionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PendingActionDto>>> GetActions([FromQuery] int limit = 10)
    {
        if (limit < 1) limit = 10;
        if (limit > 50) limit = 50;

        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation("Pending actions requested by user {UserId}, limit {Limit}", user.Id, limit);

        // Get pending match reviews (Status = Proposed)
        var pendingMatches = await _dbContext.ReceiptTransactionMatches
            .Where(m => m.UserId == user.Id && m.Status == MatchProposalStatus.Proposed)
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Include(m => m.Receipt)
            .Include(m => m.Transaction)
            .Select(m => new PendingActionDto
            {
                Id = m.Id.ToString(),
                Type = "match_review",
                Title = $"Review match: {m.Receipt.VendorExtracted ?? "Receipt"} ↔ {m.Transaction.Description}",
                Description = $"Confidence: {m.ConfidenceScore:N1}% - Amount: ${m.Transaction.Amount:N2}",
                CreatedAt = m.CreatedAt,
                Metadata = new Dictionary<string, object>
                {
                    { "confidenceScore", m.ConfidenceScore },
                    { "receiptId", m.ReceiptId },
                    { "transactionId", m.TransactionId },
                    { "amount", m.Transaction.Amount }
                }
            })
            .ToListAsync();

        return Ok(pendingMatches);
    }
}
