using System.Text.Json.Serialization;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// RFC 7807 Problem Details response for API errors.
/// </summary>
public class ProblemDetailsResponse
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "about:blank";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("instance")]
    public string? Instance { get; set; }

    [JsonPropertyName("traceId")]
    public string? TraceId { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? Extensions { get; set; }
}
