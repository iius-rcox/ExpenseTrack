namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Tracks the status and progress of cache warming import operations.
/// </summary>
public class ImportJob : BaseEntity
{
    public Guid UserId { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public string BlobUrl { get; set; } = string.Empty;
    public ImportJobStatus Status { get; set; } = ImportJobStatus.Pending;
    public int TotalRecords { get; set; }
    public int ProcessedRecords { get; set; }
    public int CachedDescriptions { get; set; }
    public int CreatedAliases { get; set; }
    public int GeneratedEmbeddings { get; set; }
    public int SkippedRecords { get; set; }
    public string? ErrorLog { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation property
    public User User { get; set; } = null!;
}

/// <summary>
/// Status of a cache warming import job.
/// </summary>
public enum ImportJobStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
