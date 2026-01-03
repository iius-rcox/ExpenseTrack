namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Learned expense pattern extracted from user's approved expense reports.
/// Used to predict which future transactions are likely business expenses.
/// </summary>
public class ExpensePattern : BaseEntity
{
    /// <summary>FK to Users - pattern owner</summary>
    public Guid UserId { get; set; }

    /// <summary>Normalized vendor name (via VendorAlias lookup)</summary>
    public string NormalizedVendor { get; set; } = string.Empty;

    /// <summary>Human-readable vendor name for UI display</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Most common category for this vendor's expenses</summary>
    public string? Category { get; set; }

    /// <summary>Running weighted average of expense amounts</summary>
    public decimal AverageAmount { get; set; }

    /// <summary>Minimum amount seen for this vendor</summary>
    public decimal MinAmount { get; set; }

    /// <summary>Maximum amount seen for this vendor</summary>
    public decimal MaxAmount { get; set; }

    /// <summary>Number of times this vendor appeared in expense reports</summary>
    public int OccurrenceCount { get; set; }

    /// <summary>Timestamp of most recent occurrence</summary>
    public DateTime LastSeenAt { get; set; }

    /// <summary>Most commonly used GL code for this vendor</summary>
    public string? DefaultGLCode { get; set; }

    /// <summary>Most commonly used department for this vendor</summary>
    public string? DefaultDepartment { get; set; }

    /// <summary>Number of times user confirmed predictions for this pattern</summary>
    public int ConfirmCount { get; set; }

    /// <summary>Number of times user rejected predictions for this pattern</summary>
    public int RejectCount { get; set; }

    /// <summary>True if user explicitly excluded this vendor from predictions</summary>
    public bool IsSuppressed { get; set; }

    /// <summary>
    /// When true, predictions only generated for transactions with confirmed receipt matches.
    /// Useful for vendors like Amazon where most purchases are personal.
    /// </summary>
    public bool RequiresReceiptMatch { get; set; }

    /// <summary>Last update timestamp</summary>
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<TransactionPrediction> Predictions { get; set; } = new List<TransactionPrediction>();
}
