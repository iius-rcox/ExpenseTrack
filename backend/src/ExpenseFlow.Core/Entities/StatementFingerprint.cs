namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Stores user-specific column mappings for recurring statement imports.
/// </summary>
public class StatementFingerprint : BaseEntity
{
    /// <summary>
    /// Owner of this fingerprint.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Statement source name (e.g., "Chase Business Card").
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of header row.
    /// </summary>
    public string HeaderHash { get; set; } = string.Empty;

    /// <summary>
    /// Column name to field type mapping (JSON).
    /// </summary>
    public string ColumnMapping { get; set; } = "{}";

    /// <summary>
    /// Date format pattern (e.g., "MM/DD/YYYY").
    /// </summary>
    public string? DateFormat { get; set; }

    /// <summary>
    /// 'negative_charges' or 'positive_charges'.
    /// </summary>
    public string AmountSign { get; set; } = "negative_charges";

    // Navigation properties
    public User User { get; set; } = null!;
}
