namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Result of AI column mapping inference.
/// </summary>
public class ColumnMappingInferenceResult
{
    /// <summary>
    /// Inferred column mapping from header names to field types.
    /// </summary>
    public Dictionary<string, string> ColumnMapping { get; set; } = new();

    /// <summary>
    /// Detected date format pattern.
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Detected amount sign convention.
    /// </summary>
    public string AmountSign { get; set; } = "negative_charges";

    /// <summary>
    /// Confidence score (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// Service interface for AI-powered column mapping inference.
/// Uses Tier 3 (GPT-4o-mini) per constitution.
/// </summary>
public interface IColumnMappingInferenceService
{
    /// <summary>
    /// Infers column mappings from statement headers and sample data.
    /// </summary>
    /// <param name="headers">Column headers from the statement.</param>
    /// <param name="sampleRows">First 3 data rows for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Inferred column mapping with confidence score.</returns>
    /// <exception cref="InvalidOperationException">Thrown when AI service is unavailable.</exception>
    Task<ColumnMappingInferenceResult> InferMappingAsync(
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> sampleRows,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the AI service is available.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}
