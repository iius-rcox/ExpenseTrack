namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Frequency options for recurring expense allowances.
/// </summary>
public enum AllowanceFrequency : short
{
    /// <summary>Allowance applies once per week</summary>
    Weekly = 0,

    /// <summary>Allowance applies once per month</summary>
    Monthly = 1,

    /// <summary>Allowance applies once per quarter (Jan/Apr/Jul/Oct)</summary>
    Quarterly = 2
}
