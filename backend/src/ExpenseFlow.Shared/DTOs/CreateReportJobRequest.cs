namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request to start a new report generation job.
/// </summary>
public record CreateReportJobRequest
{
    /// <summary>Billing period in YYYY-MM format.</summary>
    public string Period { get; init; } = string.Empty;
}
