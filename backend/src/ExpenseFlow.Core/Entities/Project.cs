namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Project reference data synced from external SQL Server.
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// Project code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Project name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether project is currently valid.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last sync timestamp from source.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
