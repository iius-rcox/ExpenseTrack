using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for AI-powered expense categorization (GL codes and departments).
/// </summary>
[Authorize]
public class CategorizationController : ApiControllerBase
{
    private readonly ICategorizationService _categorizationService;
    private readonly ITierUsageService _tierUsageService;
    private readonly IUserService _userService;
    private readonly ILogger<CategorizationController> _logger;

    public CategorizationController(
        ICategorizationService categorizationService,
        ITierUsageService tierUsageService,
        IUserService userService,
        ILogger<CategorizationController> logger)
    {
        _categorizationService = categorizationService;
        _tierUsageService = tierUsageService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets GL code suggestions for a transaction using tiered approach.
    /// Tier 1: Vendor alias default, Tier 2: Embedding similarity, Tier 3: AI inference.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>GL code suggestions with confidence and tier information.</returns>
    [HttpGet("transactions/{transactionId:guid}/gl-suggestions")]
    [ProducesResponseType(typeof(GLSuggestionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GLSuggestionsDto>> GetGLSuggestions(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var result = await _categorizationService.GetGLSuggestionsAsync(
                transactionId,
                user.Id,
                cancellationToken);

            if (result.Message == "Transaction not found")
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Transaction not found",
                    Detail = $"Transaction {transactionId} was not found"
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting GL suggestions for transaction {TransactionId}", transactionId);

            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetailsResponse
            {
                Title = "Suggestion service unavailable",
                Detail = "An error occurred while getting GL suggestions. Please retry."
            });
        }
    }

    /// <summary>
    /// Gets department suggestions for a transaction using tiered approach.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Department suggestions with confidence and tier information.</returns>
    [HttpGet("transactions/{transactionId:guid}/dept-suggestions")]
    [ProducesResponseType(typeof(DepartmentSuggestionsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DepartmentSuggestionsDto>> GetDepartmentSuggestions(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var result = await _categorizationService.GetDepartmentSuggestionsAsync(
                transactionId,
                user.Id,
                cancellationToken);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department suggestions for transaction {TransactionId}", transactionId);

            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetailsResponse
            {
                Title = "Suggestion service unavailable",
                Detail = "An error occurred while getting department suggestions. Please retry."
            });
        }
    }

    /// <summary>
    /// Gets combined GL and department suggestions for a transaction.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Combined categorization suggestions.</returns>
    [HttpGet("transactions/{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionCategorizationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<TransactionCategorizationDto>> GetCategorization(
        Guid transactionId,
        CancellationToken cancellationToken)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var result = await _categorizationService.GetCategorizationAsync(
                transactionId,
                user.Id,
                cancellationToken);

            if (result.NormalizedDescription == string.Empty && result.GL.TopSuggestion == null)
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Transaction not found",
                    Detail = $"Transaction {transactionId} was not found"
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categorization for transaction {TransactionId}", transactionId);

            Response.Headers.Append("Retry-After", "30");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetailsResponse
            {
                Title = "Categorization service unavailable",
                Detail = "An error occurred while getting categorization. Please retry."
            });
        }
    }

    /// <summary>
    /// Confirms user's categorization selection, creating verified embedding and updating vendor alias.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <param name="request">Confirmation request with selected GL code and department.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Confirmation result with learning feedback.</returns>
    [HttpPost("transactions/{transactionId:guid}/confirm")]
    [ProducesResponseType(typeof(CategorizationConfirmationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategorizationConfirmationDto>> ConfirmCategorization(
        Guid transactionId,
        [FromBody] CategorizationConfirmRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Request body is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.GLCode))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "GL code is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.DepartmentCode))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Department code is required"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var result = await _categorizationService.ConfirmCategorizationAsync(
                transactionId,
                user.Id,
                request.GLCode,
                request.DepartmentCode,
                request.AcceptedSuggestion,
                cancellationToken);

            if (result.Message == "Transaction not found")
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Transaction not found",
                    Detail = $"Transaction {transactionId} was not found"
                });
            }

            _logger.LogInformation(
                "User {UserId} confirmed categorization for transaction {TransactionId}: GL={GLCode}, Dept={Dept}",
                user.Id, transactionId, request.GLCode, request.DepartmentCode);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming categorization for transaction {TransactionId}", transactionId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetailsResponse
            {
                Title = "Confirmation failed",
                Detail = "An error occurred while confirming categorization."
            });
        }
    }

    /// <summary>
    /// Skips AI suggestion for manual categorization.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <param name="request">Skip request with reason.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Skip confirmation.</returns>
    [HttpPost("transactions/{transactionId:guid}/skip")]
    [ProducesResponseType(typeof(CategorizationSkipDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategorizationSkipDto>> SkipSuggestion(
        Guid transactionId,
        [FromBody] CategorizationSkipRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Skip reason is required"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        var result = await _categorizationService.SkipSuggestionAsync(
            transactionId,
            user.Id,
            request.Reason,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets tier usage statistics for cost monitoring.
    /// </summary>
    /// <param name="startDate">Start of date range (default: 30 days ago).</param>
    /// <param name="endDate">End of date range (default: today).</param>
    /// <param name="operationType">Optional filter by operation type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tier usage statistics.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(TierUsageStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TierUsageStatsDto>> GetStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? operationType = null,
        CancellationToken cancellationToken = default)
    {
        var effectiveStartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
        var effectiveEndDate = endDate ?? DateTime.UtcNow;

        var stats = await _tierUsageService.GetStatsAsync(
            effectiveStartDate,
            effectiveEndDate,
            operationType,
            cancellationToken);

        return Ok(stats);
    }
}
