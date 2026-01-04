using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for managing extraction corrections (training feedback).
/// Records user corrections to AI-extracted receipt fields for model improvement.
/// </summary>
public class ExtractionCorrectionService : IExtractionCorrectionService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<ExtractionCorrectionService> _logger;

    public ExtractionCorrectionService(
        ExpenseFlowDbContext dbContext,
        ILogger<ExtractionCorrectionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ExtractionCorrectionPagedResult> GetCorrectionsAsync(
        ExtractionCorrectionQueryParams queryParams,
        CancellationToken ct = default)
    {
        var query = _dbContext.ExtractionCorrections
            .Include(c => c.User)
            .AsNoTracking();

        // Apply filters
        if (!string.IsNullOrEmpty(queryParams.FieldName))
        {
            query = query.Where(c => c.FieldName == queryParams.FieldName);
        }

        if (queryParams.StartDate.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= queryParams.StartDate.Value);
        }

        if (queryParams.EndDate.HasValue)
        {
            query = query.Where(c => c.CreatedAt <= queryParams.EndDate.Value);
        }

        if (queryParams.UserId.HasValue)
        {
            query = query.Where(c => c.UserId == queryParams.UserId.Value);
        }

        if (queryParams.ReceiptId.HasValue)
        {
            query = query.Where(c => c.ReceiptId == queryParams.ReceiptId.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply sorting
        query = queryParams.SortBy?.ToLowerInvariant() switch
        {
            "fieldname" => queryParams.SortDirection?.ToLowerInvariant() == "asc"
                ? query.OrderBy(c => c.FieldName)
                : query.OrderByDescending(c => c.FieldName),
            _ => queryParams.SortDirection?.ToLowerInvariant() == "asc"
                ? query.OrderBy(c => c.CreatedAt)
                : query.OrderByDescending(c => c.CreatedAt)
        };

        // Apply pagination
        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(c => new ExtractionCorrectionDto
            {
                Id = c.Id,
                ReceiptId = c.ReceiptId,
                UserId = c.UserId,
                UserName = c.User.DisplayName ?? c.User.Email,
                FieldName = c.FieldName,
                OriginalValue = c.OriginalValue,
                CorrectedValue = c.CorrectedValue,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);

        var totalPages = (int)Math.Ceiling(totalCount / (double)queryParams.PageSize);

        return new ExtractionCorrectionPagedResult
        {
            Items = items,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = queryParams.Page < totalPages,
            HasPreviousPage = queryParams.Page > 1
        };
    }

    /// <inheritdoc />
    public async Task<ExtractionCorrectionDetailDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return await _dbContext.ExtractionCorrections
            .Include(c => c.User)
            .Include(c => c.Receipt)
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ExtractionCorrectionDetailDto
            {
                Id = c.Id,
                ReceiptId = c.ReceiptId,
                UserId = c.UserId,
                UserName = c.User.DisplayName ?? c.User.Email,
                FieldName = c.FieldName,
                OriginalValue = c.OriginalValue,
                CorrectedValue = c.CorrectedValue,
                CreatedAt = c.CreatedAt,
                ReceiptVendor = c.Receipt.VendorExtracted,
                ReceiptDate = c.Receipt.DateExtracted,
                ReceiptAmount = c.Receipt.AmountExtracted
            })
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task RecordCorrectionsAsync(
        Guid receiptId,
        Guid userId,
        IEnumerable<CorrectionMetadataDto> corrections,
        Dictionary<string, string?> currentValues,
        CancellationToken ct = default)
    {
        var correctionsList = corrections.ToList();

        if (correctionsList.Count == 0)
        {
            return;
        }

        var recordedCount = 0;

        foreach (var correction in correctionsList)
        {
            // Get the current (corrected) value for this field
            var fieldKey = correction.FieldName;
            if (correction.FieldName == "line_item" && correction.LineItemIndex.HasValue)
            {
                fieldKey = $"line_item_{correction.LineItemIndex}_{correction.LineItemField}";
            }

            if (!currentValues.TryGetValue(fieldKey, out var correctedValue))
            {
                // If we don't have the current value, skip this correction
                _logger.LogWarning(
                    "Skipping correction for field {FieldName} - current value not provided",
                    correction.FieldName);
                continue;
            }

            // Filter no-op corrections (original equals corrected)
            if (correction.OriginalValue == correctedValue)
            {
                _logger.LogDebug(
                    "Skipping no-op correction for field {FieldName} - value unchanged",
                    correction.FieldName);
                continue;
            }

            var entity = new ExtractionCorrection
            {
                Id = Guid.NewGuid(),
                ReceiptId = receiptId,
                UserId = userId,
                FieldName = correction.FieldName,
                OriginalValue = correction.OriginalValue,
                CorrectedValue = correctedValue,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.ExtractionCorrections.Add(entity);
            recordedCount++;
        }

        if (recordedCount > 0)
        {
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Recorded {Count} extraction correction(s) for receipt {ReceiptId} by user {UserId}",
                recordedCount,
                receiptId,
                userId);
        }
    }
}
