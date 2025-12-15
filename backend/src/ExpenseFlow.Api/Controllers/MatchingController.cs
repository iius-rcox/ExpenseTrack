using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for receipt-to-transaction matching operations.
/// </summary>
[Authorize]
public class MatchingController : ApiControllerBase
{
    private readonly IMatchingService _matchingService;
    private readonly IUserService _userService;
    private readonly ILogger<MatchingController> _logger;

    public MatchingController(
        IMatchingService matchingService,
        IUserService userService,
        ILogger<MatchingController> logger)
    {
        _matchingService = matchingService;
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Run auto-match for all unmatched receipts.
    /// </summary>
    /// <param name="request">Optional list of specific receipt IDs to match</param>
    /// <returns>Auto-match results including proposed matches</returns>
    [HttpPost("auto")]
    [ProducesResponseType(typeof(AutoMatchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AutoMatchResponseDto>> RunAutoMatch([FromBody] AutoMatchRequestDto? request = null)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation("Auto-match requested by user {UserId}", user.Id);

        var result = await _matchingService.RunAutoMatchAsync(user.Id, request?.ReceiptIds);

        var response = new AutoMatchResponseDto
        {
            ProposedCount = result.ProposedCount,
            ProcessedCount = result.ProcessedCount,
            AmbiguousCount = result.AmbiguousCount,
            DurationMs = result.DurationMs,
            Proposals = result.Proposals.Select(MapToProposalDto).ToList()
        };

        return Ok(response);
    }

    /// <summary>
    /// Get all proposed matches for review.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    /// <returns>Paginated list of proposed matches</returns>
    [HttpGet("proposals")]
    [ProducesResponseType(typeof(ProposalListResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProposalListResponseDto>> GetProposals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);
        var (items, totalCount) = await _matchingService.GetProposalsAsync(user.Id, page, pageSize);

        var response = new ProposalListResponseDto
        {
            Items = items.Select(MapToProposalDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Confirm a proposed match.
    /// </summary>
    /// <param name="matchId">Match record ID</param>
    /// <param name="request">Optional vendor alias configuration</param>
    /// <returns>Confirmed match details</returns>
    [HttpPost("{matchId:guid}/confirm")]
    [ProducesResponseType(typeof(MatchDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchDetailResponseDto>> ConfirmMatch(
        Guid matchId,
        [FromBody] ConfirmMatchRequestDto? request = null)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var match = await _matchingService.ConfirmMatchAsync(
                matchId,
                user.Id,
                request?.VendorDisplayName,
                request?.DefaultGLCode,
                request?.DefaultDepartment);

            return Ok(MapToDetailDto(match));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid Operation",
                Detail = ex.Message
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetailsResponse
            {
                Title = "Conflict",
                Detail = "This match was modified by another user. Please refresh and try again."
            });
        }
    }

    /// <summary>
    /// Reject a proposed match.
    /// </summary>
    /// <param name="matchId">Match record ID</param>
    /// <returns>Rejected match details</returns>
    [HttpPost("{matchId:guid}/reject")]
    [ProducesResponseType(typeof(MatchDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchDetailResponseDto>> RejectMatch(Guid matchId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var match = await _matchingService.RejectMatchAsync(matchId, user.Id);
            return Ok(MapToDetailDto(match));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid Operation",
                Detail = ex.Message
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ProblemDetailsResponse
            {
                Title = "Conflict",
                Detail = "This match was modified by another user. Please refresh and try again."
            });
        }
    }

    /// <summary>
    /// Manually match a receipt to a transaction.
    /// </summary>
    /// <param name="request">Receipt and transaction IDs to match</param>
    /// <returns>Created match details</returns>
    [HttpPost("manual")]
    [ProducesResponseType(typeof(MatchDetailResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchDetailResponseDto>> CreateManualMatch([FromBody] ManualMatchRequestDto request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var match = await _matchingService.CreateManualMatchAsync(
                user.Id,
                request.ReceiptId,
                request.TransactionId,
                request.VendorDisplayName,
                request.DefaultGLCode,
                request.DefaultDepartment);

            return CreatedAtAction(nameof(GetMatch), new { matchId = match.Id }, MapToDetailDto(match));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already matched"))
        {
            return Conflict(new ProblemDetailsResponse
            {
                Title = "Conflict",
                Detail = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new ProblemDetailsResponse
            {
                Title = "Invalid Operation",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Get match details.
    /// </summary>
    /// <param name="matchId">Match record ID</param>
    /// <returns>Match details</returns>
    [HttpGet("{matchId:guid}")]
    [ProducesResponseType(typeof(MatchDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchDetailResponseDto>> GetMatch(Guid matchId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var match = await _matchingService.GetMatchAsync(matchId, user.Id);

        if (match == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = $"Match with ID {matchId} was not found"
            });
        }

        return Ok(MapToDetailDto(match));
    }

    /// <summary>
    /// Get matching statistics.
    /// </summary>
    /// <returns>Matching statistics</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(MatchingStatsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchingStatsResponseDto>> GetStats()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var stats = await _matchingService.GetStatsAsync(user.Id);

        var response = new MatchingStatsResponseDto
        {
            MatchedCount = stats.MatchedCount,
            ProposedCount = stats.ProposedCount,
            UnmatchedReceiptsCount = stats.UnmatchedReceiptsCount,
            UnmatchedTransactionsCount = stats.UnmatchedTransactionsCount,
            AutoMatchRate = stats.AutoMatchRate,
            AverageConfidence = stats.AverageConfidence
        };

        return Ok(response);
    }

    /// <summary>
    /// Get unmatched receipts with extracted data.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    /// <returns>Paginated list of unmatched receipts</returns>
    [HttpGet("receipts/unmatched")]
    [ProducesResponseType(typeof(UnmatchedReceiptsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnmatchedReceiptsResponseDto>> GetUnmatchedReceipts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);
        var (items, totalCount) = await _matchingService.GetUnmatchedReceiptsAsync(user.Id, page, pageSize);

        var response = new UnmatchedReceiptsResponseDto
        {
            Items = items.Select(r => new MatchReceiptSummaryDto
            {
                Id = r.Id,
                VendorExtracted = r.VendorExtracted,
                DateExtracted = r.DateExtracted,
                AmountExtracted = r.AmountExtracted,
                Currency = r.Currency,
                ThumbnailUrl = r.ThumbnailUrl,
                OriginalFilename = r.OriginalFilename
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    /// <summary>
    /// Get unmatched transactions.
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page (max 100)</param>
    /// <returns>Paginated list of unmatched transactions</returns>
    [HttpGet("transactions/unmatched")]
    [ProducesResponseType(typeof(UnmatchedTransactionsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UnmatchedTransactionsResponseDto>> GetUnmatchedTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);
        var (items, totalCount) = await _matchingService.GetUnmatchedTransactionsAsync(user.Id, page, pageSize);

        var response = new UnmatchedTransactionsResponseDto
        {
            Items = items.Select(t => new MatchTransactionSummaryDto
            {
                Id = t.Id,
                Description = t.Description,
                OriginalDescription = t.OriginalDescription,
                TransactionDate = t.TransactionDate,
                PostDate = t.PostDate,
                Amount = t.Amount
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }

    private static MatchProposalDto MapToProposalDto(ReceiptTransactionMatch match)
    {
        return new MatchProposalDto
        {
            MatchId = match.Id,
            ReceiptId = match.ReceiptId,
            TransactionId = match.TransactionId,
            ConfidenceScore = match.ConfidenceScore,
            AmountScore = match.AmountScore,
            DateScore = match.DateScore,
            VendorScore = match.VendorScore,
            MatchReason = match.MatchReason,
            Status = match.Status.ToString(),
            Receipt = match.Receipt != null ? new MatchReceiptSummaryDto
            {
                Id = match.Receipt.Id,
                VendorExtracted = match.Receipt.VendorExtracted,
                DateExtracted = match.Receipt.DateExtracted,
                AmountExtracted = match.Receipt.AmountExtracted,
                Currency = match.Receipt.Currency,
                ThumbnailUrl = match.Receipt.ThumbnailUrl,
                OriginalFilename = match.Receipt.OriginalFilename
            } : null,
            Transaction = match.Transaction != null ? new MatchTransactionSummaryDto
            {
                Id = match.Transaction.Id,
                Description = match.Transaction.Description,
                OriginalDescription = match.Transaction.OriginalDescription,
                TransactionDate = match.Transaction.TransactionDate,
                PostDate = match.Transaction.PostDate,
                Amount = match.Transaction.Amount
            } : null,
            CreatedAt = match.CreatedAt
        };
    }

    private static MatchDetailResponseDto MapToDetailDto(ReceiptTransactionMatch match)
    {
        return new MatchDetailResponseDto
        {
            MatchId = match.Id,
            ReceiptId = match.ReceiptId,
            TransactionId = match.TransactionId,
            ConfidenceScore = match.ConfidenceScore,
            AmountScore = match.AmountScore,
            DateScore = match.DateScore,
            VendorScore = match.VendorScore,
            MatchReason = match.MatchReason,
            Status = match.Status.ToString(),
            Receipt = match.Receipt != null ? new MatchReceiptSummaryDto
            {
                Id = match.Receipt.Id,
                VendorExtracted = match.Receipt.VendorExtracted,
                DateExtracted = match.Receipt.DateExtracted,
                AmountExtracted = match.Receipt.AmountExtracted,
                Currency = match.Receipt.Currency,
                ThumbnailUrl = match.Receipt.ThumbnailUrl,
                OriginalFilename = match.Receipt.OriginalFilename
            } : null,
            Transaction = match.Transaction != null ? new MatchTransactionSummaryDto
            {
                Id = match.Transaction.Id,
                Description = match.Transaction.Description,
                OriginalDescription = match.Transaction.OriginalDescription,
                TransactionDate = match.Transaction.TransactionDate,
                PostDate = match.Transaction.PostDate,
                Amount = match.Transaction.Amount
            } : null,
            CreatedAt = match.CreatedAt,
            ConfirmedAt = match.ConfirmedAt,
            IsManualMatch = match.IsManualMatch,
            VendorAlias = match.MatchedVendorAlias != null ? new VendorAliasSummaryDto
            {
                Id = match.MatchedVendorAlias.Id,
                CanonicalName = match.MatchedVendorAlias.CanonicalName,
                DisplayName = match.MatchedVendorAlias.DisplayName,
                AliasPattern = match.MatchedVendorAlias.AliasPattern,
                DefaultGLCode = match.MatchedVendorAlias.DefaultGLCode,
                DefaultDepartment = match.MatchedVendorAlias.DefaultDepartment,
                MatchCount = match.MatchedVendorAlias.MatchCount
            } : null
        };
    }
}
