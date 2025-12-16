namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary DTO for split pattern list views.
/// </summary>
public class SplitPatternSummaryDto
{
    /// <summary>
    /// The pattern ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User-defined name for the pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The vendor alias ID this pattern is associated with (if any).
    /// </summary>
    public Guid? VendorAliasId { get; set; }

    /// <summary>
    /// The vendor name for display.
    /// </summary>
    public string? VendorName { get; set; }

    /// <summary>
    /// Number of allocations in this pattern.
    /// </summary>
    public int AllocationCount { get; set; }

    /// <summary>
    /// Number of times this pattern has been used.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// When this pattern was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether this is the default pattern for the vendor.
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// Detail DTO for individual split pattern views.
/// </summary>
public class SplitPatternDetailDto
{
    /// <summary>
    /// The pattern ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User-defined name for the pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The user ID who owns this pattern.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The vendor alias ID this pattern is associated with (if any).
    /// </summary>
    public Guid? VendorAliasId { get; set; }

    /// <summary>
    /// The vendor name for display.
    /// </summary>
    public string? VendorName { get; set; }

    /// <summary>
    /// The allocations in this pattern.
    /// </summary>
    public List<SplitAllocationDto> Allocations { get; set; } = new();

    /// <summary>
    /// Number of times this pattern has been used.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// When this pattern was last used.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Whether this is the default pattern for the vendor.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// When this pattern was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this pattern was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating a split pattern.
/// </summary>
public class CreateSplitPatternRequestDto
{
    /// <summary>
    /// User-defined name for the pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The vendor alias ID to associate with (optional).
    /// </summary>
    public Guid? VendorAliasId { get; set; }

    /// <summary>
    /// The allocations for this pattern.
    /// Must sum to exactly 100%.
    /// </summary>
    public List<SplitAllocationDto> Allocations { get; set; } = new();

    /// <summary>
    /// Whether this should be the default pattern for the vendor.
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// Request DTO for updating a split pattern.
/// </summary>
public class UpdateSplitPatternRequestDto
{
    /// <summary>
    /// User-defined name for the pattern.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The allocations for this pattern.
    /// Must sum to exactly 100%.
    /// </summary>
    public List<SplitAllocationDto> Allocations { get; set; } = new();

    /// <summary>
    /// Whether this should be the default pattern for the vendor.
    /// </summary>
    public bool IsDefault { get; set; }
}

/// <summary>
/// Paginated list response for split patterns.
/// </summary>
public class SplitPatternListResponseDto
{
    /// <summary>
    /// The patterns.
    /// </summary>
    public List<SplitPatternSummaryDto> Patterns { get; set; } = new();

    /// <summary>
    /// Total count of patterns.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }
}
