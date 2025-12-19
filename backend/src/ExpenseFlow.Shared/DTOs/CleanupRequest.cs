namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for the test cleanup endpoint.
/// Allows optional filtering of which entity types to clean and by creation timestamp.
/// </summary>
public class CleanupRequest
{
    /// <summary>
    /// Specific entity types to clean. If omitted or empty, all entity types are cleaned.
    /// Valid values: "receipts", "transactions", "matches", "imports"
    /// </summary>
    public List<string>? EntityTypes { get; set; }

    /// <summary>
    /// Only delete items created after this timestamp.
    /// If omitted, all items for the user are deleted.
    /// </summary>
    public DateTime? CreatedAfter { get; set; }
}
