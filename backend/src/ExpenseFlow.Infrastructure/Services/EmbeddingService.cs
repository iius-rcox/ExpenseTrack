using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Embeddings;
using Pgvector;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for embedding generation and similarity search (Tier 2).
/// </summary>
public class EmbeddingService : IEmbeddingService
{
    private readonly IExpenseEmbeddingRepository _embeddingRepository;
    private readonly ITextEmbeddingGenerationService? _embeddingGenerationService;
    private readonly ILogger<EmbeddingService> _logger;
    private readonly float _similarityThreshold;
    private readonly int _embeddingRetentionMonths;

    public EmbeddingService(
        IExpenseEmbeddingRepository embeddingRepository,
        ITextEmbeddingGenerationService? embeddingGenerationService,
        IConfiguration configuration,
        ILogger<EmbeddingService> logger)
    {
        _embeddingRepository = embeddingRepository;
        _embeddingGenerationService = embeddingGenerationService;
        _logger = logger;
        _similarityThreshold = configuration.GetValue("Categorization:EmbeddingSimilarityThreshold", 0.92f);
        _embeddingRetentionMonths = configuration.GetValue("Categorization:EmbeddingRetentionMonths", 6);
    }

    public async Task<Vector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (_embeddingGenerationService == null)
        {
            throw new InvalidOperationException("Embedding generation service not available");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be empty", nameof(text));
        }

        // Truncate to 500 characters as per spec
        var truncatedText = text.Length > 500 ? text[..500] : text;

        var embedding = await _embeddingGenerationService.GenerateEmbeddingAsync(
            truncatedText,
            cancellationToken: cancellationToken);

        return new Vector(embedding.ToArray());
    }

    public async Task<IReadOnlyList<ExpenseEmbedding>> FindSimilarAsync(
        Vector queryEmbedding,
        Guid userId,
        int limit = 5,
        float threshold = 0.92f,
        CancellationToken cancellationToken = default)
    {
        var effectiveThreshold = threshold > 0 ? threshold : _similarityThreshold;

        var results = await _embeddingRepository.FindSimilarAsync(
            queryEmbedding,
            userId,
            limit,
            effectiveThreshold);

        _logger.LogDebug("Found {Count} similar embeddings for user {UserId} with threshold {Threshold}",
            results.Count, userId, effectiveThreshold);

        return results;
    }

    public async Task<ExpenseEmbedding> CreateVerifiedEmbeddingAsync(
        string descriptionText,
        string glCode,
        string department,
        Guid userId,
        Guid transactionId,
        string? vendorNormalized = null,
        CancellationToken cancellationToken = default)
    {
        var embedding = await GenerateEmbeddingAsync(descriptionText, cancellationToken);

        var expenseEmbedding = new ExpenseEmbedding
        {
            DescriptionText = descriptionText,
            VendorNormalized = vendorNormalized,
            Embedding = embedding,
            GLCode = glCode,
            Department = department,
            Verified = true,
            UserId = userId,
            TransactionId = transactionId,
            ExpiresAt = null // Verified embeddings don't expire
        };

        await _embeddingRepository.AddAsync(expenseEmbedding);
        await _embeddingRepository.SaveChangesAsync();

        _logger.LogInformation("Created verified embedding for transaction {TransactionId}", transactionId);

        return expenseEmbedding;
    }

    public async Task<int> PurgeStaleEmbeddingsAsync(CancellationToken cancellationToken = default)
    {
        var deletedCount = await _embeddingRepository.DeleteExpiredAsync();

        if (deletedCount > 0)
        {
            _logger.LogInformation("Purged {Count} stale embeddings", deletedCount);
        }

        return deletedCount;
    }
}
