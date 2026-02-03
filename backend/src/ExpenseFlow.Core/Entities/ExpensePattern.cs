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

    // Classification threshold constants (lowered personal threshold for better detection)
    private const decimal BusinessConfirmThreshold = 0.50m;  // 50%+ confirm rate
    private const int BusinessMinCount = 1;                   // Minimum 1 sample
    private const decimal PersonalRejectThreshold = 0.60m;   // 60%+ reject rate (was 75%)
    private const int PersonalMinCount = 3;                   // Minimum 3 samples (was 4)

    /// <summary>
    /// Active classification calculated from feedback.
    /// true = business expense (50%+ confirm rate, count >= 1)
    /// false = personal expense (60%+ reject rate, count >= 3)
    /// null = undetermined (doesn't meet either threshold)
    /// </summary>
    public bool? ActiveClassification
    {
        get
        {
            var totalCount = ConfirmCount + RejectCount;

            // No feedback data
            if (totalCount == 0)
                return null;

            var confirmRate = (decimal)ConfirmCount / totalCount;
            var rejectRate = (decimal)RejectCount / totalCount;

            // Business: 50%+ confirm rate AND total count >= 1
            // Business classification takes precedence (check first)
            if (confirmRate >= BusinessConfirmThreshold && totalCount >= BusinessMinCount)
                return true;

            // Personal: 60%+ reject rate AND total count >= 3
            if (rejectRate >= PersonalRejectThreshold && totalCount >= PersonalMinCount)
                return false;

            // Undetermined
            return null;
        }
    }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<TransactionPrediction> Predictions { get; set; } = new List<TransactionPrediction>();
}
