using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for detecting and managing travel periods from receipts.
/// Uses Tier 1 (rule-based) detection for airline and hotel vendors.
/// </summary>
public class TravelDetectionService : ITravelDetectionService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ITravelPeriodRepository _travelPeriodRepository;
    private readonly IVendorAliasService _vendorAliasService;
    private readonly ILogger<TravelDetectionService> _logger;

    // Default GL code for travel expenses
    private const string TravelGLCode = "66300";

    public TravelDetectionService(
        ExpenseFlowDbContext dbContext,
        ITravelPeriodRepository travelPeriodRepository,
        IVendorAliasService vendorAliasService,
        ILogger<TravelDetectionService> logger)
    {
        _dbContext = dbContext;
        _travelPeriodRepository = travelPeriodRepository;
        _vendorAliasService = vendorAliasService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TravelDetectionResultDto> DetectFromReceiptAsync(Receipt receipt)
    {
        _logger.LogInformation(
            "Tier 1 - Detecting travel period from receipt {ReceiptId} for user {UserId}",
            receipt.Id, receipt.UserId);

        // Check if vendor matches airline or hotel category using VendorAliasService
        var vendorAlias = await _vendorAliasService.FindMatchingAliasAsync(
            receipt.VendorExtracted ?? string.Empty,
            VendorCategory.Airline, VendorCategory.Hotel);

        if (vendorAlias is null ||
            (vendorAlias.Category != VendorCategory.Airline && vendorAlias.Category != VendorCategory.Hotel))
        {
            _logger.LogDebug(
                "Tier 1 - No travel vendor match for receipt {ReceiptId}: {VendorExtracted}",
                receipt.Id, receipt.VendorExtracted);

            return new TravelDetectionResultDto
            {
                Detected = false,
                Action = TravelDetectionAction.None,
                Confidence = 0,
                Message = "Receipt vendor does not match airline or hotel pattern"
            };
        }

        _logger.LogInformation(
            "Tier 1 - Found travel vendor match: {VendorName} ({Category})",
            vendorAlias.DisplayName, vendorAlias.Category);

        // Extract date from receipt
        var receiptDate = receipt.DateExtracted ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var destination = ExtractDestination(receipt.VendorExtracted, vendorAlias);

        // Check if this extends an existing travel period
        var existingPeriod = await FindOverlappingOrAdjacentPeriodAsync(receipt.UserId, receiptDate);

        TravelPeriod travelPeriod;
        TravelDetectionAction action;

        if (existingPeriod is not null)
        {
            // Extend existing period
            travelPeriod = await ExtendTravelPeriodAsync(existingPeriod, receiptDate, vendorAlias.Category, receipt.Id);
            action = TravelDetectionAction.Extended;
            _logger.LogInformation(
                "Tier 1 - Extended travel period {TravelPeriodId} to {EndDate}",
                travelPeriod.Id, travelPeriod.EndDate);
        }
        else
        {
            // Create new travel period
            travelPeriod = await CreateTravelPeriodFromReceiptAsync(
                receipt.UserId, receiptDate, destination, vendorAlias.Category, receipt.Id);
            action = TravelDetectionAction.Created;
            _logger.LogInformation(
                "Tier 1 - Created new travel period {TravelPeriodId} from {StartDate} to {EndDate}",
                travelPeriod.Id, travelPeriod.StartDate, travelPeriod.EndDate);
        }

        // Determine if AI review is needed (complex itineraries)
        var requiresAiReview = DetermineAiReviewRequired(travelPeriod, vendorAlias.Category);
        if (requiresAiReview && !travelPeriod.RequiresAiReview)
        {
            travelPeriod.RequiresAiReview = true;
            await _travelPeriodRepository.UpdateAsync(travelPeriod);
            await _travelPeriodRepository.SaveChangesAsync();
        }

        return new TravelDetectionResultDto
        {
            Detected = true,
            TravelPeriod = MapToDetailDto(travelPeriod),
            Action = action,
            DetectedCategory = vendorAlias.Category,
            ExtractedDestination = destination,
            Confidence = vendorAlias.Confidence,
            RequiresAiReview = travelPeriod.RequiresAiReview,
            Message = action == TravelDetectionAction.Created
                ? $"Created new travel period for {destination ?? "unknown destination"}"
                : $"Extended existing travel period to {travelPeriod.EndDate}"
        };
    }

    /// <inheritdoc />
    public async Task<TravelPeriodListResponseDto> GetTravelPeriodsAsync(
        Guid userId,
        int page,
        int pageSize,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        var (travelPeriods, totalCount) = await _travelPeriodRepository.GetPagedAsync(
            userId, page, pageSize, startDate, endDate);

        return new TravelPeriodListResponseDto
        {
            TravelPeriods = travelPeriods.Select(MapToSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <inheritdoc />
    public async Task<TravelPeriodDetailDto?> GetTravelPeriodAsync(Guid userId, Guid travelPeriodId)
    {
        var travelPeriod = await _travelPeriodRepository.GetByIdAsync(userId, travelPeriodId);
        return travelPeriod is null ? null : MapToDetailDto(travelPeriod);
    }

    /// <inheritdoc />
    public async Task<TravelPeriodDetailDto> CreateTravelPeriodAsync(
        Guid userId, CreateTravelPeriodRequestDto request)
    {
        var travelPeriod = new TravelPeriod
        {
            UserId = userId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Destination = request.Destination,
            Source = TravelPeriodSource.Manual,
            RequiresAiReview = false
        };

        await _travelPeriodRepository.AddAsync(travelPeriod);
        await _travelPeriodRepository.SaveChangesAsync();

        _logger.LogInformation("Created manual travel period {TravelPeriodId} for user {UserId}",
            travelPeriod.Id, userId);

        return MapToDetailDto(travelPeriod);
    }

    /// <inheritdoc />
    public async Task<TravelPeriodDetailDto?> UpdateTravelPeriodAsync(
        Guid userId, Guid travelPeriodId, UpdateTravelPeriodRequestDto request)
    {
        var travelPeriod = await _travelPeriodRepository.GetByIdAsync(userId, travelPeriodId);
        if (travelPeriod is null) return null;

        travelPeriod.StartDate = request.StartDate;
        travelPeriod.EndDate = request.EndDate;
        travelPeriod.Destination = request.Destination;

        await _travelPeriodRepository.UpdateAsync(travelPeriod);
        await _travelPeriodRepository.SaveChangesAsync();

        _logger.LogInformation("Updated travel period {TravelPeriodId}", travelPeriodId);

        return MapToDetailDto(travelPeriod);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTravelPeriodAsync(Guid userId, Guid travelPeriodId)
    {
        var travelPeriod = await _travelPeriodRepository.GetByIdAsync(userId, travelPeriodId);
        if (travelPeriod is null) return false;

        await _travelPeriodRepository.DeleteAsync(travelPeriod);
        await _travelPeriodRepository.SaveChangesAsync();

        _logger.LogInformation("Deleted travel period {TravelPeriodId}", travelPeriodId);

        return true;
    }

    /// <inheritdoc />
    public async Task<TravelExpenseListResponseDto> GetTravelExpensesAsync(Guid userId, Guid travelPeriodId)
    {
        var travelPeriod = await _travelPeriodRepository.GetByIdAsync(userId, travelPeriodId);
        if (travelPeriod is null)
        {
            return new TravelExpenseListResponseDto();
        }

        // Get receipts within the travel period dates
        var receipts = await _dbContext.Receipts
            .Where(r => r.UserId == userId &&
                        r.DateExtracted.HasValue &&
                        r.DateExtracted.Value >= travelPeriod.StartDate &&
                        r.DateExtracted.Value <= travelPeriod.EndDate)
            .ToListAsync();

        // Get transactions within the travel period dates
        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == userId &&
                        t.TransactionDate >= travelPeriod.StartDate &&
                        t.TransactionDate <= travelPeriod.EndDate)
            .ToListAsync();

        var expenses = new List<TravelExpenseDto>();

        // Add receipts as expenses
        foreach (var receipt in receipts)
        {
            expenses.Add(new TravelExpenseDto
            {
                Id = receipt.Id,
                Date = receipt.DateExtracted!.Value,
                Description = receipt.VendorExtracted ?? "Unknown Vendor",
                Amount = receipt.AmountExtracted ?? 0,
                ReceiptId = receipt.Id,
                SuggestedGLCode = TravelGLCode,
                IsLinked = receipt.Id == travelPeriod.SourceReceiptId,
                Source = TravelExpenseSource.Receipt
            });
        }

        // Add transactions as expenses (exclude ones with matched receipts already counted)
        var matchedReceiptIds = receipts.Select(r => r.Id).ToHashSet();
        foreach (var transaction in transactions.Where(t => t.MatchedReceiptId is null || !matchedReceiptIds.Contains(t.MatchedReceiptId.Value)))
        {
            expenses.Add(new TravelExpenseDto
            {
                Id = transaction.Id,
                Date = transaction.TransactionDate,
                Description = transaction.Description,
                Amount = transaction.Amount,
                ReceiptId = transaction.MatchedReceiptId,
                SuggestedGLCode = TravelGLCode,
                IsLinked = false,
                Source = TravelExpenseSource.Transaction
            });
        }

        var sortedExpenses = expenses.OrderBy(e => e.Date).ThenBy(e => e.Description).ToList();
        var linkedCount = sortedExpenses.Count(e => e.IsLinked);

        return new TravelExpenseListResponseDto
        {
            Expenses = sortedExpenses,
            TotalCount = sortedExpenses.Count,
            TotalAmount = sortedExpenses.Sum(e => e.Amount),
            LinkedCount = linkedCount,
            UnlinkedCount = sortedExpenses.Count - linkedCount
        };
    }

    /// <inheritdoc />
    public async Task<string?> GetSuggestedGLCodeForDateAsync(Guid userId, DateOnly date)
    {
        var isWithinTravel = await IsWithinTravelPeriodAsync(userId, date);
        return isWithinTravel ? TravelGLCode : null;
    }

    /// <inheritdoc />
    public async Task<bool> IsWithinTravelPeriodAsync(Guid userId, DateOnly date)
    {
        var travelPeriod = await _travelPeriodRepository.GetByDateAsync(userId, date);
        return travelPeriod is not null;
    }

    /// <inheritdoc />
    public async Task<TravelTimelineResponseDto> GetTimelineAsync(
        Guid userId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool includeExpenses = true)
    {
        _logger.LogInformation("Getting travel timeline for user {UserId} from {StartDate} to {EndDate}",
            userId, startDate, endDate);

        // Get travel periods within date range
        var travelPeriods = await _dbContext.TravelPeriods
            .Where(tp => tp.UserId == userId)
            .Where(tp => !startDate.HasValue || tp.EndDate >= startDate.Value)
            .Where(tp => !endDate.HasValue || tp.StartDate <= endDate.Value)
            .OrderBy(tp => tp.StartDate)
            .ToListAsync();

        var response = new TravelTimelineResponseDto
        {
            TotalCount = travelPeriods.Count
        };

        var destinationCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var period in travelPeriods)
        {
            var entry = await BuildTimelineEntryAsync(period, includeExpenses);
            response.Periods.Add(entry);
            response.TotalAmount += entry.TotalAmount;

            if (entry.RequiresReview)
            {
                response.PeriodsPendingReview++;
            }

            response.TotalUnlinkedExpenses += entry.UnlinkedExpenseCount;

            // Track destinations for summary
            if (!string.IsNullOrWhiteSpace(entry.Destination))
            {
                destinationCounts.TryGetValue(entry.Destination, out var count);
                destinationCounts[entry.Destination] = count + 1;
            }
        }

        // Build summary statistics
        response.Summary = BuildTimelineSummary(response.Periods, destinationCounts);

        return response;
    }

    #region Private Methods

    private async Task<TravelPeriod?> FindOverlappingOrAdjacentPeriodAsync(Guid userId, DateOnly date)
    {
        // Look for periods that overlap or are adjacent (within 2 days) to the receipt date
        var searchStart = date.AddDays(-2);
        var searchEnd = date.AddDays(2);

        var overlapping = await _travelPeriodRepository.GetOverlappingAsync(userId, searchStart, searchEnd);
        return overlapping.FirstOrDefault();
    }

    private async Task<TravelPeriod> CreateTravelPeriodFromReceiptAsync(
        Guid userId, DateOnly date, string? destination, VendorCategory category, Guid receiptId)
    {
        // For flights, create a period starting/ending on the flight date
        // For hotels, assume at least one night stay
        var (startDate, endDate) = category switch
        {
            VendorCategory.Airline => (date, date),
            VendorCategory.Hotel => (date, date.AddDays(1)),
            _ => (date, date)
        };

        var source = category switch
        {
            VendorCategory.Airline => TravelPeriodSource.Flight,
            VendorCategory.Hotel => TravelPeriodSource.Hotel,
            _ => TravelPeriodSource.Manual
        };

        var travelPeriod = new TravelPeriod
        {
            UserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            Destination = destination,
            Source = source,
            SourceReceiptId = receiptId,
            RequiresAiReview = false
        };

        await _travelPeriodRepository.AddAsync(travelPeriod);
        await _travelPeriodRepository.SaveChangesAsync();

        return travelPeriod;
    }

    private async Task<TravelPeriod> ExtendTravelPeriodAsync(
        TravelPeriod existingPeriod, DateOnly newDate, VendorCategory category, Guid receiptId)
    {
        var changed = false;

        // Extend start date if new date is earlier
        if (newDate < existingPeriod.StartDate)
        {
            existingPeriod.StartDate = newDate;
            changed = true;
        }

        // Extend end date if new date is later
        var effectiveEndDate = category == VendorCategory.Hotel ? newDate.AddDays(1) : newDate;
        if (effectiveEndDate > existingPeriod.EndDate)
        {
            existingPeriod.EndDate = effectiveEndDate;
            changed = true;
        }

        if (changed)
        {
            await _travelPeriodRepository.UpdateAsync(existingPeriod);
            await _travelPeriodRepository.SaveChangesAsync();
        }

        return existingPeriod;
    }

    private static string? ExtractDestination(string? vendorExtracted, VendorAlias vendorAlias)
    {
        // Simple destination extraction - look for city/airport codes after vendor name
        // This is a Tier 1 rule-based extraction; complex cases require AI review
        if (string.IsNullOrWhiteSpace(vendorExtracted)) return null;

        // Remove the vendor pattern and see if there's location info remaining
        var remaining = vendorExtracted
            .ToUpperInvariant()
            .Replace(vendorAlias.AliasPattern.ToUpperInvariant(), "")
            .Trim();

        // Common airport code pattern (3 letters)
        if (remaining.Length >= 3 && remaining.All(char.IsLetter))
        {
            return remaining[..3];
        }

        return null;
    }

    private static bool DetermineAiReviewRequired(TravelPeriod travelPeriod, VendorCategory category)
    {
        // Flag for AI review if:
        // 1. Travel period is unusually long (>14 days)
        // 2. Multiple extensions might indicate complex itinerary
        var duration = travelPeriod.EndDate.DayNumber - travelPeriod.StartDate.DayNumber;
        return duration > 14;
    }

    private static TravelPeriodSummaryDto MapToSummaryDto(TravelPeriod tp)
    {
        return new TravelPeriodSummaryDto
        {
            Id = tp.Id,
            StartDate = tp.StartDate,
            EndDate = tp.EndDate,
            Destination = tp.Destination,
            Source = tp.Source,
            RequiresAiReview = tp.RequiresAiReview,
            ExpenseCount = 0 // Could be populated with a query if needed
        };
    }

    private static TravelPeriodDetailDto MapToDetailDto(TravelPeriod tp)
    {
        return new TravelPeriodDetailDto
        {
            Id = tp.Id,
            StartDate = tp.StartDate,
            EndDate = tp.EndDate,
            Destination = tp.Destination,
            Source = tp.Source,
            SourceReceiptId = tp.SourceReceiptId,
            RequiresAiReview = tp.RequiresAiReview,
            CreatedAt = tp.CreatedAt,
            UpdatedAt = tp.UpdatedAt
        };
    }

    private async Task<TravelTimelineEntryDto> BuildTimelineEntryAsync(TravelPeriod period, bool includeExpenses)
    {
        var entry = new TravelTimelineEntryDto
        {
            Id = period.Id,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Destination = period.Destination,
            DurationDays = period.EndDate.DayNumber - period.StartDate.DayNumber + 1,
            RequiresReview = period.RequiresAiReview,
            ReviewReason = period.RequiresAiReview ? "Complex itinerary detected" : null
        };

        // Get source receipt info if available
        if (period.SourceReceiptId.HasValue)
        {
            var sourceReceipt = await _dbContext.Receipts
                .FirstOrDefaultAsync(r => r.Id == period.SourceReceiptId.Value);

            if (sourceReceipt is not null)
            {
                entry.SourceDocuments.Add(new TravelSourceDocumentDto
                {
                    ReceiptId = sourceReceipt.Id,
                    DocumentType = period.Source.ToString(),
                    VendorName = sourceReceipt.VendorExtracted ?? "Unknown",
                    DocumentDate = sourceReceipt.DateExtracted ?? DateOnly.FromDateTime(sourceReceipt.CreatedAt),
                    Amount = sourceReceipt.AmountExtracted ?? 0,
                    ThumbnailUrl = sourceReceipt.ThumbnailUrl
                });
            }
        }

        // Get receipts within the travel period dates
        var receipts = await _dbContext.Receipts
            .Where(r => r.UserId == period.UserId &&
                        r.DateExtracted.HasValue &&
                        r.DateExtracted.Value >= period.StartDate &&
                        r.DateExtracted.Value <= period.EndDate)
            .ToListAsync();

        // Get transactions within the travel period dates
        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == period.UserId &&
                        t.TransactionDate >= period.StartDate &&
                        t.TransactionDate <= period.EndDate)
            .ToListAsync();

        entry.ReceiptCount = receipts.Count;
        entry.TransactionCount = transactions.Count;

        // Count explicitly linked vs unlinked
        var linkedReceiptIds = new HashSet<Guid>();
        if (period.SourceReceiptId.HasValue)
        {
            linkedReceiptIds.Add(period.SourceReceiptId.Value);
        }

        entry.UnlinkedExpenseCount = receipts.Count(r => !linkedReceiptIds.Contains(r.Id)) + transactions.Count;

        // Calculate total amount
        entry.TotalAmount = receipts.Sum(r => r.AmountExtracted ?? 0) +
                           transactions.Where(t => t.MatchedReceiptId is null || !receipts.Any(r => r.Id == t.MatchedReceiptId))
                                       .Sum(t => t.Amount);

        // Add expenses if requested
        if (includeExpenses)
        {
            // Add receipts
            foreach (var receipt in receipts.OrderBy(r => r.DateExtracted))
            {
                entry.Expenses.Add(new TravelTimelineExpenseDto
                {
                    Id = receipt.Id,
                    ExpenseType = "Receipt",
                    Date = receipt.DateExtracted!.Value,
                    Description = receipt.VendorExtracted ?? "Unknown Vendor",
                    Amount = receipt.AmountExtracted ?? 0,
                    GLCode = TravelGLCode, // Suggest travel GL code for receipts within travel period
                    GLCodeSuggested = true,
                    IsLinked = linkedReceiptIds.Contains(receipt.Id),
                    ThumbnailUrl = receipt.ThumbnailUrl,
                    Category = null // Category not stored on receipt entity
                });
            }

            // Add transactions (exclude duplicates with matched receipts)
            var matchedReceiptIds = receipts.Select(r => r.Id).ToHashSet();
            foreach (var transaction in transactions
                .Where(t => t.MatchedReceiptId is null || !matchedReceiptIds.Contains(t.MatchedReceiptId.Value))
                .OrderBy(t => t.TransactionDate))
            {
                entry.Expenses.Add(new TravelTimelineExpenseDto
                {
                    Id = transaction.Id,
                    ExpenseType = "Transaction",
                    Date = transaction.TransactionDate,
                    Description = transaction.Description,
                    Amount = transaction.Amount,
                    GLCode = TravelGLCode, // Suggest travel GL code for transactions within travel period
                    GLCodeSuggested = true,
                    IsLinked = false,
                    ThumbnailUrl = null,
                    Category = null
                });
            }

            // Sort expenses by date
            entry.Expenses = entry.Expenses.OrderBy(e => e.Date).ThenBy(e => e.Description).ToList();
        }

        return entry;
    }

    private static TravelTimelineSummaryDto BuildTimelineSummary(
        List<TravelTimelineEntryDto> periods,
        Dictionary<string, int> destinationCounts)
    {
        if (periods.Count == 0)
        {
            return new TravelTimelineSummaryDto();
        }

        var totalTravelDays = periods.Sum(p => p.DurationDays);
        var avgDuration = (decimal)totalTravelDays / periods.Count;
        var totalAmount = periods.Sum(p => p.TotalAmount);
        var avgCost = totalAmount / periods.Count;

        var mostVisited = destinationCounts
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key)
            .FirstOrDefault();

        return new TravelTimelineSummaryDto
        {
            TotalTravelDays = totalTravelDays,
            UniqueDestinations = destinationCounts.Count,
            AverageTripDuration = Math.Round(avgDuration, 1),
            AverageTripCost = Math.Round(avgCost, 2),
            MostVisitedDestination = mostVisited.Key,
            TotalReceipts = periods.Sum(p => p.ReceiptCount),
            TotalTransactions = periods.Sum(p => p.TransactionCount)
        };
    }

    #endregion
}
