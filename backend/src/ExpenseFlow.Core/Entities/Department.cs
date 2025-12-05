namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Department reference data synced from external SQL Server.
/// </summary>
public class Department : BaseEntity
{
    /// <summary>
    /// Department code (e.g., "07").
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Department name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Department description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether department is currently valid.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Last sync timestamp from source.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
