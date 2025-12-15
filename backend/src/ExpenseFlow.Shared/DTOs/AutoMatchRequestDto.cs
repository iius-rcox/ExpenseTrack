namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Request DTO for auto-match operation.
/// </summary>
public class AutoMatchRequestDto
{
    /// <summary>
    /// Optional list of specific receipt IDs to match.
    /// If empty or null, matches all unmatched receipts.
    /// </summary>
    public List<Guid>? ReceiptIds { get; set; }
}
