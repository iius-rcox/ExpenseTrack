namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for health check endpoint.
/// </summary>
public class HealthResponse
{
    public string Status { get; set; } = "healthy";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? Version { get; set; }
    public Dictionary<string, string>? Checks { get; set; }
}
