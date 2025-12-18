using System.Text.Json.Serialization;

namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Response DTO for import job details.
/// </summary>
public record ImportJobResponse(
    Guid Id,
    string Status,
    string SourceFileName,
    DateTime StartedAt,
    DateTime? CompletedAt,
    ImportProgressDto Progress
);

/// <summary>
/// DTO for import job progress information.
/// </summary>
public record ImportProgressDto(
    int TotalRecords,
    int ProcessedRecords,
    int CachedDescriptions,
    int CreatedAliases,
    int GeneratedEmbeddings,
    int SkippedRecords,
    double PercentComplete
);

/// <summary>
/// Response DTO for paginated list of import jobs.
/// </summary>
public record ImportJobListResponse(
    List<ImportJobResponse> Items,
    int TotalCount,
    int Page,
    int PageSize
);

/// <summary>
/// DTO for individual import error detail.
/// </summary>
public record ImportErrorDto(
    int LineNumber,
    string ErrorMessage,
    string? RawData
);

/// <summary>
/// Response DTO for paginated list of import errors.
/// </summary>
public record ImportErrorListResponse(
    List<ImportErrorDto> Errors,
    int TotalCount,
    int Page,
    int PageSize
);

/// <summary>
/// Response DTO for cache warming statistics.
/// </summary>
public record CacheWarmingStatsResponse(
    string Period,
    CacheWarmingStatsDto Overall,
    List<CacheWarmingStatsByOperationDto>? ByOperation
);

/// <summary>
/// DTO for overall cache warming statistics.
/// </summary>
public record CacheWarmingStatsDto(
    int TotalOperations,
    int Tier1Hits,
    int Tier2Hits,
    int Tier3Hits,
    decimal Tier1HitRate,
    decimal Tier2HitRate,
    decimal Tier3HitRate,
    decimal? EstimatedMonthlyCost,
    int? AvgResponseTimeMs,
    bool BelowTarget
);

/// <summary>
/// DTO for cache statistics grouped by operation type (for warming context).
/// </summary>
public record CacheWarmingStatsByOperationDto(
    string OperationType,
    int Tier1Hits,
    int Tier2Hits,
    int Tier3Hits,
    decimal Tier1HitRate
);

/// <summary>
/// Response DTO for cache warming summary.
/// </summary>
public record CacheWarmingSummaryResponse(
    CacheCountBySourceDto DescriptionCache,
    CacheCountBySourceDto VendorAliases,
    CacheCountBySourceDto ExpenseEmbeddings,
    decimal ExpectedHitRate,
    ImportJobResponse? LastImportJob
);

/// <summary>
/// DTO for cache entry counts by source.
/// </summary>
public record CacheCountBySourceDto(
    int Total,
    int FromImport,
    int FromRuntime
);

/// <summary>
/// Request DTO for starting an import job (used internally).
/// </summary>
public record StartImportRequest(
    Stream FileStream,
    string FileName,
    Guid UserId
);
