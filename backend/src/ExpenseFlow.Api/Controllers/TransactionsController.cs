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
    private readonly IExpensePredictionService _predictionService;
    private readonly IUserService _userService;
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionRepository transactionRepository,
        IStatementImportRepository importRepository,
        IExpensePredictionService predictionService,
        IUserService userService,
        ILogger<TransactionsController> logger)
    {
        _transactionRepository = transactionRepository;
        _importRepository = importRepository;
        _predictionService = predictionService;
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
    /// <param name="matched">Optional filter by receipt match status.</param>
    /// <param name="importId">Optional filter by specific import batch.</param>
    /// <param name="search">Optional text search on description (case-insensitive).</param>
    /// <param name="sortBy">Field to sort by: date (default), amount, description.</param>
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
        [FromQuery] bool? matched = null,
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
            matched,
            importId,
            search,
            sortBy,
            sortOrder,
            minAmount,
            maxAmount,
            hasPendingPrediction);

        var response = new TransactionListResponse
        {
            Transactions = transactions.Select(t => new TransactionSummaryDto
            {
                Id = t.Id,
                TransactionDate = t.TransactionDate,
                Description = t.Description,
                Amount = t.Amount,
                HasMatchedReceipt = t.MatchedReceiptId.HasValue
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            UnmatchedCount = unmatchedCount
        };

        return Ok(response);
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
            CreatedAt = transaction.CreatedAt
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
