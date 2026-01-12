using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents a user-created group of related transactions that act as
/// a single unit for receipt matching purposes.
/// Example: 3 Twilio charges throughout December grouped to match one invoice.
/// </summary>
public class TransactionGroup : BaseEntity
{
    /// <summary>
    /// Owner of this group (FK to Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User-editable display name (auto-generated on creation, e.g., "Twilio (3 charges)").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display date for the group. Defaults to max(transaction dates), user can override.
    /// </summary>
    public DateOnly DisplayDate { get; set; }

    /// <summary>
    /// Whether the user has manually overridden the display date.
    /// </summary>
    public bool IsDateOverridden { get; set; }

    /// <summary>
    /// Combined amount of all grouped transactions (computed sum, stored for query efficiency).
    /// </summary>
    public decimal CombinedAmount { get; set; }

    /// <summary>
    /// Number of transactions in this group (denormalized for UI display).
    /// </summary>
    public int TransactionCount { get; set; }

    /// <summary>
    /// Linked receipt ID (FK to Receipts). Set when a match is confirmed.
    /// Uses same pattern as Transaction.MatchedReceiptId.
    /// </summary>
    public Guid? MatchedReceiptId { get; set; }

    /// <summary>
    /// Match status: Unmatched=0, Proposed=1, Matched=2.
    /// </summary>
    public MatchStatus MatchStatus { get; set; } = MatchStatus.Unmatched;

    /// <summary>
    /// Merchant/vendor name for this group. Used for receipt matching vendor score calculation.
    /// Typically derived from the first transaction's description or user-specified.
    /// </summary>
    public string? MerchantName { get; set; }

    /// <summary>
    /// Whether this group represents reimbursable expenses (for missing receipts tracking).
    /// Null = unknown/not set, true = reimbursable, false = not reimbursable.
    /// </summary>
    public bool? IsReimbursable { get; set; }

    /// <summary>
    /// Expense category for reporting and analysis (e.g., "travel", "meals", "software").
    /// Matches the category system used in ExpensePattern/TransactionPrediction.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional user notes/description for additional context beyond the Name field.
    /// Useful for explaining why transactions were grouped or providing audit trail details.
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Receipt? MatchedReceipt { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
