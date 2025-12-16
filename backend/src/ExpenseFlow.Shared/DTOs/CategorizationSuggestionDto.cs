namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Represents a single categorization suggestion with confidence and source info.
/// </summary>
public class CategorizationSuggestionDto
{
    /// <summary>
    /// Suggested code (GL code or department code).
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for the code.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Confidence score (0.00-1.00).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Tier that produced this suggestion (1, 2, or 3).
    /// </summary>
    public int Tier { get; set; }

    /// <summary>
    /// Source of the suggestion ('vendor_alias', 'embedding_similarity', 'ai_inference').
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable explanation of how this suggestion was derived.
    /// </summary>
    public string Explanation { get; set; } = string.Empty;
}

/// <summary>
/// GL code suggestions response.
/// </summary>
public class GLSuggestionsDto
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// List of GL code suggestions ordered by confidence.
    /// </summary>
    public List<CategorizationSuggestionDto> Suggestions { get; set; } = new();

    /// <summary>
    /// Top suggestion (highest confidence).
    /// </summary>
    public CategorizationSuggestionDto? TopSuggestion { get; set; }

    /// <summary>
    /// Message if no suggestions available.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Service status ('healthy', 'degraded').
    /// </summary>
    public string? ServiceStatus { get; set; }
}

/// <summary>
/// Department suggestions response.
/// </summary>
public class DepartmentSuggestionsDto
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>
    /// List of department suggestions ordered by confidence.
    /// </summary>
    public List<CategorizationSuggestionDto> Suggestions { get; set; } = new();

    /// <summary>
    /// Top suggestion (highest confidence).
    /// </summary>
    public CategorizationSuggestionDto? TopSuggestion { get; set; }
}
