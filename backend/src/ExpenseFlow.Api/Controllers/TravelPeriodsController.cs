using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for travel period management operations.
/// </summary>
[Authorize]
public class TravelPeriodsController : ApiControllerBase
{
    private readonly ITravelDetectionService _travelDetectionService;
    private readonly IReceiptRepository _receiptRepository;
    private readonly IUserService _userService;
    private readonly ILogger<TravelPeriodsController> _logger;

    public TravelPeriodsController(
        ITravelDetectionService travelDetectionService,
        IReceiptRepository receiptRepository,
        IUserService userService,
        ILogger<TravelPeriodsController> logger)
    {
        _travelDetectionService = travelDetectionService;
        _receiptRepository = receiptRepository;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets paginated list of travel periods with optional filters.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <returns>Paginated list of travel periods.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(TravelPeriodListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TravelPeriodListResponseDto>> GetTravelPeriods(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);

        var response = await _travelDetectionService.GetTravelPeriodsAsync(
            user.Id, page, pageSize, startDate, endDate);

        return Ok(response);
    }

    /// <summary>
    /// Gets travel period details by ID.
    /// </summary>
    /// <param name="id">Travel period ID.</param>
    /// <returns>Travel period details or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TravelPeriodDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelPeriodDetailDto>> GetTravelPeriod(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var travelPeriod = await _travelDetectionService.GetTravelPeriodAsync(user.Id, id);
        if (travelPeriod == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Travel period not found",
                Detail = $"No travel period found with ID {id}"
            });
        }

        return Ok(travelPeriod);
    }

    /// <summary>
    /// Creates a new manual travel period.
    /// </summary>
    /// <param name="request">Travel period creation request.</param>
    /// <returns>Created travel period.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TravelPeriodDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TravelPeriodDetailDto>> CreateTravelPeriod(
        [FromBody] CreateTravelPeriodRequestDto request)
    {
        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid date range",
                Detail = "End date cannot be before start date"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        var travelPeriod = await _travelDetectionService.CreateTravelPeriodAsync(user.Id, request);

        _logger.LogInformation(
            "Created manual travel period {TravelPeriodId} for user {UserId}",
            travelPeriod.Id, user.Id);

        return CreatedAtAction(
            nameof(GetTravelPeriod),
            new { id = travelPeriod.Id },
            travelPeriod);
    }

    /// <summary>
    /// Updates an existing travel period.
    /// </summary>
    /// <param name="id">Travel period ID.</param>
    /// <param name="request">Travel period update request.</param>
    /// <returns>Updated travel period or 404 if not found.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TravelPeriodDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelPeriodDetailDto>> UpdateTravelPeriod(
        Guid id,
        [FromBody] UpdateTravelPeriodRequestDto request)
    {
        if (request.EndDate < request.StartDate)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid date range",
                Detail = "End date cannot be before start date"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        var travelPeriod = await _travelDetectionService.UpdateTravelPeriodAsync(user.Id, id, request);
        if (travelPeriod == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Travel period not found",
                Detail = $"No travel period found with ID {id}"
            });
        }

        _logger.LogInformation(
            "Updated travel period {TravelPeriodId} for user {UserId}",
            id, user.Id);

        return Ok(travelPeriod);
    }

    /// <summary>
    /// Deletes a travel period.
    /// </summary>
    /// <param name="id">Travel period ID.</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTravelPeriod(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var deleted = await _travelDetectionService.DeleteTravelPeriodAsync(user.Id, id);
        if (!deleted)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Travel period not found",
                Detail = $"No travel period found with ID {id}"
            });
        }

        _logger.LogInformation(
            "Deleted travel period {TravelPeriodId} for user {UserId}",
            id, user.Id);

        return NoContent();
    }

    /// <summary>
    /// Gets expenses within a travel period.
    /// </summary>
    /// <param name="id">Travel period ID.</param>
    /// <returns>List of expenses within the travel period.</returns>
    [HttpGet("{id:guid}/expenses")]
    [ProducesResponseType(typeof(TravelExpenseListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelExpenseListResponseDto>> GetTravelExpenses(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        // Verify travel period exists
        var travelPeriod = await _travelDetectionService.GetTravelPeriodAsync(user.Id, id);
        if (travelPeriod == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Travel period not found",
                Detail = $"No travel period found with ID {id}"
            });
        }

        var expenses = await _travelDetectionService.GetTravelExpensesAsync(user.Id, id);
        return Ok(expenses);
    }

    /// <summary>
    /// Gets a timeline view of travel periods with linked expenses and summary statistics.
    /// </summary>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="includeExpenses">Whether to include expense details (default true).</param>
    /// <returns>Timeline response with periods, expenses, and summary.</returns>
    [HttpGet("timeline")]
    [ProducesResponseType(typeof(TravelTimelineResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<TravelTimelineResponseDto>> GetTimeline(
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] bool includeExpenses = true)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var timeline = await _travelDetectionService.GetTimelineAsync(
            user.Id, startDate, endDate, includeExpenses);

        return Ok(timeline);
    }

    /// <summary>
    /// Triggers travel detection from a specific receipt.
    /// </summary>
    /// <param name="receiptId">Receipt ID to analyze.</param>
    /// <returns>Detection result with created/updated travel period.</returns>
    [HttpPost("detect")]
    [ProducesResponseType(typeof(TravelDetectionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TravelDetectionResultDto>> DetectTravelPeriod(
        [FromQuery] Guid receiptId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var receipt = await _receiptRepository.GetByIdAsync(user.Id, receiptId);
        if (receipt == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Receipt not found",
                Detail = $"No receipt found with ID {receiptId}"
            });
        }

        var result = await _travelDetectionService.DetectFromReceiptAsync(receipt);

        _logger.LogInformation(
            "Travel detection from receipt {ReceiptId} for user {UserId}: {Action}",
            receiptId, user.Id, result.Action);

        return Ok(result);
    }
}
