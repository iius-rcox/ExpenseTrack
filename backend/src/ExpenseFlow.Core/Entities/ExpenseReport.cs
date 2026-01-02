using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents a draft expense report for a specific period.
/// </summary>
public class ExpenseReport : BaseEntity
{
    /// <summary>FK to Users - owner of the report</summary>
    public Guid UserId { get; set; }

    /// <summary>Reporting period in YYYY-MM format (e.g., "2025-01")</summary>
    public string Period { get; set; } = string.Empty;

    /// <summary>Report status (Draft → Generated → Submitted)</summary>
    public ReportStatus Status { get; set; } = ReportStatus.Draft;

    /// <summary>Timestamp when report was finalized (status changed to Generated)</summary>
    public DateTimeOffset? GeneratedAt { get; set; }

    /// <summary>Timestamp when report was submitted (status changed to Submitted)</summary>
    public DateTimeOffset? SubmittedAt { get; set; }

    /// <summary>Sum of all expense line amounts</summary>
    public decimal TotalAmount { get; set; }

    /// <summary>Number of expense lines</summary>
    public int LineCount { get; set; }

    /// <summary>Lines without receipts</summary>
    public int MissingReceiptCount { get; set; }

    /// <summary>Suggestions from cache (Tier 1)</summary>
    public int Tier1HitCount { get; set; }

    /// <summary>Suggestions from embeddings (Tier 2)</summary>
    public int Tier2HitCount { get; set; }

    /// <summary>Suggestions from AI (Tier 3)</summary>
    public int Tier3HitCount { get; set; }

    /// <summary>Soft delete flag</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Last modification timestamp</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>PostgreSQL xmin for optimistic locking</summary>
    public uint RowVersion { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<ExpenseLine> Lines { get; set; } = new List<ExpenseLine>();
}
