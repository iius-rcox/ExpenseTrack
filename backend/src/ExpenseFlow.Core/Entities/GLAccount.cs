namespace ExpenseFlow.Core.Entities;

/// <summary>
/// GL Account reference data synced from external SQL Server.
/// </summary>
public class GLAccount : BaseEntity
{
    /// <summary>
    /// GL account code (e.g., "66300").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Account name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Account description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether account is currently valid.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last sync timestamp from source.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
