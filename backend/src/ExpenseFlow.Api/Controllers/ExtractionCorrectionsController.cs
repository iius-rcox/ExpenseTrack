using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseFlow.Api.Controllers;

/// <summary>
/// Controller for managing extraction correction records (training feedback).
/// Provides admin access to view correction history for model improvement analysis.
/// Feature 024: Extraction Editor Training
/// </summary>
[Authorize]
public class ExtractionCorrectionsController : ApiControllerBase
{
    private readonly IExtractionCorrectionService _correctionService;
    private readonly ILogger<ExtractionCorrectionsController> _logger;

    public ExtractionCorrectionsController(
        IExtractionCorrectionService correctionService,
        ILogger<ExtractionCorrectionsController> logger)
    {
        _correctionService = correctionService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of extraction corrections with optional filtering.
    /// </summary>
    /// <param name="queryParams">Query parameters for filtering, sorting, and pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated list of extraction corrections.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ExtractionCorrectionPagedResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ExtractionCorrectionPagedResult>> GetCorrections(
        [FromQuery] ExtractionCorrectionQueryParams queryParams,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Fetching extraction corrections with filters: FieldName={FieldName}, Page={Page}, PageSize={PageSize}",
            queryParams.FieldName,
            queryParams.Page,
            queryParams.PageSize);

        var result = await _correctionService.GetCorrectionsAsync(queryParams, ct);

        return Ok(result);
    }

    /// <summary>
    /// Gets a specific extraction correction by ID with full details.
    /// </summary>
    /// <param name="id">The correction ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The correction details if found.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ExtractionCorrectionDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExtractionCorrectionDetailDto>> GetCorrectionById(
        Guid id,
        CancellationToken ct = default)
    {
        var correction = await _correctionService.GetByIdAsync(id, ct);

        if (correction == null)
        {
            return NotFound();
        }

        return Ok(correction);
    }
}
