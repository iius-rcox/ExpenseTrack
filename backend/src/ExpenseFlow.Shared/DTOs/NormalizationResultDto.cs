namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Result of description normalization.
/// </summary>
public class NormalizationResultDto
{
    /// <summary>
    /// Original raw bank description.
    /// </summary>
    public string RawDescription { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable normalized description.
    /// </summary>
    public string NormalizedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Extracted vendor name.
    /// </summary>
    public string ExtractedVendor { get; set; } = string.Empty;

    /// <summary>
    /// Tier used for normalization (1 or 3; Tier 2 not applicable for normalization).
    /// </summary>
    public int Tier { get; set; }

    /// <summary>
    /// Whether result came from cache.
    /// </summary>
    public bool CacheHit { get; set; }

    /// <summary>
    /// Confidence score (1.0 for cache hits, varies for AI).
    /// </summary>
    public decimal Confidence { get; set; }
}

/// <summary>
/// Request to normalize a description.
/// </summary>
public class NormalizationRequest
{
    /// <summary>
    /// Raw bank description to normalize.
    /// </summary>
    public string RawDescription { get; set; } = string.Empty;
}
