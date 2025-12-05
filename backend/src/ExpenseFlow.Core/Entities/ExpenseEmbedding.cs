using Pgvector;

namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Stores vector embeddings for expense descriptions with associated categorization.
/// </summary>
public class ExpenseEmbedding : BaseEntity
{
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
}
