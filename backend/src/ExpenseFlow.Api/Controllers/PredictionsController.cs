using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for expense prediction and pattern management operations.
/// </summary>
[Authorize]
public class PredictionsController : ApiControllerBase
{
    private readonly IExpensePredictionService _predictionService;
    private readonly IUserService _userService;
    private readonly ILogger<PredictionsController> _logger;

    public PredictionsController(
        IExpensePredictionService predictionService,
        IUserService userService,
        ILogger<PredictionsController> logger)
    {
        _predictionService = predictionService;
        _userService = userService;
        _logger = logger;
    }

    #region Dashboard

    /// <summary>
    /// Gets the prediction dashboard summary.
    /// </summary>
    /// <returns>Dashboard with pending counts and top predictions.</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(PredictionDashboardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PredictionDashboardDto>> GetDashboard()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var dashboard = await _predictionService.GetDashboardAsync(user.Id);
        return Ok(dashboard);
    }

    /// <summary>
    /// Gets prediction accuracy statistics.
    /// </summary>
    /// <returns>Accuracy stats including confirm/reject rates.</returns>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(PredictionAccuracyStatsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PredictionAccuracyStatsDto>> GetStats()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var stats = await _predictionService.GetAccuracyStatsAsync(user.Id);
        return Ok(stats);
    }

    /// <summary>
    /// Checks if predictions are available (user has learned patterns).
    /// </summary>
    /// <returns>Availability status with pattern count and message.</returns>
    [HttpGet("availability")]
    [ProducesResponseType(typeof(PredictionAvailabilityDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PredictionAvailabilityDto>> CheckAvailability()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var availability = await _predictionService.CheckAvailabilityAsync(user.Id);
        return Ok(availability);
    }

    #endregion

    #region Predictions CRUD

    /// <summary>
    /// Gets paginated list of predictions with optional filters.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="minConfidence">Optional minimum confidence level filter.</param>
    /// <returns>Paginated list of predictions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PredictionListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PredictionListResponseDto>> GetPredictions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PredictionStatus? status = null,
        [FromQuery] PredictionConfidence? minConfidence = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);

        var response = await _predictionService.GetPredictionsAsync(
            user.Id, page, pageSize, status, minConfidence);

        return Ok(response);
    }

    /// <summary>
    /// Gets prediction details by ID.
    /// </summary>
    /// <param name="id">Prediction ID.</param>
    /// <returns>Prediction details or 404 if not found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PredictionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PredictionDetailDto>> GetPrediction(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var prediction = await _predictionService.GetPredictionAsync(user.Id, id);
        if (prediction == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Prediction not found",
                Detail = $"No prediction found with ID {id}"
            });
        }

        return Ok(prediction);
    }

    /// <summary>
    /// Gets prediction for a specific transaction if available.
    /// </summary>
    /// <param name="transactionId">Transaction ID.</param>
    /// <returns>Prediction summary or 204 if no prediction.</returns>
    [HttpGet("transaction/{transactionId:guid}")]
    [ProducesResponseType(typeof(PredictionSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<PredictionSummaryDto>> GetPredictionForTransaction(Guid transactionId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var prediction = await _predictionService.GetPredictionForTransactionAsync(user.Id, transactionId);
        if (prediction == null)
        {
            return NoContent();
        }

        return Ok(prediction);
    }

    #endregion

    #region Prediction Actions

    /// <summary>
    /// Confirms a prediction as correct.
    /// </summary>
    /// <param name="request">Confirmation request with optional overrides.</param>
    /// <returns>Action result.</returns>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(PredictionActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PredictionActionResponseDto>> ConfirmPrediction(
        [FromBody] ConfirmPredictionRequestDto request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var result = await _predictionService.ConfirmPredictionAsync(user.Id, request);

        if (!result.Success)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Prediction not found",
                Detail = result.Message
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Rejects a prediction as incorrect.
    /// </summary>
    /// <param name="request">Rejection request.</param>
    /// <returns>Action result.</returns>
    [HttpPost("reject")]
    [ProducesResponseType(typeof(PredictionActionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PredictionActionResponseDto>> RejectPrediction(
        [FromBody] RejectPredictionRequestDto request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var result = await _predictionService.RejectPredictionAsync(user.Id, request);

        if (!result.Success)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Prediction not found",
                Detail = result.Message
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Performs bulk confirmation or rejection of predictions.
    /// </summary>
    /// <param name="request">Bulk action request.</param>
    /// <returns>Bulk action result.</returns>
    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkPredictionActionResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkPredictionActionResponseDto>> BulkAction(
        [FromBody] BulkPredictionActionRequestDto request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var result = await _predictionService.BulkActionAsync(user.Id, request);
        return Ok(result);
    }

    #endregion

    #region Patterns

    /// <summary>
    /// Gets paginated list of learned expense patterns.
    /// </summary>
    /// <param name="page">Page number (1-based, default 1).</param>
    /// <param name="pageSize">Page size (default 20, max 100).</param>
    /// <param name="includeSuppressed">Include suppressed patterns (default false).</param>
    /// <param name="suppressedOnly">Show only suppressed patterns (default false).</param>
    /// <param name="category">Filter by category name.</param>
    /// <param name="search">Search patterns by vendor name.</param>
    /// <param name="sortBy">Sort field: displayName, averageAmount, accuracyRate, occurrenceCount (default: accuracyRate).</param>
    /// <param name="sortOrder">Sort direction: asc or desc (default: desc).</param>
    /// <returns>Paginated list of patterns.</returns>
    [HttpGet("patterns")]
    [ProducesResponseType(typeof(PatternListResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatternListResponseDto>> GetPatterns(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeSuppressed = false,
        [FromQuery] bool suppressedOnly = false,
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] string sortBy = "accuracyRate",
        [FromQuery] string sortOrder = "desc")
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var user = await _userService.GetOrCreateUserAsync(User);

        var response = await _predictionService.GetPatternsAsync(
            user.Id, page, pageSize, includeSuppressed, suppressedOnly, category, search, sortBy, sortOrder);

        return Ok(response);
    }

    /// <summary>
    /// Gets pattern details by ID.
    /// </summary>
    /// <param name="id">Pattern ID.</param>
    /// <returns>Pattern details or 404 if not found.</returns>
    [HttpGet("patterns/{id:guid}")]
    [ProducesResponseType(typeof(PatternDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatternDetailDto>> GetPattern(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);

        var pattern = await _predictionService.GetPatternAsync(user.Id, id);
        if (pattern == null)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Pattern not found",
                Detail = $"No pattern found with ID {id}"
            });
        }

        return Ok(pattern);
    }

    /// <summary>
    /// Updates pattern suppression status.
    /// </summary>
    /// <param name="id">Pattern ID.</param>
    /// <param name="request">Suppression update request.</param>
    /// <returns>No content on success, 404 if not found.</returns>
    [HttpPatch("patterns/{id:guid}/suppression")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatternSuppression(
        Guid id,
        [FromBody] UpdatePatternSuppressionRequestDto request)
    {
        request.PatternId = id; // Ensure route ID matches request

        var user = await _userService.GetOrCreateUserAsync(User);
        var success = await _predictionService.UpdatePatternSuppressionAsync(user.Id, request);

        if (!success)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Pattern not found",
                Detail = $"No pattern found with ID {id}"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Updates whether a pattern requires receipt match for predictions.
    /// When enabled, predictions are only generated for transactions with confirmed receipt matches.
    /// </summary>
    /// <param name="id">Pattern ID.</param>
    /// <param name="request">Receipt match requirement update request.</param>
    /// <returns>No content on success, 404 if not found.</returns>
    [HttpPatch("patterns/{id:guid}/receipt-match")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePatternReceiptMatch(
        Guid id,
        [FromBody] UpdatePatternReceiptMatchRequestDto request)
    {
        request.PatternId = id; // Ensure route ID matches request

        var user = await _userService.GetOrCreateUserAsync(User);
        var success = await _predictionService.UpdatePatternReceiptMatchAsync(user.Id, request);

        if (!success)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Pattern not found",
                Detail = $"No pattern found with ID {id}"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a pattern and all associated predictions.
    /// </summary>
    /// <param name="id">Pattern ID.</param>
    /// <returns>No content on success, 404 if not found.</returns>
    [HttpDelete("patterns/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePattern(Guid id)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var success = await _predictionService.DeletePatternAsync(user.Id, id);

        if (!success)
        {
            return NotFound(new ProblemDetailsResponse
            {
                Title = "Pattern not found",
                Detail = $"No pattern found with ID {id}"
            });
        }

        return NoContent();
    }

    /// <summary>
    /// Performs bulk actions on patterns (suppress, enable, delete).
    /// </summary>
    /// <param name="request">Bulk action request with pattern IDs and action type.</param>
    /// <returns>Bulk action result with success/failure counts.</returns>
    [HttpPost("patterns/bulk")]
    [ProducesResponseType(typeof(BulkPatternActionResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BulkPatternActionResponseDto>> BulkPatternAction(
        [FromBody] BulkPatternActionRequestDto request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var result = await _predictionService.BulkPatternActionAsync(user.Id, request);

        _logger.LogInformation(
            "Bulk pattern action '{Action}' completed for user {UserId}: {SuccessCount} succeeded, {FailedCount} failed",
            request.Action, user.Id, result.SuccessCount, result.FailedCount);

        return Ok(result);
    }

    #endregion

    #region Pattern Learning

    /// <summary>
    /// Imports expense patterns from external data (CSV, historical records).
    /// </summary>
    /// <param name="request">Import request with expense entries.</param>
    /// <returns>Import results with created/updated counts.</returns>
    [HttpPost("patterns/import")]
    [ProducesResponseType(typeof(ImportPatternsResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ImportPatternsResponseDto>> ImportPatterns(
        [FromBody] ImportPatternsRequestDto request)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var result = await _predictionService.ImportPatternsAsync(user.Id, request);

        _logger.LogInformation(
            "Imported patterns for user {UserId}: {Created} created, {Updated} updated",
            user.Id, result.CreatedCount, result.UpdatedCount);

        return Ok(result);
    }

    /// <summary>
    /// Manually triggers pattern learning from a specific report.
    /// </summary>
    /// <param name="reportId">Report ID to learn from.</param>
    /// <returns>Number of patterns created or updated.</returns>
    [HttpPost("learn/{reportId:guid}")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> LearnFromReport(Guid reportId)
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var patternsCount = await _predictionService.LearnFromReportAsync(user.Id, reportId);

        _logger.LogInformation(
            "Learned {Count} patterns from report {ReportId} for user {UserId}",
            patternsCount, reportId, user.Id);

        return Ok(patternsCount);
    }

    /// <summary>
    /// Rebuilds all patterns from historical reports (Draft, Generated, and Submitted).
    /// WARNING: This clears all existing patterns before rebuilding.
    /// </summary>
    /// <returns>Number of patterns rebuilt.</returns>
    [HttpPost("rebuild")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<int>> RebuildPatterns()
    {
        try
        {
            var user = await _userService.GetOrCreateUserAsync(User);
            var patternsCount = await _predictionService.RebuildPatternsAsync(user.Id);

            _logger.LogInformation(
                "Rebuilt {Count} patterns for user {UserId}",
                patternsCount, user.Id);

            return Ok(patternsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild patterns");
            return StatusCode(500, new ProblemDetailsResponse
            {
                Title = "Failed to rebuild patterns",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Learns patterns from all historical transaction classifications.
    /// Scans existing Confirmed/Rejected predictions and creates patterns from them.
    /// Use this once to backfill patterns from historical Business/Personal markings.
    /// </summary>
    /// <returns>Summary of patterns created and updated.</returns>
    [HttpPost("learn-from-history")]
    [ProducesResponseType(typeof(LearnFromHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetailsResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LearnFromHistoryResponseDto>> LearnFromHistoricalClassifications()
    {
        try
        {
            var user = await _userService.GetOrCreateUserAsync(User);
            var (created, updated, processed) = await _predictionService.LearnFromHistoricalClassificationsAsync(user.Id);

            _logger.LogInformation(
                "Learned from historical classifications for user {UserId}: {Created} created, {Updated} updated, {Processed} processed",
                user.Id, created, updated, processed);

            return Ok(new LearnFromHistoryResponseDto
            {
                PatternsCreated = created,
                PatternsUpdated = updated,
                ClassificationsProcessed = processed,
                Message = $"Successfully learned from {processed} historical classifications: {created} patterns created, {updated} patterns updated"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to learn from historical classifications");
            return StatusCode(500, new ProblemDetailsResponse
            {
                Title = "Failed to learn from historical classifications",
                Detail = ex.Message
            });
        }
    }

    /// <summary>
    /// Generates predictions for all unprocessed transactions.
    /// </summary>
    /// <returns>Number of predictions generated.</returns>
    [HttpPost("generate")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<ActionResult<int>> GeneratePredictions()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var predictionsCount = await _predictionService.GenerateAllPendingPredictionsAsync(user.Id);

        _logger.LogInformation(
            "Generated {Count} predictions for user {UserId}",
            predictionsCount, user.Id);

        return Ok(predictionsCount);
    }

    /// <summary>
    /// Backfills transaction types based on learned vendor patterns.
    /// Auto-classifies existing transactions as Business or Personal based on
    /// feedback history (ActiveClassification from ExpensePattern).
    /// </summary>
    /// <returns>Count of transactions classified as business and personal.</returns>
    [HttpPost("backfill")]
    [ProducesResponseType(typeof(BackfillResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BackfillResultDto>> BackfillTransactionTypes()
    {
        var user = await _userService.GetOrCreateUserAsync(User);
        var (businessCount, personalCount) = await _predictionService.BackfillTransactionTypesAsync(user.Id);

        _logger.LogInformation(
            "Backfill complete for user {UserId}: {BusinessCount} business, {PersonalCount} personal",
            user.Id, businessCount, personalCount);

        return Ok(new BackfillResultDto
        {
            BusinessCount = businessCount,
            PersonalCount = personalCount,
            TotalClassified = businessCount + personalCount
        });
    }

    #endregion
}

/// <summary>
/// Result of transaction type backfill operation.
/// </summary>
public class BackfillResultDto
{
    /// <summary>Number of transactions classified as Business.</summary>
    public int BusinessCount { get; set; }

    /// <summary>Number of transactions classified as Personal.</summary>
    public int PersonalCount { get; set; }

    /// <summary>Total transactions classified.</summary>
    public int TotalClassified { get; set; }
}
