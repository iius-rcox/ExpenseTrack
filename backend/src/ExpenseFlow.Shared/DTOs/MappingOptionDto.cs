namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Represents a mapping option returned from statement analysis.
/// </summary>
public class MappingOptionDto
{
    /// <summary>
    /// Source of this mapping option.
    /// Values: "system_fingerprint", "user_fingerprint", "ai_inference"
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Tier level: 1 = cached fingerprint, 3 = AI inference.
    /// </summary>
    public int Tier { get; set; }

    /// <summary>
    /// Fingerprint ID if source is a fingerprint (null for AI inference).
    /// </summary>
    public Guid? FingerprintId { get; set; }

    /// <summary>
    /// Friendly name for the mapping source (e.g., "Chase Business Card").
    /// </summary>
    public string? SourceName { get; set; }

    /// <summary>
    /// Column header to field type mapping.
    /// </summary>
    public Dictionary<string, string> ColumnMapping { get; set; } = new();

    /// <summary>
    /// Expected date format pattern (e.g., "MM/dd/yyyy").
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Amount sign convention: "negative_charges" or "positive_charges".
    /// </summary>
    public string AmountSign { get; set; } = "negative_charges";

    /// <summary>
    /// AI confidence score (0.0-1.0). Only present for ai_inference source.
    /// </summary>
    public double? Confidence { get; set; }
}

/// <summary>
/// Valid mapping sources.
/// </summary>
public static class MappingSources
{
    public const string SystemFingerprint = "system_fingerprint";
    public const string UserFingerprint = "user_fingerprint";
    public const string AiInference = "ai_inference";
}

/// <summary>
/// Valid amount sign conventions.
/// </summary>
public static class AmountSignConventions
{
    public const string NegativeCharges = "negative_charges";
    public const string PositiveCharges = "positive_charges";
}
