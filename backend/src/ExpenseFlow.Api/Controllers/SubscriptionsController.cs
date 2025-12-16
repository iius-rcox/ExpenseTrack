using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for subscription detection and management operations.
/// </summary>
[Authorize]
public class SubscriptionsController : ApiControllerBase
{
    private readonly ISubscriptionDetectionService _subscriptionService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUserService _userService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(
        ISubscriptionDetectionService subscriptionService,
        ITransactionRepository transactionRepository,
        IUserService userService,
        ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _transactionRepository = transactionRepository;
        _userService = userService;
        _logger = logger;
    }

    #region Subscription CRUD

    /// <summary>
    /// Gets paginated list of subscriptions with optional filters.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="status">Optional status filter.</param>
    /// <returns>Paginated list of subscriptions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(SubscriptionListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionListResponseDto>> GetSubscriptions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] SubscriptionStatus? status = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);

        var response = await _subscriptionService.GetSubscriptionsAsync(
            user.Id, page, pageSize, status);

        return Ok(response);
    }

    /// <summary>
    /// Gets subscription details by ID.
    /// </summary>
    /// <param name="id">Subscription ID.</param>
    /// <returns>Subscription details or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDetailDto>> GetSubscription(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var subscription = await _subscriptionService.GetSubscriptionAsync(user.Id, id);
        if (subscription == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Subscription not found",
                Detail = $"No subscription found with ID {id}"
            });
        }

        return Ok(subscription);
    }

    /// <summary>
    /// Creates a new manual subscription.
    /// </summary>
    /// <param name="request">Subscription creation request.</param>
    /// <returns>Created subscription.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionDetailDto>> CreateSubscription(
        [FromBody] CreateSubscriptionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.VendorName))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Vendor name is required"
            });
        }

        if (request.ExpectedAmount <= 0)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Expected amount must be greater than zero"
            });
        }

        if (request.ExpectedDay < 1 || request.ExpectedDay > 31)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Expected day must be between 1 and 31"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        var subscription = await _subscriptionService.CreateSubscriptionAsync(user.Id, request);

        _logger.LogInformation(
            "Created manual subscription {SubscriptionId} for user {UserId}",
            subscription.Id, user.Id);

        return CreatedAtAction(
            nameof(GetSubscription),
            new { id = subscription.Id },
            subscription);
    }

    /// <summary>
    /// Updates an existing subscription.
    /// </summary>
    /// <param name="id">Subscription ID.</param>
    /// <param name="request">Subscription update request.</param>
    /// <returns>Updated subscription or 404 if not found.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDetailDto>> UpdateSubscription(
        Guid id,
        [FromBody] UpdateSubscriptionRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.VendorName))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Vendor name is required"
            });
        }

        if (request.ExpectedAmount <= 0)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Expected amount must be greater than zero"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        var subscription = await _subscriptionService.UpdateSubscriptionAsync(user.Id, id, request);
        if (subscription == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Subscription not found",
                Detail = $"No subscription found with ID {id}"
            });
        }

        _logger.LogInformation(
            "Updated subscription {SubscriptionId} for user {UserId}",
            id, user.Id);

        return Ok(subscription);
    }

    /// <summary>
    /// Deletes a subscription.
    /// </summary>
    /// <param name="id">Subscription ID.</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubscription(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var deleted = await _subscriptionService.DeleteSubscriptionAsync(user.Id, id);
        if (!deleted)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Subscription not found",
                Detail = $"No subscription found with ID {id}"
            });
        }

        _logger.LogInformation(
            "Deleted subscription {SubscriptionId} for user {UserId}",
            id, user.Id);

        return NoContent();
    }

    #endregion

    #region Detection

    /// <summary>
    /// Triggers subscription detection from a specific transaction.
    /// </summary>
    /// <param name="transactionId">Transaction ID to analyze.</param>
    /// <returns>Detection result with created/updated subscription.</returns>
    [HttpPost("detect")]
    [ProducesResponseType(typeof(SubscriptionDetectionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDetectionResultDto>> DetectSubscription(
        [FromQuery] Guid transactionId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var transaction = await _transactionRepository.GetByIdAsync(user.Id, transactionId);
        if (transaction == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Transaction not found",
                Detail = $"No transaction found with ID {transactionId}"
            });
        }

        var result = await _subscriptionService.DetectFromTransactionAsync(transaction);

        _logger.LogInformation(
            "Subscription detection from transaction {TransactionId} for user {UserId}: {Action}",
            transactionId, user.Id, result.Action);

        return Ok(result);
    }

    #endregion

    #region Alerts

    /// <summary>
    /// Gets subscription alerts for the current user.
    /// </summary>
    /// <param name="includeAcknowledged">Include acknowledged alerts (default false).</param>
    /// <returns>List of subscription alerts.</returns>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(SubscriptionAlertListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionAlertListResponseDto>> GetAlerts(
        [FromQuery] bool includeAcknowledged = false)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var alerts = await _subscriptionService.GetAlertsAsync(user.Id, includeAcknowledged);
        return Ok(alerts);
    }

    /// <summary>
    /// Acknowledges one or more subscription alerts.
    /// </summary>
    /// <param name="request">Alert acknowledgement request.</param>
    /// <returns>Number of alerts acknowledged.</returns>
    [HttpPost("alerts/acknowledge")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AcknowledgeAlerts([FromBody] AcknowledgeAlertRequestDto request)
    {
        if (request.AlertIds == null || request.AlertIds.Count == 0)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "At least one alert ID is required"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        var acknowledged = await _subscriptionService.AcknowledgeAlertsAsync(user.Id, request.AlertIds);

        _logger.LogInformation(
            "Acknowledged {Count} alerts for user {UserId}",
            acknowledged, user.Id);

        return Ok(new { acknowledged });
    }

    #endregion

    #region Monitoring

    /// <summary>
    /// Gets subscription monitoring summary dashboard data.
    /// </summary>
    /// <returns>Subscription monitoring summary.</returns>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(SubscriptionMonitoringSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SubscriptionMonitoringSummaryDto>> GetMonitoringSummary()
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var summary = await _subscriptionService.GetMonitoringSummaryAsync(user.Id);
        return Ok(summary);
    }

    /// <summary>
    /// Manually triggers a subscription check for the current month.
    /// </summary>
    /// <param name="month">Month to check (YYYY-MM format). Defaults to previous month.</param>
    /// <returns>List of generated alerts.</returns>
    [HttpPost("check")]
    [ProducesResponseType(typeof(List<SubscriptionAlertDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<SubscriptionAlertDto>>> RunManualCheck(
        [FromQuery] string? month = null)
    {
        // Default to previous month if not specified
        if (string.IsNullOrWhiteSpace(month))
        {
            month = DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM");
        }

        // Validate month format
        if (!System.Text.RegularExpressions.Regex.IsMatch(month, @"^\d{4}-\d{2}$"))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid month format",
                Detail = "Month must be in YYYY-MM format"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        var alerts = await _subscriptionService.RunMonthlyCheckAsync(user.Id, month);

        _logger.LogInformation(
            "Manual subscription check for {Month} generated {AlertCount} alerts for user {UserId}",
            month, alerts.Count, user.Id);

        return Ok(alerts);
    }

    #endregion
}
