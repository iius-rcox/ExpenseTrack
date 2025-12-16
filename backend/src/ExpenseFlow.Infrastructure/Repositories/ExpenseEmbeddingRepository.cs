using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IExpenseEmbeddingRepository.
/// Uses pgvector for cosine similarity search.
/// </summary>
public class ExpenseEmbeddingRepository : IExpenseEmbeddingRepository
{
    private readonly ExpenseFlowDbContext _context;

    public ExpenseEmbeddingRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ExpenseEmbedding>> FindSimilarAsync(
        Vector queryEmbedding,
        Guid userId,
        int limit = 5,
        float threshold = 0.92f)
    {
        // pgvector cosine distance: 1 - similarity, so we convert threshold
        var maxDistance = 1 - threshold;

        // Query with cosine similarity, prioritizing verified embeddings
        var results = await _context.ExpenseEmbeddings
            .Where(e => e.UserId == userId)
            .Where(e => e.Embedding.CosineDistance(queryEmbedding) <= maxDistance)
            .OrderBy(e => e.Verified ? 0 : 1) // Verified first
            .ThenBy(e => e.Embedding.CosineDistance(queryEmbedding))
            .Take(limit)
            .ToListAsync();

        return results;
    }

    public async Task AddAsync(ExpenseEmbedding embedding)
    {
        await _context.ExpenseEmbeddings.AddAsync(embedding);
    }

    public async Task<ExpenseEmbedding?> GetByIdAsync(Guid id)
    {
        return await _context.ExpenseEmbeddings.FindAsync(id);
    }

    public async Task MarkVerifiedAsync(Guid id)
    {
        await _context.ExpenseEmbeddings
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Verified, true)
                .SetProperty(e => e.ExpiresAt, (DateTime?)null));
    }

    public async Task<int> DeleteExpiredAsync()
    {
        return await _context.ExpenseEmbeddings
            .Where(e => e.ExpiresAt != null && e.ExpiresAt <= DateTime.UtcNow)
            .ExecuteDeleteAsync();
    }

    public async Task<(int TotalEntries, int VerifiedCount)> GetStatsAsync()
    {
        var stats = await _context.ExpenseEmbeddings
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalEntries = g.Count(),
                VerifiedCount = g.Count(e => e.Verified)
            })
            .FirstOrDefaultAsync();

        return stats != null
            ? (stats.TotalEntries, stats.VerifiedCount)
            : (0, 0);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
