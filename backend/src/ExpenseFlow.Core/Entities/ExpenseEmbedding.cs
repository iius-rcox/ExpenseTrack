using Pgvector;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Stores vector embeddings for expense descriptions with associated categorization.
/// </summary>
public class ExpenseEmbedding : BaseEntity
{
    /// <summary>
    /// Owner user for row-level security.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Source transaction reference.
    /// </summary>
    public Guid? TransactionId { get; set; }

    /// <summary>
    /// Reference to source expense line (future).
    /// </summary>
    public Guid? ExpenseLineId { get; set; }

    /// <summary>
    /// Normalized vendor name.
    /// </summary>
    public string? VendorNormalized { get; set; }

    /// <summary>
    /// Description text that was embedded.
    /// </summary>
    public string DescriptionText { get; set; } = string.Empty;

    /// <summary>
    /// Associated GL code.
    /// </summary>
    public string? GLCode { get; set; }

    /// <summary>
    /// Associated department.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// text-embedding-3-small vector (1536 dimensions).
    /// </summary>
    public Vector Embedding { get; set; } = null!;

    /// <summary>
    /// Whether user verified this categorization.
    /// </summary>
    public bool Verified { get; set; }

    /// <summary>
    /// Auto-purge date. NULL for verified (never expires), CreatedAt + 6 months for unverified.
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
