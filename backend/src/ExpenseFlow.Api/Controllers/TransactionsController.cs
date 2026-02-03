using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for transaction management operations.
/// </summary>
[Authorize]
public class TransactionsController : ApiControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IStatementImportRepository _importRepository;
    private readonly IExpensePatternRepository _patternRepository;
    private readonly IExpensePredictionService _predictionService;
    private readonly IMatchRepository _matchRepository;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IUserService _userService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionRepository transactionRepository,
        IStatementImportRepository importRepository,
        IExpensePatternRepository patternRepository,
        IExpensePredictionService predictionService,
        IMatchRepository matchRepository,
        IReceiptRepository receiptRepository,
        IUserService userService,
        ILogger<TransactionsController> logger)
    {
        _transactionRepository = transactionRepository;
        _importRepository = importRepository;
        _patternRepository = patternRepository;
        _predictionService = predictionService;
        _matchRepository = matchRepository;
        _receiptRepository = receiptRepository;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated list of transactions with optional filters.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 50, max 200).</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="matchStatus">Optional filter by match status (matched, pending, unmatched, missing-receipt). Supports multiple values. 'missing-receipt' filters to Business expenses without receipts that aren't dismissed and not on submitted reports.</param>
    /// <param name="importId">Optional filter by specific import batch.</param>
    /// <param name="search">Optional text search on description (case-insensitive).</param>
    /// <param name="sortBy">Field to sort by: date (default), amount, description, merchant.</param>
    /// <param name="sortOrder">Sort direction: desc (default) or asc.</param>
    /// <param name="minAmount">Optional minimum amount filter.</param>
    /// <param name="maxAmount">Optional maximum amount filter.</param>
    /// <param name="hasPendingPrediction">Filter to transactions with pending expense predictions.</param>
    /// <returns>Paginated list of transactions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(TransactionListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionListResponse>> GetTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] List<string>? matchStatus = null,
        [FromQuery] Guid? importId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] bool? hasPendingPrediction = null)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var user = await _userService.GetOrCreateUserAsync(User);

        var (transactions, totalCount, unmatchedCount) = await _transactionRepository.GetPagedAsync(
            user.Id,
            page,
            pageSize,
            startDate,
            endDate,
            matchStatus,
            importId,
            search,
            sortBy,
            sortOrder,
            minAmount,
            maxAmount,
            hasPendingPrediction);

        // Batch-fetch predictions for all transactions in one query
        var transactionIds = transactions.Select(t => t.Id).ToList();
        var predictions = await _predictionService.GetPredictionsForTransactionsAsync(user.Id, transactionIds);

        var response = new TransactionListResponse
        {
            Transactions = transactions.Select(t => new TransactionSummaryDto
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                Description = t.Description,
                Amount = t.Amount,
                HasMatchedReceipt = t.MatchedReceiptId.HasValue,
                Prediction = predictions.TryGetValue(t.Id, out var prediction) ? prediction : null
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            UnmatchedCount = unmatchedCount
        };

        return Ok(response);
    }

    /// <summary>
    /// Gets available categories for transaction filtering.
    /// Returns distinct categories from user's expense patterns,
    /// merged with default categories for comprehensive filtering.
    /// </summary>
    /// <returns>List of categories with id and name.</returns>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(TransactionCategoriesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionCategoriesResponse>> GetCategories()
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        // Get user's custom categories from expense patterns
        var userCategories = await _patternRepository.GetDistinctCategoriesAsync(user.Id);

        // Default categories (always available for filtering)
        var defaultCategories = new List<string>
        {
            "Food & Dining",
            "Transportation",
            "Utilities",
            "Entertainment",
            "Shopping",
            "Travel",
            "Health & Medical",
            "Business",
            "Other"
        };

        // Merge and deduplicate (user categories first, then defaults)
        var allCategories = userCategories
            .Union(defaultCategories, StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .Select(c => new CategoryDto
            {
                Id = c.ToLowerInvariant().Replace(" & ", "-").Replace(" ", "-"),
                Name = c
            })
            .ToList();

        return Ok(new TransactionCategoriesResponse { Categories = allCategories });
    }

    /// <summary>
    /// Gets available tags for transaction filtering.
    /// Currently returns an empty array as tags are not implemented on transactions.
    /// TODO: Add Tags field to Transaction entity and implement tag management.
    /// </summary>
    /// <returns>List of tags (currently empty).</returns>
    [HttpGet("tags")]
    [ProducesResponseType(typeof(TransactionTagsResponse), StatusCodes.Status200OK)]
    public ActionResult<TransactionTagsResponse> GetTags()
    {
        // Tags are not currently implemented on transactions.
        // Frontend expects this endpoint to exist for filter dropdowns.
        // Return empty array to prevent 404 errors.
        return Ok(new TransactionTagsResponse { Tags = new List<string>() });
    }

    /// <summary>
    /// Gets smart filter suggestions based on the user's transaction data.
    /// Analyzes transaction patterns to suggest relevant filters like
    /// top merchants, date ranges, and amount brackets.
    /// </summary>
    /// <returns>List of filter suggestions with relevance scores.</returns>
    [HttpGet("filter-suggestions")]
    [ProducesResponseType(typeof(FilterSuggestionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<FilterSuggestionsResponse>> GetFilterSuggestions()
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var stats = await _transactionRepository.GetFilterSuggestionsDataAsync(user.Id);

        var suggestions = new List<FilterSuggestionDto>();

        // Suggest top merchants (only those with multiple transactions)
        foreach (var merchant in stats.TopMerchants.Where(m => m.TransactionCount >= 2).Take(5))
        {
            suggestions.Add(new FilterSuggestionDto
            {
                Type = "merchant",
                Label = merchant.Merchant.Length > 30
                    ? merchant.Merchant[..27] + "..."
                    : merchant.Merchant,
                Description = $"{merchant.TransactionCount} transactions, ${merchant.TotalAmount:N2} total",
                FilterValue = merchant.Merchant,
                TransactionCount = merchant.TransactionCount,
                RelevanceScore = Math.Min(100, merchant.TransactionCount * 10)
            });
        }

        // Suggest unmatched filter if there are unmatched transactions
        if (stats.MatchStats.UnmatchedCount > 0)
        {
            var unmatchedPct = (double)stats.MatchStats.UnmatchedCount / stats.TotalTransactions * 100;
            suggestions.Add(new FilterSuggestionDto
            {
                Type = "match_status",
                Label = "Unmatched Transactions",
                Description = $"{stats.MatchStats.UnmatchedCount} transactions need receipts ({unmatchedPct:N0}%)",
                FilterValue = new[] { "unmatched" },
                TransactionCount = stats.MatchStats.UnmatchedCount,
                RelevanceScore = Math.Min(100, (int)(unmatchedPct * 1.5))
            });
        }

        // Suggest pending matches if any
        if (stats.MatchStats.PendingCount > 0)
        {
            suggestions.Add(new FilterSuggestionDto
            {
                Type = "match_status",
                Label = "Pending Review",
                Description = $"{stats.MatchStats.PendingCount} proposed matches awaiting approval",
                FilterValue = new[] { "pending" },
                TransactionCount = stats.MatchStats.PendingCount,
                RelevanceScore = 80 // High relevance - actionable items
            });
        }

        // Suggest high-value filter if there are significant high-value transactions
        if (stats.AmountStats.HighValueCount >= 5)
        {
            suggestions.Add(new FilterSuggestionDto
            {
                Type = "amount_range",
                Label = "High Value ($100+)",
                Description = $"{stats.AmountStats.HighValueCount} transactions of $100 or more",
                FilterValue = new { min = 100, max = (decimal?)null },
                TransactionCount = stats.AmountStats.HighValueCount,
                RelevanceScore = 60
            });
        }

        // Suggest date range filters
        foreach (var period in stats.RecentPeriods.Take(3))
        {
            // Only add if period has meaningful activity
            var activityPct = (double)period.TransactionCount / stats.TotalTransactions * 100;
            if (activityPct >= 5)
            {
                suggestions.Add(new FilterSuggestionDto
                {
                    Type = "date_range",
                    Label = period.PeriodName,
                    Description = $"{period.TransactionCount} transactions ({activityPct:N0}% of total)",
                    FilterValue = new
                    {
                        startDate = period.StartDate.ToString("yyyy-MM-dd"),
                        endDate = period.EndDate.ToString("yyyy-MM-dd")
                    },
                    TransactionCount = period.TransactionCount,
                    RelevanceScore = Math.Min(90, (int)(activityPct * 2))
                });
            }
        }

        // Sort by relevance score descending
        suggestions = suggestions.OrderByDescending(s => s.RelevanceScore).ToList();

        return Ok(new FilterSuggestionsResponse
        {
            Suggestions = suggestions,
            TotalTransactions = stats.TotalTransactions,
            EarliestDate = stats.EarliestDate,
            LatestDate = stats.LatestDate
        });
    }

    /// <summary>
    /// Gets transaction details by ID.
    /// </summary>
    /// <param name="id">Transaction ID.</param>
    /// <returns>Transaction details or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionDetailDto>> GetTransaction(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var transaction = await _transactionRepository.GetByIdAsync(user.Id, id);
        if (transaction == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Transaction not found",
                Detail = $"No transaction found with ID {id}"
            });
        }

        // Get import info for the filename
        var importFileName = string.Empty;
        if (transaction.ImportId != Guid.Empty)
        {
            var import = await _importRepository.GetByIdAsync(user.Id, transaction.ImportId);
            importFileName = import?.FileName ?? string.Empty;
        }

        // Get matched receipt info if available
        MatchedReceiptInfoDto? matchedReceiptInfo = null;
        if (transaction.MatchedReceiptId.HasValue)
        {
            var receipt = await _receiptRepository.GetByIdAsync(user.Id, transaction.MatchedReceiptId.Value);
            if (receipt != null)
            {
                // Find the confirmed match record to get the matchId
                var match = await _matchRepository.GetByTransactionIdAsync(transaction.Id, user.Id);

                matchedReceiptInfo = new MatchedReceiptInfoDto
                {
                    MatchId = match?.Id ?? Guid.Empty,
                    Id = receipt.Id,
                    Vendor = receipt.VendorExtracted,
                    Date = receipt.DateExtracted,
                    Amount = receipt.AmountExtracted,
                    ThumbnailUrl = receipt.ThumbnailUrl,
                    MatchConfidence = match?.ConfidenceScore ?? 0m
                };
            }
        }

        var response = new TransactionDetailDto
        {
            Id = transaction.Id,
            TransactionDate = transaction.TransactionDate,
            PostDate = transaction.PostDate,
            Description = transaction.Description,
            OriginalDescription = transaction.OriginalDescription,
            Amount = transaction.Amount,
            MatchedReceiptId = transaction.MatchedReceiptId,
            ImportId = transaction.ImportId,
            ImportFileName = importFileName,
            CreatedAt = transaction.CreatedAt,
            MatchedReceipt = matchedReceiptInfo
        };

        return Ok(response);
    }

    /// <summary>
    /// Deletes a transaction.
    /// </summary>
    /// <param name="id">Transaction ID.</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var transaction = await _transactionRepository.GetByIdAsync(user.Id, id);
        if (transaction == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Transaction not found",
                Detail = $"No transaction found with ID {id}"
            });
        }

        await _transactionRepository.DeleteAsync(transaction);
        await _transactionRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted transaction {TransactionId} for user {UserId}",
            id, user.Id);

        return NoContent();
    }

    #region Reimbursability Management

    /// <summary>
    /// Marks a transaction as reimbursable (business expense).
    /// Creates or updates a manual override prediction.
    /// </summary>
    /// <param name="id">Transaction ID.</param>
    /// <returns>Prediction action result.</returns>
    [HttpPost("{id:guid}/reimbursable")]
    [ProducesResponseType(typeof(PredictionActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PredictionActionResponseDto>> MarkAsReimbursable(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var result = await _predictionService.MarkTransactionReimbursableAsync(user.Id, id);

            _logger.LogInformation(
                "Marked transaction {TransactionId} as reimbursable for user {UserId}",
                id, user.Id);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Transaction not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Marks a transaction as not reimbursable (personal expense).
    /// Creates or updates a manual override prediction.
    /// </summary>
    /// <param name="id">Transaction ID.</param>
    /// <returns>Prediction action result.</returns>
    [HttpPost("{id:guid}/not-reimbursable")]
    [ProducesResponseType(typeof(PredictionActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PredictionActionResponseDto>> MarkAsNotReimbursable(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var result = await _predictionService.MarkTransactionNotReimbursableAsync(user.Id, id);

            _logger.LogInformation(
                "Marked transaction {TransactionId} as not reimbursable for user {UserId}",
                id, user.Id);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Transaction not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Clears a manual reimbursability override, allowing automatic re-prediction.
    /// Only works on transactions with manual overrides.
    /// </summary>
    /// <param name="id">Transaction ID.</param>
    /// <returns>Prediction action result.</returns>
    [HttpDelete("{id:guid}/reimbursability-override")]
    [ProducesResponseType(typeof(PredictionActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PredictionActionResponseDto>> ClearReimbursabilityOverride(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var result = await _predictionService.ClearManualOverrideAsync(user.Id, id);

            _logger.LogInformation(
                "Cleared reimbursability override for transaction {TransactionId} for user {UserId}",
                id, user.Id);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found") || ex.Message.Contains("No manual override"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Override not found",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Bulk marks multiple transactions as reimbursable or not reimbursable.
    /// Creates or updates manual override predictions for all specified transactions.
    /// </summary>
    /// <param name="request">Bulk reimbursability request.</param>
    /// <returns>Bulk action result with success/failure counts.</returns>
    [HttpPost("bulk/reimbursability")]
    [ProducesResponseType(typeof(BulkTransactionReimbursabilityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BulkTransactionReimbursabilityResponseDto>> BulkMarkReimbursability(
        [FromBody] BulkTransactionReimbursabilityRequestDto request)
    {
        if (request.TransactionIds == null || request.TransactionIds.Count == 0)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "At least one transaction ID is required"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);
        var result = await _predictionService.BulkMarkTransactionsAsync(user.Id, request);

        var action = request.IsReimbursable ? "reimbursable" : "not reimbursable";
        _logger.LogInformation(
            "Bulk marked {Count} transactions as {Action} for user {UserId}: {SuccessCount} succeeded, {FailedCount} failed",
            request.TransactionIds.Count, action, user.Id, result.SuccessCount, result.FailedCount);

        return Ok(result);
    }

    #endregion
}
