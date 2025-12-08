using ExpenseFlow.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// In-memory cache for analysis sessions between analyze and import steps.
/// Sessions expire after 30 minutes.
/// </summary>
public class AnalysisSessionCache
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalysisSessionCache> _logger;
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(30);

    public AnalysisSessionCache(IMemoryCache cache, ILogger<AnalysisSessionCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Stores analysis session data.
    /// </summary>
    /// <param name="analysisId">Unique session ID.</param>
    /// <param name="session">Session data.</param>
    public void Store(Guid analysisId, AnalysisSession session)
    {
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(DefaultExpiry)
            .RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _logger.LogDebug("Analysis session {SessionId} evicted: {Reason}", key, reason);
            });

        _cache.Set(GetCacheKey(analysisId), session, options);
        _logger.LogDebug("Stored analysis session {SessionId}", analysisId);
    }

    /// <summary>
    /// Retrieves and removes analysis session data.
    /// </summary>
    /// <param name="analysisId">Session ID.</param>
    /// <returns>Session data or null if not found/expired.</returns>
    public AnalysisSession? Retrieve(Guid analysisId)
    {
        var key = GetCacheKey(analysisId);
        if (_cache.TryGetValue(key, out AnalysisSession? session))
        {
            _cache.Remove(key); // Single use - remove after retrieval
            _logger.LogDebug("Retrieved and removed analysis session {SessionId}", analysisId);
            return session;
        }

        _logger.LogWarning("Analysis session {SessionId} not found or expired", analysisId);
        return null;
    }

    /// <summary>
    /// Peeks at session data without removing it.
    /// </summary>
    /// <param name="analysisId">Session ID.</param>
    /// <returns>Session data or null if not found/expired.</returns>
    public AnalysisSession? Peek(Guid analysisId)
    {
        _cache.TryGetValue(GetCacheKey(analysisId), out AnalysisSession? session);
        return session;
    }

    private static string GetCacheKey(Guid analysisId) => $"analysis:{analysisId}";
}

/// <summary>
/// Data stored for an analysis session.
/// </summary>
public class AnalysisSession
{
    /// <summary>
    /// User who initiated the analysis.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Original filename.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Parsed statement data.
    /// </summary>
    public ParsedStatementData ParsedData { get; set; } = null!;

    /// <summary>
    /// Tier used for mapping detection (1 = fingerprint, 3 = AI).
    /// </summary>
    public int TierUsed { get; set; }

    /// <summary>
    /// Fingerprint ID if a fingerprint was matched (null for AI inference).
    /// </summary>
    public Guid? MatchedFingerprintId { get; set; }

    /// <summary>
    /// When the session was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
