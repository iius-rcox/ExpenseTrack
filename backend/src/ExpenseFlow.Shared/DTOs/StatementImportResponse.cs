namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response from statement import endpoint.
/// </summary>
public class StatementImportResponse
{
    /// <summary>
    /// Unique identifier for this import batch.
    /// </summary>
    public Guid ImportId { get; set; }

    /// <summary>
    /// Tier used for this import: 1 = fingerprint cache, 3 = AI inference.
    /// </summary>
    public int TierUsed { get; set; }

    /// <summary>
    /// Number of transactions successfully imported.
    /// </summary>
    public int Imported { get; set; }

    /// <summary>
    /// Number of rows skipped due to missing required fields.
    /// </summary>
    public int Skipped { get; set; }

    /// <summary>
    /// Number of duplicate rows not imported.
    /// </summary>
    public int Duplicates { get; set; }

    /// <summary>
    /// Whether a fingerprint was created or updated.
    /// </summary>
    public bool FingerprintSaved { get; set; }

    /// <summary>
    /// First 10 imported transactions as preview.
    /// </summary>
    public List<TransactionSummaryDto> Transactions { get; set; } = new();
}
