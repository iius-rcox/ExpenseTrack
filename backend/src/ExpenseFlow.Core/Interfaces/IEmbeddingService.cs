using ExpenseFlow.Core.Entities;
using Pgvector;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for embedding generation and similarity search (Tier 2).
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Generates an embedding vector for the given text.
    /// </summary>
    /// <param name="text">Text to embed (max 500 characters).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>1536-dimension embedding vector.</returns>
    Task<Vector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds similar verified embeddings using cosine similarity.
    /// </summary>
    /// <param name="queryEmbedding">The query vector.</param>
    /// <param name="userId">User ID for filtering.</param>
    /// <param name="limit">Maximum results to return.</param>
    /// <param name="threshold">Minimum similarity threshold (default: 0.92).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Similar embeddings ordered by similarity, verified embeddings first.</returns>
    Task<IReadOnlyList<ExpenseEmbedding>> FindSimilarAsync(
        Vector queryEmbedding,
        Guid userId,
        int limit = 5,
        float threshold = 0.92f,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a verified embedding from user confirmation.
    /// </summary>
    /// <param name="descriptionText">Normalized description text.</param>
    /// <param name="glCode">Confirmed GL code.</param>
    /// <param name="department">Confirmed department.</param>
    /// <param name="userId">User ID.</param>
    /// <param name="transactionId">Source transaction ID.</param>
    /// <param name="vendorNormalized">Normalized vendor name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created embedding.</returns>
    Task<ExpenseEmbedding> CreateVerifiedEmbeddingAsync(
        string descriptionText,
        string glCode,
        string department,
        Guid userId,
        Guid transactionId,
        string? vendorNormalized = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges stale unverified embeddings older than retention period.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of embeddings deleted.</returns>
    Task<int> PurgeStaleEmbeddingsAsync(CancellationToken cancellationToken = default);
}
