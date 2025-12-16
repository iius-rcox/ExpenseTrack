namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Tier usage statistics for cost monitoring.
/// </summary>
public class TierUsageStatsDto
{
    /// <summary>
    /// Date range for statistics.
    /// </summary>
    public TierUsagePeriod Period { get; set; } = new();

    /// <summary>
    /// Summary statistics across all operations.
    /// </summary>
    public TierUsageSummary Summary { get; set; } = new();

    /// <summary>
    /// Statistics broken down by operation type.
    /// </summary>
    public List<TierUsageByOperationType> ByOperationType { get; set; } = new();

    /// <summary>
    /// Vendors that are candidates for alias creation.
    /// </summary>
    public List<VendorAliasCandidateDto> VendorCandidates { get; set; } = new();
}

/// <summary>
/// Date range for tier usage statistics.
/// </summary>
public class TierUsagePeriod
{
    /// <summary>
    /// Start of date range.
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// End of date range.
    /// </summary>
    public DateTime End { get; set; }
}

/// <summary>
/// Summary of tier usage across all operations.
/// </summary>
public class TierUsageSummary
{
    /// <summary>
    /// Total operations in period.
    /// </summary>
    public int TotalOperations { get; set; }

    /// <summary>
    /// Count of Tier 1 operations.
    /// </summary>
    public int Tier1Count { get; set; }

    /// <summary>
    /// Percentage of Tier 1 operations.
    /// </summary>
    public decimal Tier1Percentage { get; set; }

    /// <summary>
    /// Count of Tier 2 operations.
    /// </summary>
    public int Tier2Count { get; set; }

    /// <summary>
    /// Percentage of Tier 2 operations.
    /// </summary>
    public decimal Tier2Percentage { get; set; }

    /// <summary>
    /// Count of Tier 3 operations.
    /// </summary>
    public int Tier3Count { get; set; }

    /// <summary>
    /// Percentage of Tier 3 operations.
    /// </summary>
    public decimal Tier3Percentage { get; set; }

    /// <summary>
    /// Estimated cost based on tier usage.
    /// </summary>
    public decimal EstimatedCost { get; set; }
}

/// <summary>
/// Tier usage statistics for a specific operation type.
/// </summary>
public class TierUsageByOperationType
{
    /// <summary>
    /// Operation type ('normalization', 'gl_suggestion', 'dept_suggestion').
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Total operations of this type.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Percentage of Tier 1 operations.
    /// </summary>
    public decimal Tier1Percentage { get; set; }

    /// <summary>
    /// Percentage of Tier 2 operations.
    /// </summary>
    public decimal Tier2Percentage { get; set; }

    /// <summary>
    /// Percentage of Tier 3 operations.
    /// </summary>
    public decimal Tier3Percentage { get; set; }
}

/// <summary>
/// Vendor that could benefit from creating a vendor alias.
/// </summary>
public class VendorAliasCandidateDto
{
    /// <summary>
    /// Vendor name.
    /// </summary>
    public string Vendor { get; set; } = string.Empty;

    /// <summary>
    /// Number of Tier 3 calls for this vendor.
    /// </summary>
    public int Tier3Count { get; set; }

    /// <summary>
    /// Recommendation message.
    /// </summary>
    public string Recommendation { get; set; } = string.Empty;
}
