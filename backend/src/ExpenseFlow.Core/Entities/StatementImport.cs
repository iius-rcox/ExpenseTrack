namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Audit record for each statement import operation.
/// </summary>
public class StatementImport : BaseEntity
{
    /// <summary>
    /// User who performed the import (FK to Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Fingerprint used for this import (FK to StatementFingerprints, nullable).
    /// Null if AI inference was used without saving a fingerprint.
    /// </summary>
    public Guid? FingerprintId { get; set; }

    /// <summary>
    /// Original uploaded filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Tier used: 1 = fingerprint cache, 3 = AI inference.
    /// </summary>
    public int TierUsed { get; set; }

    /// <summary>
    /// Number of transactions successfully imported.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Number of rows skipped due to missing required fields.
    /// </summary>
    public int SkippedCount { get; set; }

    /// <summary>
    /// Number of duplicate rows not imported.
    /// </summary>
    public int DuplicateCount { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public StatementFingerprint? Fingerprint { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
