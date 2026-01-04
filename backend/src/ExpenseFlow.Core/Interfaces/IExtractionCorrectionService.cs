using ExpenseFlow.Shared.DTOs;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service for managing extraction corrections (training feedback).
/// Records user corrections to AI-extracted receipt fields for model improvement.
/// </summary>
public interface IExtractionCorrectionService
{
    /// <summary>
    /// Gets a paginated list of extraction corrections with optional filtering.
    /// </summary>
    /// <param name="queryParams">Query parameters for filtering and pagination.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Paginated result of extraction corrections.</returns>
    Task<ExtractionCorrectionPagedResult> GetCorrectionsAsync(
        ExtractionCorrectionQueryParams queryParams,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a single extraction correction by ID.
    /// </summary>
    /// <param name="id">Correction ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Correction details or null if not found.</returns>
    Task<ExtractionCorrectionDetailDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);

    /// <summary>
    /// Records multiple corrections for a receipt update.
    /// Filters out no-op corrections (where original equals corrected value).
    /// </summary>
    /// <param name="receiptId">Receipt being corrected.</param>
    /// <param name="userId">User making the corrections.</param>
    /// <param name="corrections">Correction metadata from the update request.</param>
    /// <param name="currentValues">Dictionary of current field values after update (fieldName -> value).</param>
    /// <param name="ct">Cancellation token.</param>
    Task RecordCorrectionsAsync(
        Guid receiptId,
        Guid userId,
        IEnumerable<CorrectionMetadataDto> corrections,
        Dictionary<string, string?> currentValues,
        CancellationToken ct = default);
}
