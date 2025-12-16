using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Types of subscription alerts.
/// </summary>
public enum SubscriptionAlertType
{
    /// <summary>
    /// Expected subscription charge is missing for the month.
    /// </summary>
    MissingCharge,

    /// <summary>
    /// Subscription amount varied significantly from average.
    /// </summary>
    AmountVariance,

    /// <summary>
    /// New subscription pattern detected.
    /// </summary>
    NewSubscription,

    /// <summary>
    /// Subscription may have been cancelled (missing multiple months).
    /// </summary>
    PossibleCancellation
}

/// <summary>
/// Alert priority levels.
/// </summary>
public enum AlertPriority
{
    Low,
    Medium,
    High
}

/// <summary>
/// Individual subscription alert.
/// </summary>
public class SubscriptionAlertDto
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public SubscriptionAlertType AlertType { get; set; }
    public AlertPriority Priority { get; set; }
    public string Message { get; set; } = string.Empty;
    public decimal? ExpectedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

/// <summary>
/// Response containing list of subscription alerts.
/// </summary>
public class SubscriptionAlertListResponseDto
{
    public List<SubscriptionAlertDto> Alerts { get; set; } = new();
    public int TotalCount { get; set; }
    public int UnacknowledgedCount { get; set; }
}

/// <summary>
/// Request to acknowledge an alert.
/// </summary>
public class AcknowledgeAlertRequestDto
{
    public List<Guid> AlertIds { get; set; } = new();
}

/// <summary>
/// Summary of subscription monitoring status.
/// </summary>
public class SubscriptionMonitoringSummaryDto
{
    public int TotalActiveSubscriptions { get; set; }
    public int MissingThisMonth { get; set; }
    public int FlaggedForReview { get; set; }
    public int NewDetections { get; set; }
    public decimal TotalMonthlyExpected { get; set; }
    public decimal TotalMonthlyActual { get; set; }
    public List<SubscriptionAlertDto> RecentAlerts { get; set; } = new();
}
