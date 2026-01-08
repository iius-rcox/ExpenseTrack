using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for transaction grouping operations.
/// Allows users to group multiple transactions into a single unit for receipt matching.
/// </summary>
[Authorize]
[Route("api/transaction-groups")] // Explicit kebab-case route for frontend compatibility
public class TransactionGroupsController : ApiControllerBase
{
    private readonly ITransactionGroupService _groupService;
    private readonly IUserService _userService;
    private readonly ILogger<TransactionGroupsController> _logger;

    public TransactionGroupsController(
        ITransactionGroupService groupService,
        IUserService userService,
        ILogger<TransactionGroupsController> logger)
    {
        _groupService = groupService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new transaction group from selected transactions.
    /// </summary>
    /// <param name="request">Transaction IDs to group, optional name and date override.</param>
    /// <returns>Created group with full transaction details.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TransactionGroupDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionGroupDetailDto>> CreateGroup(
        [FromBody] CreateGroupRequest request)
    {
        if (request.TransactionIds == null || request.TransactionIds.Count < 2)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "At least 2 transactions are required to create a group"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var group = await _groupService.CreateGroupAsync(user.Id, request);

            _logger.LogInformation(
                "Created transaction group {GroupId} with {Count} transactions for user {UserId}",
                group.Id, group.TransactionCount, user.Id);

            return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Cannot create group",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets a transaction group by ID.
    /// </summary>
    /// <param name="id">Group ID.</param>
    /// <returns>Group details with member transactions.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TransactionGroupDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionGroupDetailDto>> GetGroup(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var group = await _groupService.GetGroupAsync(user.Id, id);

        if (group == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Group not found",
                Detail = $"No transaction group found with ID {id}"
            });
        }

        return Ok(group);
    }

    /// <summary>
    /// Gets all transaction groups for the current user.
    /// </summary>
    /// <returns>List of transaction groups.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(TransactionGroupListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionGroupListResponse>> GetGroups()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var groups = await _groupService.GetGroupsAsync(user.Id);
        return Ok(groups);
    }

    /// <summary>
    /// Updates a group's name or display date.
    /// </summary>
    /// <param name="id">Group ID.</param>
    /// <param name="request">Update request with optional name and date.</param>
    /// <returns>Updated group details.</returns>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(TransactionGroupDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionGroupDetailDto>> UpdateGroup(
        Guid id,
        [FromBody] UpdateGroupRequest request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var group = await _groupService.UpdateGroupAsync(user.Id, id, request);

        if (group == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Group not found",
                Detail = $"No transaction group found with ID {id}"
            });
        }

        _logger.LogInformation(
            "Updated transaction group {GroupId} for user {UserId}",
            id, user.Id);

        return Ok(group);
    }

    /// <summary>
    /// Dissolves a group (ungroups all transactions).
    /// Clears any receipt match and removes the group.
    /// </summary>
    /// <param name="id">Group ID.</param>
    /// <returns>204 No Content on success.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGroup(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var deleted = await _groupService.DeleteGroupAsync(user.Id, id);

        if (!deleted)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Group not found",
                Detail = $"No transaction group found with ID {id}"
            });
        }

        _logger.LogInformation(
            "Deleted transaction group {GroupId} for user {UserId}",
            id, user.Id);

        return NoContent();
    }

    /// <summary>
    /// Adds transactions to an existing group.
    /// </summary>
    /// <param name="id">Group ID.</param>
    /// <param name="request">Transaction IDs to add.</param>
    /// <returns>Updated group details.</returns>
    [HttpPost("{id:guid}/transactions")]
    [ProducesResponseType(typeof(TransactionGroupDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionGroupDetailDto>> AddTransactions(
        Guid id,
        [FromBody] AddToGroupRequest request)
    {
        if (request.TransactionIds == null || request.TransactionIds.Count == 0)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid request",
                Detail = "At least 1 transaction ID is required"
            });
        }

        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var group = await _groupService.AddTransactionsToGroupAsync(user.Id, id, request);

            if (group == null)
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Group not found",
                    Detail = $"No transaction group found with ID {id}"
                });
            }

            _logger.LogInformation(
                "Added {Count} transactions to group {GroupId} for user {UserId}",
                request.TransactionIds.Count, id, user.Id);

            return Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Cannot add transactions",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Removes a single transaction from a group.
    /// Group must maintain at least 2 transactions. Use DELETE /api/transaction-groups/{id} to fully ungroup.
    /// </summary>
    /// <param name="id">Group ID.</param>
    /// <param name="transactionId">Transaction ID to remove.</param>
    /// <returns>Updated group details.</returns>
    [HttpDelete("{id:guid}/transactions/{transactionId:guid}")]
    [ProducesResponseType(typeof(TransactionGroupDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionGroupDetailDto>> RemoveTransaction(
        Guid id,
        Guid transactionId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var group = await _groupService.RemoveTransactionFromGroupAsync(user.Id, id, transactionId);

            if (group == null)
            {
                return NotFound(new ProblemDetailsResponse
                {
                    Title = "Group not found",
                    Detail = $"No transaction group found with ID {id}"
                });
            }

            _logger.LogInformation(
                "Removed transaction {TransactionId} from group {GroupId} for user {UserId}",
                transactionId, id, user.Id);

            return Ok(group);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Cannot remove transaction",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets a mixed list of ungrouped transactions and transaction groups.
    /// This is the primary endpoint for the Transactions page, replacing the standard transaction list.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 50, max 200).</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="matchStatus">Optional filter by match status (matched, pending, unmatched). Supports multiple values.</param>
    /// <param name="search">Optional text search on description.</param>
    /// <param name="sortBy">Sort field: "date" (default), "amount".</param>
    /// <param name="sortOrder">Sort order: "desc" (default), "asc".</param>
    /// <returns>Combined list of ungrouped transactions and groups.</returns>
    [HttpGet("mixed")]
    [ProducesResponseType(typeof(TransactionMixedListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TransactionMixedListResponse>> GetMixedList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] List<string>? matchStatus = null,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "date",
        [FromQuery] string sortOrder = "desc")
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 200) pageSize = 200;

        var user = await _userService.GetOrCreateUserAsync(User);

        var result = await _groupService.GetMixedListAsync(
            user.Id,
            page,
            pageSize,
            startDate,
            endDate,
            matchStatus,
            search,
            sortBy,
            sortOrder);

        return Ok(result);
    }
}
