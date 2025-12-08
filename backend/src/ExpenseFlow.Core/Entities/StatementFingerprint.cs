namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Stores column mappings for recurring statement imports.
/// When UserId is null, this is a system-wide fingerprint (e.g., Chase, Amex).
/// </summary>
public class StatementFingerprint : BaseEntity
{
    /// <summary>
    /// Owner of this fingerprint. NULL for system-wide fingerprints.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Statement source name (e.g., "Chase Business Card").
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of normalized, sorted header row.
    /// </summary>
    public string HeaderHash { get; set; } = string.Empty;

    /// <summary>
    /// Column name to field type mapping (JSON).
    /// </summary>
    public string ColumnMapping { get; set; } = "{}";

    /// <summary>
    /// Date format pattern (e.g., "MM/dd/yyyy").
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// Amount sign convention: 'negative_charges' or 'positive_charges'.
    /// </summary>
    public string AmountSign { get; set; } = "negative_charges";

    /// <summary>
    /// Number of times this fingerprint was successfully used.
    /// </summary>
    public int HitCount { get; set; }

    /// <summary>
    /// Last successful use timestamp.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public User? User { get; set; }

    /// <summary>
    /// Returns true if this is a system-wide fingerprint (UserId is null).
    /// </summary>
    public bool IsSystem => UserId == null;
}
