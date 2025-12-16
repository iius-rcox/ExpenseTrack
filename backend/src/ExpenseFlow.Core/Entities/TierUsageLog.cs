namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Records tier usage for cost monitoring and optimization analytics.
/// </summary>
public class TierUsageLog : BaseEntity
{
    /// <summary>
    /// User who triggered the operation.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Related transaction (if applicable).
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// Type of operation: 'normalization', 'gl_suggestion', 'dept_suggestion'.
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// Tier used: 1 (cache/alias), 2 (embedding), or 3 (AI inference).
    /// </summary>
    public int TierUsed { get; set; }

    /// <summary>
    /// Result confidence score (0.00-1.00).
    /// </summary>
    public decimal? Confidence { get; set; }

    /// <summary>
    /// Processing time in milliseconds.
    /// </summary>
    public int? ResponseTimeMs { get; set; }

    /// <summary>
    /// Whether result came from cache.
    /// </summary>
    public bool CacheHit { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
