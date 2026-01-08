namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents a job cost budget record synced from Viewpoint Vista JCCP table.
/// Stores aggregated budget amounts by job, phase, and cost type for expense comparison.
/// </summary>
public class VistaBudget : BaseEntity
{
    /// <summary>
    /// Job Cost Company from Vista (JCCP.JCCo).
    /// Typically 1 for primary company.
    /// </summary>
    public int JCCo { get; set; }

    /// <summary>
    /// Vista job/contract code (JCCP.Job).
    /// Max 50 chars to match Vista schema.
    /// </summary>
    public string Job { get; set; } = string.Empty;

    /// <summary>
    /// Phase group code for cost categorization (JCCP.PhaseGroup).
    /// Max 30 chars.
    /// </summary>
    public string PhaseCode { get; set; } = string.Empty;

    /// <summary>
    /// Cost type code (JCCP.CostType).
    /// Examples: "1" (Labor), "2" (Material), "3" (Equipment), "6" (Subcontract).
    /// Max 10 chars.
    /// </summary>
    public string CostType { get; set; } = string.Empty;

    /// <summary>
    /// Total budget amount for this job/phase/cost type combination.
    /// Aggregated SUM from Vista JCCP records.
    /// </summary>
    public decimal BudgetAmount { get; set; }

    /// <summary>
    /// Fiscal year for budget tracking.
    /// Extracted from Vista PostedDate using DATEPART.
    /// </summary>
    public int FiscalYear { get; set; }

    /// <summary>
    /// Description from Vista job master (JCCM.Description).
    /// Used for display purposes.
    /// </summary>
    public string? JobDescription { get; set; }

    /// <summary>
    /// Whether the budget record is currently valid.
    /// Set to false when job becomes inactive in Vista.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last sync timestamp from Vista.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the record was last modified in Vista.
    /// Used for incremental sync detection.
    /// </summary>
    public DateTime? ModifiedInVistaAt { get; set; }

    /// <summary>
    /// Soft delete timestamp.
    /// Null if active, set when marked inactive.
    /// </summary>
    public DateTime? DeletedAt { get; set; }
}
