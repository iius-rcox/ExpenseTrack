namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for the test cleanup endpoint.
/// Contains counts of deleted items and any warnings encountered.
/// </summary>
public class CleanupResponse
{
    /// <summary>
    /// Whether cleanup completed without errors.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Counts of deleted items by entity type.
    /// </summary>
    public CleanupDeletedCounts DeletedCounts { get; set; } = new();

    /// <summary>
    /// Time taken to complete cleanup in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Non-fatal issues encountered during cleanup.
    /// </summary>
    public List<string>? Warnings { get; set; }
}

/// <summary>
/// Counts of deleted items by entity type.
/// </summary>
public class CleanupDeletedCounts
{
    /// <summary>
    /// Number of receipts deleted.
    /// </summary>
    public int Receipts { get; set; }

    /// <summary>
    /// Number of transactions deleted.
    /// </summary>
    public int Transactions { get; set; }

    /// <summary>
    /// Number of receipt-transaction matches deleted.
    /// </summary>
    public int Matches { get; set; }

    /// <summary>
    /// Number of statement imports deleted.
    /// </summary>
    public int Imports { get; set; }

    /// <summary>
    /// Number of blob storage items deleted.
    /// </summary>
    public int BlobsDeleted { get; set; }
}
