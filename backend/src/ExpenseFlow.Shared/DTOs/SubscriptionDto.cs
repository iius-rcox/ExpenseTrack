using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary DTO for subscription list views.
/// </summary>
public class SubscriptionSummaryDto
{
    public Guid Id { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal AverageAmount { get; set; }
    public DateOnly LastSeenDate { get; set; }
    public DateOnly? ExpectedNextDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public string? Category { get; set; }
    public int OccurrenceCount { get; set; }
}

/// <summary>
/// Detail DTO for individual subscription views.
/// </summary>
public class SubscriptionDetailDto
{
    public Guid Id { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public decimal AverageAmount { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
    public List<string> OccurrenceMonths { get; set; } = new();
    public DateOnly LastSeenDate { get; set; }
    public DateOnly? ExpectedNextDate { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DetectionSource DetectionSource { get; set; }
    public Guid? VendorAliasId { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Request DTO for creating a manual subscription.
/// </summary>
public class CreateSubscriptionRequestDto
{
    public string VendorName { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public int ExpectedDay { get; set; } = 1;
    public string? Category { get; set; }
}

/// <summary>
/// Request DTO for updating a subscription.
/// </summary>
public class UpdateSubscriptionRequestDto
{
    public string VendorName { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public int ExpectedDay { get; set; }
    public SubscriptionStatus Status { get; set; }
    public string? Category { get; set; }
}

/// <summary>
/// Paginated list response for subscriptions.
/// </summary>
public class SubscriptionListResponseDto
{
    public List<SubscriptionSummaryDto> Subscriptions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int ActiveCount { get; set; }
    public int MissingCount { get; set; }
    public int FlaggedCount { get; set; }
}
