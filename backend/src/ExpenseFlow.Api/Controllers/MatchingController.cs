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
    /// <remarks>
    /// Considers both individual transactions and transaction groups as match candidates.
    /// Returns ProposedCount broken down by TransactionMatchCount and GroupMatchCount.
    /// </remarks>
    /// <param name="request">Optional list of specific receipt IDs to match</param>
    /// <returns>Auto-match results including proposed matches to transactions and groups</returns>
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
            TransactionMatchCount = result.TransactionMatchCount,
            GroupMatchCount = result.GroupMatchCount,
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

        // Debug: Log TransactionGroup presence for each match
        foreach (var match in items)
        {
            _logger.LogInformation(
                "Match {MatchId}: TransactionGroupId={GroupId}, TransactionGroup={HasGroup}, Transaction={HasTxn}",
                match.Id,
                match.TransactionGroupId,
                match.TransactionGroup != null ? $"Name={match.TransactionGroup.Name}" : "null",
                match.Transaction != null ? $"Desc={match.Transaction.Description}" : "null");
        }

        var response = new ProposalListResponseDto
        {
            Items = items.Select(MapToProposalDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        // Debug: Log mapped DTOs to verify transactionGroup is present
        foreach (var dto in response.Items)
        {
            _logger.LogInformation(
                "DTO {MatchId}: CandidateType={CandidateType}, TransactionGroup={TransactionGroup}",
                dto.MatchId,
                dto.CandidateType,
                dto.TransactionGroup != null
                    ? $"{{Id={dto.TransactionGroup.Id}, Name={dto.TransactionGroup.Name}, Amount={dto.TransactionGroup.CombinedAmount}}}"
                    : "null");
        }

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
    /// Unmatch a previously confirmed match, returning both receipt and transaction to unmatched state.
    /// </summary>
    /// <remarks>
    /// This reverses a confirmed match. The match record is preserved with status Unmatched for audit trail.
    /// Both the receipt and transaction/group will have their match status reset to Unmatched.
    /// </remarks>
    /// <param name="matchId">Match record ID</param>
    /// <returns>Updated match details</returns>
    [HttpPost("{matchId:guid}/unmatch")]
    [ProducesResponseType(typeof(MatchDetailResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MatchDetailResponseDto>> UnmatchMatch(Guid matchId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var match = await _matchingService.UnmatchAsync(matchId, user.Id);
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
    /// Manually match a receipt to a transaction or transaction group.
    /// </summary>
    /// <remarks>
    /// Provide either TransactionId OR TransactionGroupId (not both).
    /// When matching to a group, the group's MatchStatus is updated to Matched.
    /// </remarks>
    /// <param name="request">Receipt and transaction/group IDs to match</param>
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
            ReceiptTransactionMatch match;

            // Route to appropriate method based on whether TransactionId or TransactionGroupId is provided
            if (request.TransactionId.HasValue && request.TransactionId.Value != Guid.Empty)
            {
                match = await _matchingService.CreateManualMatchAsync(
                    user.Id,
                    request.ReceiptId,
                    request.TransactionId.Value,
                    request.VendorDisplayName,
                    request.DefaultGLCode,
                    request.DefaultDepartment);
            }
            else if (request.TransactionGroupId.HasValue && request.TransactionGroupId.Value != Guid.Empty)
            {
                match = await _matchingService.CreateManualGroupMatchAsync(
                    user.Id,
                    request.ReceiptId,
                    request.TransactionGroupId.Value,
                    request.VendorDisplayName);
            }
            else
            {
                return BadRequest(new ProblemDetailsResponse
                {
                    Title = "Invalid Request",
                    Detail = "Either TransactionId or TransactionGroupId must be provided."
                });
            }

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
    /// Batch approve matches above a confidence threshold.
    /// </summary>
    /// <param name="request">Batch approve criteria (IDs or minimum confidence)</param>
    /// <returns>Result with approved and skipped counts</returns>
    [HttpPost("batch-approve")]
    [ProducesResponseType(typeof(BatchApproveResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<BatchApproveResponseDto>> BatchApprove([FromBody] BatchApproveRequestDto? request = null)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        _logger.LogInformation("Batch approve requested by user {UserId} with minConfidence={MinConfidence}, ids={IdCount}",
            user.Id, request?.MinConfidence, request?.Ids?.Count ?? 0);

        var result = await _matchingService.BatchApproveAsync(
            user.Id,
            request?.MinConfidence,
            request?.Ids);

        return Ok(new BatchApproveResponseDto
        {
            Approved = result.ApprovedCount,
            Skipped = result.SkippedCount
        });
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

    /// <summary>
    /// Gets ranked match candidates for a specific receipt.
    /// Returns both ungrouped transactions and transaction groups as potential matches.
    /// </summary>
    /// <param name="receiptId">Receipt ID to find candidates for</param>
    /// <param name="limit">Maximum number of candidates to return (default 10, max 50)</param>
    /// <returns>List of ranked candidates ordered by confidence score descending</returns>
    [HttpGet("candidates/{receiptId:guid}")]
    [ProducesResponseType(typeof(List<MatchCandidateDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<MatchCandidateDto>>> GetCandidates(
        Guid receiptId,
        [FromQuery] int limit = 10)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        try
        {
            var candidates = await _matchingService.GetCandidatesAsync(user.Id, receiptId, limit);

            var dtos = candidates.Select(c => new MatchCandidateDto
            {
                Id = c.Id,
                CandidateType = c.Type == Core.Interfaces.MatchCandidateType.Group ? "group" : "transaction",
                Amount = c.Amount,
                Date = c.Date,
                DisplayName = c.DisplayName,
                TransactionCount = c.TransactionCount,
                ConfidenceScore = c.ConfidenceScore,
                AmountScore = c.AmountScore,
                DateScore = c.DateScore,
                VendorScore = c.VendorScore,
                MatchReason = c.MatchReason
            }).ToList();

            return Ok(dtos);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Not Found",
                Detail = ex.Message
            });
        }
    }

    private static MatchProposalDto MapToProposalDto(ReceiptTransactionMatch match)
    {
        return new MatchProposalDto
        {
            MatchId = match.Id,
            ReceiptId = match.ReceiptId,
            TransactionId = match.TransactionId,
            TransactionGroupId = match.TransactionGroupId,
            CandidateType = match.TransactionGroupId.HasValue ? "group" : "transaction",
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
            TransactionGroup = match.TransactionGroup != null ? new MatchTransactionGroupSummaryDto
            {
                Id = match.TransactionGroup.Id,
                Name = match.TransactionGroup.Name,
                CombinedAmount = match.TransactionGroup.CombinedAmount,
                DisplayDate = match.TransactionGroup.DisplayDate,
                TransactionCount = match.TransactionGroup.TransactionCount
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
            TransactionGroupId = match.TransactionGroupId,
            CandidateType = match.TransactionGroupId.HasValue ? "group" : "transaction",
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
            TransactionGroup = match.TransactionGroup != null ? new MatchTransactionGroupSummaryDto
            {
                Id = match.TransactionGroup.Id,
                Name = match.TransactionGroup.Name,
                CombinedAmount = match.TransactionGroup.CombinedAmount,
                DisplayDate = match.TransactionGroup.DisplayDate,
                TransactionCount = match.TransactionGroup.TransactionCount
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
