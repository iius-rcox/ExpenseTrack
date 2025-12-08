namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Summary view of a statement import for history displays.
/// </summary>
public class ImportSummaryDto
{
    /// <summary>
    /// Unique import identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Original uploaded filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the fingerprint source or "AI Detected".
    /// </summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>
    /// Tier used: 1 = fingerprint cache, 3 = AI inference.
    /// </summary>
    public int TierUsed { get; set; }

    /// <summary>
    /// Number of transactions imported.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// When the import was performed.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Paginated list of statement imports.
/// </summary>
public class StatementImportListResponse
{
    /// <summary>
    /// List of imports.
    /// </summary>
    public List<ImportSummaryDto> Imports { get; set; } = new();

    /// <summary>
    /// Total count of imports.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }
}
