namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for user preferences.
/// </summary>
public class UserPreferencesResponse
{
    /// <summary>
    /// User's preferred color theme: "light", "dark", or "system".
    /// </summary>
    public string Theme { get; set; } = "system";

    /// <summary>
    /// Default department ID for new expense reports.
    /// </summary>
    public Guid? DefaultDepartmentId { get; set; }

    /// <summary>
    /// Default project ID for new expense reports.
    /// </summary>
    public Guid? DefaultProjectId { get; set; }
}
