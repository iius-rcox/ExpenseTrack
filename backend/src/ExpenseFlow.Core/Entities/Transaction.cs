using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Represents an imported credit card transaction.
/// </summary>
public class Transaction : BaseEntity
{
    /// <summary>
    /// Owner of this transaction (FK to Users).
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Source import batch (FK to StatementImports).
    /// </summary>
    public Guid ImportId { get; set; }

    /// <summary>
    /// Date the transaction occurred.
    /// </summary>
    public DateOnly TransactionDate { get; set; }

    /// <summary>
    /// Date the transaction posted (optional).
    /// </summary>
    public DateOnly? PostDate { get; set; }

    /// <summary>
    /// Parsed/normalized description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Raw description from statement.
    /// </summary>
    public string OriginalDescription { get; set; } = string.Empty;

    /// <summary>
    /// Transaction amount. Positive = expense, negative = credit/refund.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// SHA-256 hash of (date + amount + description) for duplicate detection.
    /// </summary>
    public string DuplicateHash { get; set; } = string.Empty;

    /// <summary>
    /// Linked receipt ID (FK to Receipts, nullable). Sprint 5 scope.
    /// </summary>
    public Guid? MatchedReceiptId { get; set; }

    /// <summary>
    /// Match status: Unmatched=0, Proposed=1, Matched=2.
    /// </summary>
    public MatchStatus MatchStatus { get; set; } = MatchStatus.Unmatched;

    // Navigation properties
    public User User { get; set; } = null!;
    public StatementImport Import { get; set; } = null!;
    public Receipt? MatchedReceipt { get; set; }
}
