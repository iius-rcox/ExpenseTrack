namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for project data.
/// </summary>
public class ProjectResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
