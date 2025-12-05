using ExpenseFlow.Core.Entities;
using Pgvector;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Service interface for expense embedding operations (vector similarity search).
/// </summary>
public interface IExpenseEmbeddingService
{
    /// <summary>
    /// Finds similar embeddings using vector cosine similarity.
    /// </summary>
    /// <param name="queryEmbedding">The query vector (1536 dimensions).</param>
    /// <param name="limit">Maximum results to return.</param>
    /// <param name="threshold">Minimum similarity threshold (0-1).</param>
    /// <returns>List of similar embeddings ordered by similarity.</returns>
    Task<IReadOnlyList<ExpenseEmbedding>> FindSimilarAsync(
        Vector queryEmbedding,
        int limit = 5,
        float threshold = 0.7f);

    /// <summary>
    /// Adds a new expense embedding.
    /// </summary>
    /// <param name="embedding">The embedding to add.</param>
    /// <returns>The saved embedding.</returns>
    Task<ExpenseEmbedding> AddAsync(ExpenseEmbedding embedding);

    /// <summary>
    /// Marks an embedding as verified by the user.
    /// </summary>
    /// <param name="embeddingId">The embedding ID.</param>
    Task MarkVerifiedAsync(Guid embeddingId);

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    /// <returns>Total entries and count of verified entries.</returns>
    Task<(int TotalEntries, int TotalHits)> GetStatsAsync();
}
