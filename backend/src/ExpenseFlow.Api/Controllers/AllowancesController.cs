using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for managing recurring expense allowances.
/// </summary>
[Authorize]
[Route("api/allowances")]
public class AllowancesController : ApiControllerBase
{
    private readonly IAllowanceService _allowanceService;
    private readonly IUserService _userService;
    private readonly ILogger<AllowancesController> _logger;

    public AllowancesController(
        IAllowanceService allowanceService,
        IUserService userService,
        ILogger<AllowancesController> logger)
    {
        _allowanceService = allowanceService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all recurring allowances for the current user.
    /// </summary>
    /// <param name="activeOnly">If true, returns only active allowances.</param>
    /// <returns>List of recurring allowances.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(AllowanceListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AllowanceListResponse>> GetAllowances(
        [FromQuery] bool? activeOnly = null)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogDebug(
            "Getting allowances for user {UserId}, activeOnly={ActiveOnly}",
            user.Id, activeOnly);

        var result = await _allowanceService.GetByUserAsync(user.Id, activeOnly);

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific recurring allowance by ID.
    /// </summary>
    /// <param name="id">The allowance ID.</param>
    /// <returns>The allowance if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AllowanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AllowanceResponse>> GetAllowance(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var allowance = await _allowanceService.GetByIdAsync(user.Id, id);

        if (allowance == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Allowance with ID {id} was not found"
            });
        }

        return Ok(allowance);
    }

    /// <summary>
    /// Creates a new recurring allowance.
    /// </summary>
    /// <param name="request">The create request.</param>
    /// <returns>The created allowance.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AllowanceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AllowanceResponse>> CreateAllowance(
        [FromBody] CreateAllowanceRequest request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Creating allowance for user {UserId}: {VendorName}",
            user.Id, request.VendorName);
        _logger.LogDebug("Allowance details - Amount: {Amount}, Frequency: {Frequency}",
            request.Amount, request.Frequency);

        var allowance = await _allowanceService.CreateAsync(user.Id, request);

        return CreatedAtAction(
            nameof(GetAllowance),
            new { id = allowance.Id },
            allowance);
    }

    /// <summary>
    /// Updates an existing recurring allowance.
    /// Only provided fields are updated.
    /// </summary>
    /// <param name="id">The allowance ID.</param>
    /// <param name="request">The update request.</param>
    /// <returns>The updated allowance.</returns>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(AllowanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AllowanceResponse>> UpdateAllowance(
        Guid id,
        [FromBody] UpdateAllowanceRequest request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Updating allowance {AllowanceId} for user {UserId}",
            id, user.Id);

        var allowance = await _allowanceService.UpdateAsync(user.Id, id, request);

        if (allowance == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Allowance with ID {id} was not found"
            });
        }

        return Ok(allowance);
    }

    /// <summary>
    /// Soft deletes a recurring allowance by setting it to inactive.
    /// The allowance data is preserved for historical reference.
    /// </summary>
    /// <param name="id">The allowance ID.</param>
    /// <returns>No content if successful.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAllowance(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation(
            "Deactivating allowance {AllowanceId} for user {UserId}",
            id, user.Id);

        var success = await _allowanceService.DeactivateAsync(user.Id, id);

        if (!success)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"Allowance with ID {id} was not found"
            });
        }

        return NoContent();
    }
}
