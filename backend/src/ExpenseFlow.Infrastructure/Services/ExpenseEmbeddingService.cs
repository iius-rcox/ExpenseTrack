using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for expense embedding operations with vector similarity search.
/// </summary>
public class ExpenseEmbeddingService : IExpenseEmbeddingService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<ExpenseEmbeddingService> _logger;

    public ExpenseEmbeddingService(ExpenseFlowDbContext dbContext, ILogger<ExpenseEmbeddingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExpenseEmbedding>> FindSimilarAsync(
        Vector queryEmbedding,
        int limit = 5,
        float threshold = 0.7f)
    {
        // Use cosine distance (1 - cosine similarity)
        // Threshold of 0.7 similarity = 0.3 distance
        var maxDistance = 1 - threshold;

        var results = await _dbContext.ExpenseEmbeddings
            .OrderBy(e => e.Embedding.CosineDistance(queryEmbedding))
            .Where(e => e.Embedding.CosineDistance(queryEmbedding) <= maxDistance)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync();

        _logger.LogDebug("Found {Count} similar embeddings within threshold {Threshold}",
            results.Count, threshold);

        return results;
    }

    /// <inheritdoc />
    public async Task<ExpenseEmbedding> AddAsync(ExpenseEmbedding embedding)
    {
        _dbContext.ExpenseEmbeddings.Add(embedding);
        await _dbContext.SaveChangesAsync();

        _logger.LogDebug("Added expense embedding for description: {Description}",
            embedding.DescriptionText.Length > 50
                ? embedding.DescriptionText[..50] + "..."
                : embedding.DescriptionText);

        return embedding;
    }

    /// <inheritdoc />
    public async Task MarkVerifiedAsync(Guid embeddingId)
    {
        var embedding = await _dbContext.ExpenseEmbeddings.FindAsync(embeddingId);
        if (embedding is not null)
        {
            embedding.Verified = true;
            await _dbContext.SaveChangesAsync();
            _logger.LogDebug("Marked embedding {Id} as verified", embeddingId);
        }
    }

    /// <inheritdoc />
    public async Task<(int TotalEntries, int TotalHits)> GetStatsAsync()
    {
        var totalEntries = await _dbContext.ExpenseEmbeddings.CountAsync();
        var verifiedCount = await _dbContext.ExpenseEmbeddings.CountAsync(e => e.Verified);
        return (totalEntries, verifiedCount);
    }
}
