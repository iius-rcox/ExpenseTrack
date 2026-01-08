using System.ComponentModel.DataAnnotations;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Shared.DTOs;

// ============================================================================
// Request DTOs
// ============================================================================

/// <summary>
/// Request to create a new transaction group from selected transactions.
/// </summary>
public class CreateGroupRequest
{
    /// <summary>
    /// List of transaction IDs to include in the group.
    /// Minimum 2 transactions required.
    /// </summary>
    [MinLength(2, ErrorMessage = "At least 2 transactions are required to create a group")]
    public List<Guid> TransactionIds { get; set; } = new();

    /// <summary>
    /// Optional custom name for the group.
    /// If not provided, auto-generates from vendor name (e.g., "Twilio (3 charges)").
    /// Maximum 100 characters.
    /// </summary>
    [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters")]
    public string? Name { get; set; }

    /// <summary>
    /// Optional override for the display date.
    /// If not provided, uses the maximum transaction date.
    /// </summary>
    public DateOnly? DisplayDate { get; set; }
}

/// <summary>
/// Request to update an existing group's metadata.
/// </summary>
public class UpdateGroupRequest
{
    /// <summary>
    /// New name for the group (optional). Maximum 100 characters.
    /// </summary>
    [StringLength(100, ErrorMessage = "Group name cannot exceed 100 characters")]
    public string? Name { get; set; }

    /// <summary>
    /// New display date for the group (optional).
    /// When set, marks IsDateOverridden as true.
    /// </summary>
    public DateOnly? DisplayDate { get; set; }
}

/// <summary>
/// Request to add transactions to an existing group.
/// </summary>
public class AddToGroupRequest
{
    /// <summary>
    /// List of transaction IDs to add to the group.
    /// At least 1 transaction is required.
    /// </summary>
    [MinLength(1, ErrorMessage = "At least 1 transaction ID is required")]
    public List<Guid> TransactionIds { get; set; } = new();
}

// ============================================================================
// Response DTOs
// ============================================================================

/// <summary>
/// Summary view of a transaction group for list displays.
/// </summary>
public class TransactionGroupSummaryDto
{
    /// <summary>
    /// Unique group identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name (e.g., "Twilio (3 charges)").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Date used for display and matching (max date or override).
    /// </summary>
    public DateOnly DisplayDate { get; set; }

    /// <summary>
    /// Sum of all transaction amounts in the group.
    /// </summary>
    public decimal CombinedAmount { get; set; }

    /// <summary>
    /// Number of transactions in this group.
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Current match status: Unmatched, Proposed, or Matched.
    /// </summary>
    public MatchStatus MatchStatus { get; set; }

    /// <summary>
    /// ID of the matched receipt (if any).
    /// </summary>
    public Guid? MatchedReceiptId { get; set; }

    /// <summary>
    /// When the group was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Detailed view of a transaction group including its member transactions.
/// </summary>
public class TransactionGroupDetailDto : TransactionGroupSummaryDto
{
    /// <summary>
    /// Whether the display date was manually overridden.
    /// </summary>
    public bool IsDateOverridden { get; set; }

    /// <summary>
    /// List of transactions in this group.
    /// </summary>
    public List<GroupMemberTransactionDto> Transactions { get; set; } = new();

    /// <summary>
    /// Optional warning message from the last operation.
    /// For example, when removing a transaction causes amount mismatch with matched receipt.
    /// </summary>
    public string? Warning { get; set; }
}

/// <summary>
/// Minimal transaction info for display within an expanded group row.
/// </summary>
public class GroupMemberTransactionDto
{
    /// <summary>
    /// Transaction ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Date of the transaction.
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Transaction amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Transaction description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Paginated list of transaction groups.
/// </summary>
public class TransactionGroupListResponse
{
    /// <summary>
    /// List of transaction groups.
    /// </summary>
    public List<TransactionGroupSummaryDto> Groups { get; set; } = new();

    /// <summary>
    /// Total count of groups matching filters.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Combined list of ungrouped transactions and transaction groups for the UI.
/// Used by the Transactions page to display mixed items.
/// </summary>
public class TransactionMixedListResponse
{
    /// <summary>
    /// Ungrouped transactions (where GroupId is null).
    /// </summary>
    public List<TransactionSummaryDto> Transactions { get; set; } = new();

    /// <summary>
    /// Transaction groups (with expandable details).
    /// </summary>
    public List<TransactionGroupDetailDto> Groups { get; set; } = new();

    /// <summary>
    /// Total count of items (transactions + groups).
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; set; }
}
