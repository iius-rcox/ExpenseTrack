using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for normalizing raw bank descriptions to human-readable format.
/// Uses tiered approach: Tier 1 (cache) → Tier 3 (AI inference).
/// </summary>
public class DescriptionNormalizationService : IDescriptionNormalizationService
{
    private readonly IDescriptionCacheRepository _cacheRepository;
    private readonly ITierUsageService _tierUsageService;
    private readonly IChatCompletionService? _chatCompletionService;
    private readonly ILogger<DescriptionNormalizationService> _logger;
    private readonly int _maxDescriptionLength;

    public DescriptionNormalizationService(
        IDescriptionCacheRepository cacheRepository,
        ITierUsageService tierUsageService,
        IChatCompletionService? chatCompletionService,
        IConfiguration configuration,
        ILogger<DescriptionNormalizationService> logger)
    {
        _cacheRepository = cacheRepository;
        _tierUsageService = tierUsageService;
        _chatCompletionService = chatCompletionService;
        _logger = logger;
        _maxDescriptionLength = configuration.GetValue("Categorization:MaxDescriptionLength", 500);
    }

    public async Task<NormalizationResultDto> NormalizeAsync(
        string rawDescription,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        if (string.IsNullOrWhiteSpace(rawDescription))
        {
            return new NormalizationResultDto
            {
                RawDescription = rawDescription ?? string.Empty,
                NormalizedDescription = rawDescription ?? string.Empty,
                Tier = 0,
                Confidence = 0,
                CacheHit = false
            };
        }

        // Truncate if too long
        var description = rawDescription.Length > _maxDescriptionLength
            ? rawDescription[.._maxDescriptionLength]
            : rawDescription;

        var hash = ComputeHash(description);

        // Tier 1: Cache lookup
        var cached = await _cacheRepository.GetByHashAsync(hash);
        if (cached != null)
        {
            stopwatch.Stop();
            await _cacheRepository.IncrementHitCountAsync(cached.Id);
            await _cacheRepository.SaveChangesAsync();

            await _tierUsageService.LogUsageAsync(
                userId,
                "normalization",
                tierUsed: 1,
                confidence: 1.0m,
                responseTimeMs: (int)stopwatch.ElapsedMilliseconds,
                cacheHit: true,
                cancellationToken: cancellationToken);

            _logger.LogDebug("Cache hit for description: {Hash}", hash[..8]);

            return new NormalizationResultDto
            {
                RawDescription = rawDescription,
                NormalizedDescription = cached.NormalizedDescription,
                Tier = 1,
                Confidence = 1.0m,
                CacheHit = true
            };
        }

        // Tier 3: AI inference
        if (_chatCompletionService == null)
        {
            _logger.LogWarning("Chat completion service not available for normalization");
            return new NormalizationResultDto
            {
                RawDescription = rawDescription,
                NormalizedDescription = rawDescription,
                Tier = 0,
                Confidence = 0,
                CacheHit = false
            };
        }

        try
        {
            var normalizedDescription = await NormalizeWithAIAsync(description, cancellationToken);
            stopwatch.Stop();

            // Cache the result
            var cacheEntry = new DescriptionCache
            {
                RawDescriptionHash = hash,
                RawDescription = description,
                NormalizedDescription = normalizedDescription,
                HitCount = 0,
                LastAccessedAt = DateTime.UtcNow
            };

            await _cacheRepository.AddAsync(cacheEntry);
            await _cacheRepository.SaveChangesAsync();

            await _tierUsageService.LogUsageAsync(
                userId,
                "normalization",
                tierUsed: 3,
                confidence: 0.85m, // AI inference confidence
                responseTimeMs: (int)stopwatch.ElapsedMilliseconds,
                cacheHit: false,
                cancellationToken: cancellationToken);

            _logger.LogDebug("AI normalized description: {Original} -> {Normalized}",
                description[..Math.Min(30, description.Length)],
                normalizedDescription[..Math.Min(30, normalizedDescription.Length)]);

            return new NormalizationResultDto
            {
                RawDescription = rawDescription,
                NormalizedDescription = normalizedDescription,
                Tier = 3,
                Confidence = 0.85m,
                CacheHit = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing description with AI");
            stopwatch.Stop();

            return new NormalizationResultDto
            {
                RawDescription = rawDescription,
                NormalizedDescription = rawDescription, // Return original on failure
                Tier = 0,
                Confidence = 0,
                CacheHit = false
            };
        }
    }

    public async Task<(int TotalEntries, int TotalHits)> GetCacheStatsAsync()
    {
        var stats = await _cacheRepository.GetStatsAsync();
        return ((int)stats.TotalEntries, (int)stats.TotalHits);
    }

    private async Task<string> NormalizeWithAIAsync(string description, CancellationToken cancellationToken)
    {
        var prompt = $"""
            You are a financial transaction description normalizer. Convert the following raw bank/card transaction description into a clean, human-readable format.

            Rules:
            1. Extract the merchant/vendor name
            2. Remove transaction codes, reference numbers, and internal identifiers
            3. Format location information if present (City, State)
            4. Keep relevant details like date references if meaningful
            5. Return ONLY the normalized description, no explanations

            Raw description: {description}

            Normalized description:
            """;

        var response = await _chatCompletionService!.GetChatMessageContentAsync(
            prompt,
            cancellationToken: cancellationToken);

        return response.Content?.Trim() ?? description;
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input.ToLowerInvariant().Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
