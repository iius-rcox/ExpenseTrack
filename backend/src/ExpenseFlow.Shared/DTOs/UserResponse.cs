namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for user profile data.
/// </summary>
public class UserResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
}
