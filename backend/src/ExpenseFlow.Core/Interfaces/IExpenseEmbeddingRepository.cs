using ExpenseFlow.Core.Entities;
using Pgvector;

namespace ExpenseFlow.Core.Interfaces;

/// <summary>
/// Repository interface for expense embedding operations.
/// </summary>
public interface IExpenseEmbeddingRepository
{
    /// <summary>
    /// Finds similar embeddings using vector cosine similarity.
    /// Verified embeddings are prioritized.
    /// </summary>
    /// <param name="queryEmbedding">Query vector (1536 dimensions).</param>
    /// <param name="userId">User ID for filtering.</param>
    /// <param name="limit">Maximum results to return.</param>
    /// <param name="threshold">Minimum similarity threshold (0-1).</param>
    /// <returns>Similar embeddings ordered by similarity.</returns>
    Task<IReadOnlyList<ExpenseEmbedding>> FindSimilarAsync(
        Vector queryEmbedding,
        Guid userId,
        int limit = 5,
        float threshold = 0.92f);

    /// <summary>
    /// Adds a new embedding.
    /// </summary>
    /// <param name="embedding">The embedding to add.</param>
    Task AddAsync(ExpenseEmbedding embedding);

    /// <summary>
    /// Gets an embedding by ID.
    /// </summary>
    /// <param name="id">Embedding ID.</param>
    /// <returns>Embedding if found.</returns>
    Task<ExpenseEmbedding?> GetByIdAsync(Guid id);

    /// <summary>
    /// Marks an embedding as verified and removes expiration.
    /// </summary>
    /// <param name="id">Embedding ID.</param>
    Task MarkVerifiedAsync(Guid id);

    /// <summary>
    /// Deletes expired unverified embeddings.
    /// </summary>
    /// <returns>Number of embeddings deleted.</returns>
    Task<int> DeleteExpiredAsync();

    /// <summary>
    /// Gets embedding statistics.
    /// </summary>
    /// <returns>Total entries and verified count.</returns>
    Task<(int TotalEntries, int VerifiedCount)> GetStatsAsync();

    /// <summary>
    /// Saves all pending changes.
    /// </summary>
    Task SaveChangesAsync();
}
