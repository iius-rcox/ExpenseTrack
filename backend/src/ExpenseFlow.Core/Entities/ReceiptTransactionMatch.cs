using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Links a receipt to a transaction with match metadata and confidence scoring.
/// </summary>
public class ReceiptTransactionMatch : BaseEntity
{
    /// <summary>FK to Receipts</summary>
    public Guid ReceiptId { get; set; }

    /// <summary>FK to Transactions (nullable when matching a group)</summary>
    public Guid? TransactionId { get; set; }

    /// <summary>FK to TransactionGroups (nullable when matching a single transaction)</summary>
    public Guid? TransactionGroupId { get; set; }

    /// <summary>FK to Users (denormalized for queries)</summary>
    public Guid UserId { get; set; }

    /// <summary>Match status: Proposed=0, Confirmed=1, Rejected=2</summary>
    public MatchProposalStatus Status { get; set; } = MatchProposalStatus.Proposed;

    /// <summary>Overall confidence score (0.00-100.00)</summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>Amount match score component (0.00-40.00)</summary>
    public decimal AmountScore { get; set; }

    /// <summary>Date match score component (0.00-35.00)</summary>
    public decimal DateScore { get; set; }

    /// <summary>Vendor match score component (0.00-25.00)</summary>
    public decimal VendorScore { get; set; }

    /// <summary>Human-readable explanation of match</summary>
    public string? MatchReason { get; set; }

    /// <summary>FK to VendorAliases (if alias matched)</summary>
    public Guid? MatchedVendorAliasId { get; set; }

    /// <summary>True if user manually matched</summary>
    public bool IsManualMatch { get; set; }

    /// <summary>When user confirmed/rejected</summary>
    public DateTime? ConfirmedAt { get; set; }

    /// <summary>FK to Users who confirmed/rejected</summary>
    public Guid? ConfirmedByUserId { get; set; }

    /// <summary>PostgreSQL xmin for optimistic locking</summary>
    public uint RowVersion { get; set; }

    // Navigation properties
    public Receipt Receipt { get; set; } = null!;
    public Transaction? Transaction { get; set; }
    public TransactionGroup? TransactionGroup { get; set; }
    public User User { get; set; } = null!;
    public VendorAlias? MatchedVendorAlias { get; set; }
    public User? ConfirmedByUser { get; set; }
}

/// <summary>
/// Status of a match proposal (different from MatchStatus on Receipt/Transaction).
/// </summary>
public enum MatchProposalStatus : short
{
    /// <summary>Auto-match proposed, awaiting user review</summary>
    Proposed = 0,

    /// <summary>User confirmed the match</summary>
    Confirmed = 1,

    /// <summary>User rejected the match</summary>
    Rejected = 2
}
