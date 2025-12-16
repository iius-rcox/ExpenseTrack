using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Result of travel period detection from a receipt.
/// </summary>
public class TravelDetectionResultDto
{
    /// <summary>
    /// Whether a travel period was detected.
    /// </summary>
    public bool Detected { get; set; }

    /// <summary>
    /// The travel period that was created or updated.
    /// </summary>
    public TravelPeriodDetailDto? TravelPeriod { get; set; }

    /// <summary>
    /// What action was taken (Created, Extended, None).
    /// </summary>
    public TravelDetectionAction Action { get; set; }

    /// <summary>
    /// Vendor category that triggered detection.
    /// </summary>
    public VendorCategory? DetectedCategory { get; set; }

    /// <summary>
    /// Extracted destination from receipt.
    /// </summary>
    public string? ExtractedDestination { get; set; }

    /// <summary>
    /// Confidence score for the detection (0.0-1.0).
    /// </summary>
    public decimal Confidence { get; set; }

    /// <summary>
    /// Whether AI review is recommended.
    /// </summary>
    public bool RequiresAiReview { get; set; }

    /// <summary>
    /// Message describing the detection result.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Action taken during travel detection.
/// </summary>
public enum TravelDetectionAction
{
    /// <summary>No travel period action taken.</summary>
    None = 0,

    /// <summary>New travel period created.</summary>
    Created = 1,

    /// <summary>Existing travel period extended.</summary>
    Extended = 2,

    /// <summary>Receipt linked to existing travel period.</summary>
    Linked = 3
}
