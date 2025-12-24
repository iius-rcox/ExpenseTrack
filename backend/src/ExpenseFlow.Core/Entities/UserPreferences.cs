namespace ExpenseFlow.Core.Entities;

/// <summary>
/// User-configurable application preferences.
/// One record per user, created lazily on first preference update.
/// </summary>
public class UserPreferences : BaseEntity
{
    /// <summary>
    /// Foreign key to the owning user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Preferred color theme: "light", "dark", or "system".
    /// </summary>
    public string Theme { get; set; } = "system";

    /// <summary>
    /// Default department for new expense reports.
    /// </summary>
    public Guid? DefaultDepartmentId { get; set; }

    /// <summary>
    /// Default project for new expense reports.
    /// </summary>
    public Guid? DefaultProjectId { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User User { get; set; } = null!;
}
