using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for expense splitting and split pattern management.
/// </summary>
[Authorize]
public class ExpenseSplittingController : ApiControllerBase
{
    private readonly IExpenseSplittingService _splittingService;
    private readonly IUserService _userService;
    private readonly ILogger<ExpenseSplittingController> _logger;

    public ExpenseSplittingController(
        IExpenseSplittingService splittingService,
        IUserService userService,
        ILogger<ExpenseSplittingController> logger)
    {
        _splittingService = splittingService;
        _userService = userService;
        _logger = logger;
    }

    #region Expense Split Operations

    /// <summary>
    /// Gets the split status and suggestion for an expense.
    /// </summary>
    /// <param name="expenseId">Expense ID (transaction or receipt).</param>
    /// <returns>Split status with current allocations and suggestions.</returns>
    [HttpGet("expenses/{expenseId:guid}/split")]
    [ProducesResponseType(typeof(ExpenseSplitStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseSplitStatusDto>> GetSplitStatus(Guid expenseId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var status = await _splittingService.GetSplitStatusAsync(user.Id, expenseId);
        if (status == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Expense not found",
                Detail = $"No expense found with ID {expenseId}"
            });
        }

        return Ok(status);
    }

    /// <summary>
    /// Applies a split to an expense.
    /// </summary>
    /// <param name="expenseId">Expense ID (transaction or receipt).</param>
    /// <param name="request">Split allocation request.</param>
    /// <returns>Result of the split operation.</returns>
    [HttpPost("expenses/{expenseId:guid}/split")]
    [ProducesResponseType(typeof(ApplySplitResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApplySplitResultDto>> ApplySplit(
        Guid expenseId,
        [FromBody] ApplySplitRequestDto request)
    {
        if (request.Allocations == null || request.Allocations.Count < 2)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "At least two allocations are required for a split"
            });
        }

        if (!_splittingService.ValidateAllocations(request.Allocations))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid allocations",
                Detail = "Allocation percentages must sum to exactly 100%"
            });
        }

        if (request.SaveAsPattern && string.IsNullOrWhiteSpace(request.PatternName))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Pattern name is required when saving as pattern"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        var result = await _splittingService.ApplySplitAsync(user.Id, expenseId, request);
        if (!result.Success)
        {
            if (result.Message.Contains("not found"))
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Expense not found",
                    Detail = result.Message
                });
            }

            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Split failed",
                Detail = result.Message
            });
        }

        _logger.LogInformation(
            "Applied split to expense {ExpenseId} for user {UserId}",
            expenseId, user.Id);

        return Ok(result);
    }

    /// <summary>
    /// Removes a split from an expense.
    /// </summary>
    /// <param name="expenseId">Expense ID (transaction or receipt).</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpDelete("expenses/{expenseId:guid}/split")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSplit(Guid expenseId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var removed = await _splittingService.RemoveSplitAsync(user.Id, expenseId);
        if (!removed)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Expense not found",
                Detail = $"No expense found with ID {expenseId}"
            });
        }

        _logger.LogInformation(
            "Removed split from expense {ExpenseId} for user {UserId}",
            expenseId, user.Id);

        return NoContent();
    }

    #endregion

    #region Split Pattern Management

    /// <summary>
    /// Gets paginated list of split patterns.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="vendorAliasId">Optional filter by vendor alias.</param>
    /// <returns>Paginated list of split patterns.</returns>
    [HttpGet("split-patterns")]
    [ProducesResponseType(typeof(SplitPatternListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<SplitPatternListResponseDto>> GetPatterns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? vendorAliasId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);

        var response = await _splittingService.GetPatternsAsync(
            user.Id, page, pageSize, vendorAliasId);

        return Ok(response);
    }

    /// <summary>
    /// Gets a split pattern by ID.
    /// </summary>
    /// <param name="id">Pattern ID.</param>
    /// <returns>Pattern details or 404 if not found.</returns>
    [HttpGet("split-patterns/{id:guid}")]
    [ProducesResponseType(typeof(SplitPatternDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SplitPatternDetailDto>> GetPattern(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var pattern = await _splittingService.GetPatternAsync(user.Id, id);
        if (pattern == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Pattern not found",
                Detail = $"No split pattern found with ID {id}"
            });
        }

        return Ok(pattern);
    }

    /// <summary>
    /// Creates a new split pattern.
    /// </summary>
    /// <param name="request">Pattern creation request.</param>
    /// <returns>Created pattern.</returns>
    [HttpPost("split-patterns")]
    [ProducesResponseType(typeof(SplitPatternDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SplitPatternDetailDto>> CreatePattern(
        [FromBody] CreateSplitPatternRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Pattern name is required"
            });
        }

        if (request.Allocations == null || request.Allocations.Count < 2)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "At least two allocations are required"
            });
        }

        if (!_splittingService.ValidateAllocations(request.Allocations))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid allocations",
                Detail = "Allocation percentages must sum to exactly 100%"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var pattern = await _splittingService.CreatePatternAsync(user.Id, request);

            _logger.LogInformation(
                "Created split pattern {PatternId} for user {UserId}",
                pattern.Id, user.Id);

            return CreatedAtAction(
                nameof(GetPattern),
                new { id = pattern.Id },
                pattern);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid pattern",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Updates an existing split pattern.
    /// </summary>
    /// <param name="id">Pattern ID.</param>
    /// <param name="request">Pattern update request.</param>
    /// <returns>Updated pattern or 404 if not found.</returns>
    [HttpPut("split-patterns/{id:guid}")]
    [ProducesResponseType(typeof(SplitPatternDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SplitPatternDetailDto>> UpdatePattern(
        Guid id,
        [FromBody] UpdateSplitPatternRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "Pattern name is required"
            });
        }

        if (request.Allocations == null || request.Allocations.Count < 2)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "At least two allocations are required"
            });
        }

        if (!_splittingService.ValidateAllocations(request.Allocations))
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid allocations",
                Detail = "Allocation percentages must sum to exactly 100%"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var pattern = await _splittingService.UpdatePatternAsync(user.Id, id, request);
            if (pattern == null)
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Pattern not found",
                    Detail = $"No split pattern found with ID {id}"
                });
            }

            _logger.LogInformation(
                "Updated split pattern {PatternId} for user {UserId}",
                id, user.Id);

            return Ok(pattern);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid pattern",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Deletes a split pattern.
    /// </summary>
    /// <param name="id">Pattern ID.</param>
    /// <returns>204 No Content on success, 404 if not found.</returns>
    [HttpDelete("split-patterns/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePattern(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var deleted = await _splittingService.DeletePatternAsync(user.Id, id);
        if (!deleted)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Pattern not found",
                Detail = $"No split pattern found with ID {id}"
            });
        }

        _logger.LogInformation(
            "Deleted split pattern {PatternId} for user {UserId}",
            id, user.Id);

        return NoContent();
    }

    #endregion
}
