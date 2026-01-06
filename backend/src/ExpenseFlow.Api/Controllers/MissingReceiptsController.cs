using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for managing missing receipts - transactions marked as reimbursable but lacking matched receipts.
/// Feature 026: Missing Receipts UI
/// </summary>
[Authorize]
[Route("api/missing-receipts")]
public class MissingReceiptsController : ApiControllerBase
{
    private readonly IMissingReceiptService _missingReceiptService;
    private readonly IUserService _userService;
    private readonly ILogger<MissingReceiptsController> _logger;

    public MissingReceiptsController(
        IMissingReceiptService missingReceiptService,
        IUserService userService,
        ILogger<MissingReceiptsController> logger)
    {
        _missingReceiptService = missingReceiptService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of missing receipts.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1)</param>
    /// <param name="pageSize">Items per page (1-100, default 25)</param>
    /// <param name="sortBy">Sort field: date, amount, or vendor (default date)</param>
    /// <param name="sortOrder">Sort order: asc or desc (default desc)</param>
    /// <param name="includeDismissed">Include dismissed items (default false)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paginated list of missing receipts</returns>
    [HttpGet]
    [ProducesResponseType(typeof(MissingReceiptsListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MissingReceiptsListResponseDto>> GetMissingReceipts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string sortBy = "date",
        [FromQuery] string sortOrder = "desc",
        [FromQuery] bool includeDismissed = false,
        CancellationToken ct = default)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Getting missing receipts for user {UserId}, page {Page}, pageSize {PageSize}",
            user.Id, page, pageSize);

        var result = await _missingReceiptService.GetMissingReceiptsAsync(
            user.Id, page, pageSize, sortBy, sortOrder, includeDismissed, ct);

        return Ok(result);
    }

    /// <summary>
    /// Gets widget summary data for the missing receipts dashboard card.
    /// Returns total count and top 3 most recent missing receipts.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Widget summary with count and recent items</returns>
    [HttpGet("widget")]
    [ProducesResponseType(typeof(MissingReceiptsWidgetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MissingReceiptsWidgetDto>> GetWidget(CancellationToken ct = default)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation("Getting missing receipts widget for user {UserId}", user.Id);

        var result = await _missingReceiptService.GetWidgetDataAsync(user.Id, ct);

        return Ok(result);
    }

    /// <summary>
    /// Updates the receipt URL for a transaction.
    /// Pass null or empty string to clear the URL.
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="request">Request containing the new URL</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated transaction summary</returns>
    [HttpPatch("{transactionId:guid}/url")]
    [ProducesResponseType(typeof(MissingReceiptSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MissingReceiptSummaryDto>> UpdateReceiptUrl(
        Guid transactionId,
        [FromBody] UpdateReceiptUrlRequestDto request,
        CancellationToken ct = default)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Updating receipt URL for transaction {TransactionId}, user {UserId}",
            transactionId, user.Id);

        var result = await _missingReceiptService.UpdateReceiptUrlAsync(
            user.Id, transactionId, request.ReceiptUrl, ct);

        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Transaction {transactionId} not found or does not belong to current user.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Dismisses or restores a transaction from the missing receipts list.
    /// </summary>
    /// <param name="transactionId">Transaction ID</param>
    /// <param name="request">Request containing dismiss flag (true to dismiss, false/null to restore)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Updated transaction summary</returns>
    [HttpPatch("{transactionId:guid}/dismiss")]
    [ProducesResponseType(typeof(MissingReceiptSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MissingReceiptSummaryDto>> DismissMissingReceipt(
        Guid transactionId,
        [FromBody] DismissReceiptRequestDto request,
        CancellationToken ct = default)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Dismissing transaction {TransactionId} for user {UserId}, dismiss: {Dismiss}",
            transactionId, user.Id, request.Dismiss);

        var result = await _missingReceiptService.DismissTransactionAsync(
            user.Id, transactionId, request.Dismiss, ct);

        if (result == null)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Not Found",
                Detail = $"Transaction {transactionId} not found or does not belong to current user.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return Ok(result);
    }
}
