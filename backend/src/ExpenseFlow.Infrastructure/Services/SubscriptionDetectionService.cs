using System.Text.Json;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for detecting and managing subscription patterns from transactions.
/// Uses Tier 1 (rule-based) detection with pattern matching against known vendors.
/// </summary>
public class SubscriptionDetectionService : ISubscriptionDetectionService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ILogger<SubscriptionDetectionService> _logger;

    // Threshold for amount variance flagging (20%)
    private const decimal AmountVarianceThreshold = 0.20m;

    // Minimum occurrences to confirm subscription pattern
    private const int MinOccurrencesForPattern = 2;

    public SubscriptionDetectionService(
        ISubscriptionRepository subscriptionRepository,
        ILogger<SubscriptionDetectionService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _logger = logger;
    }

    #region Detection Operations

    /// <inheritdoc />
    public async Task<SubscriptionDetectionResultDto> DetectFromTransactionAsync(Transaction transaction)
    {
        _logger.LogInformation(
            "Tier 1 - Detecting subscription from transaction {TransactionId} for user {UserId}",
            transaction.Id, transaction.UserId);

        // Step 1: Check if vendor matches known subscription vendors
        var knownVendor = await FindKnownVendorAsync(transaction.Description);

        if (knownVendor is not null)
        {
            _logger.LogDebug(
                "Tier 1 - Found known subscription vendor match: {VendorName}",
                knownVendor.DisplayName);

            return await ProcessKnownVendorMatchAsync(transaction, knownVendor);
        }

        // Step 2: Check if this updates an existing subscription pattern
        var existingSubscription = await _subscriptionRepository.GetByVendorNameAsync(
            transaction.UserId, transaction.Description);

        if (existingSubscription is not null)
        {
            return await UpdateExistingSubscriptionAsync(transaction, existingSubscription);
        }

        // No pattern detected (would need recurring charges over time for pattern detection)
        return new SubscriptionDetectionResultDto
        {
            Detected = false,
            Action = SubscriptionDetectionAction.None,
            Confidence = 0,
            Message = "Transaction does not match known subscription patterns"
        };
    }

    /// <inheritdoc />
    public async Task<BatchSubscriptionDetectionResultDto> DetectFromTransactionsAsync(
        Guid userId, IEnumerable<Transaction> transactions)
    {
        var startTime = DateTime.UtcNow;
        var results = new List<SubscriptionDetectionResultDto>();
        var newCount = 0;
        var updatedCount = 0;
        var flaggedCount = 0;

        // Get all known vendors once for efficiency
        var knownVendors = await _subscriptionRepository.GetKnownVendorsAsync();

        foreach (var transaction in transactions)
        {
            var result = await DetectFromTransactionAsync(transaction);
            results.Add(result);

            switch (result.Action)
            {
                case SubscriptionDetectionAction.Created:
                    newCount++;
                    break;
                case SubscriptionDetectionAction.Updated:
                    updatedCount++;
                    break;
                case SubscriptionDetectionAction.Flagged:
                    flaggedCount++;
                    break;
            }
        }

        var processingTime = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;

        _logger.LogInformation(
            "Tier 1 - Batch subscription detection complete for user {UserId}: " +
            "{Count} transactions processed, {New} new, {Updated} updated, {Flagged} flagged in {Time}ms",
            userId, results.Count, newCount, updatedCount, flaggedCount, processingTime);

        return new BatchSubscriptionDetectionResultDto
        {
            TransactionsProcessed = results.Count,
            NewSubscriptions = newCount,
            UpdatedSubscriptions = updatedCount,
            FlaggedSubscriptions = flaggedCount,
            Results = results,
            ProcessingTimeMs = processingTime
        };
    }

    /// <inheritdoc />
    public async Task<List<SubscriptionAlertDto>> RunMonthlyCheckAsync(Guid userId, string month)
    {
        _logger.LogInformation(
            "Tier 1 - Running monthly subscription check for user {UserId}, month {Month}",
            userId, month);

        var alerts = new List<SubscriptionAlertDto>();

        // Get all active subscriptions for user
        var (subscriptions, _) = await _subscriptionRepository.GetPagedAsync(
            userId, 1, 1000, SubscriptionStatus.Active);

        foreach (var subscription in subscriptions)
        {
            var occurrenceMonths = DeserializeOccurrenceMonths(subscription.OccurrenceMonths);

            // Check if this month's charge is missing
            if (!occurrenceMonths.Contains(month))
            {
                // Update status to Missing
                subscription.Status = SubscriptionStatus.Missing;
                subscription.UpdatedAt = DateTime.UtcNow;
                await _subscriptionRepository.UpdateAsync(subscription);

                alerts.Add(new SubscriptionAlertDto
                {
                    Id = Guid.NewGuid(),
                    SubscriptionId = subscription.Id,
                    VendorName = subscription.VendorName,
                    AlertType = SubscriptionAlertType.MissingCharge,
                    Priority = AlertPriority.Medium,
                    Message = $"Expected subscription charge from {subscription.VendorName} not found for {month}",
                    ExpectedAmount = subscription.AverageAmount,
                    ExpectedDate = subscription.ExpectedNextDate,
                    CreatedAt = DateTime.UtcNow,
                    IsAcknowledged = false
                });

                _logger.LogInformation(
                    "Tier 1 - Missing subscription alert created for {VendorName} in {Month}",
                    subscription.VendorName, month);
            }
        }

        await _subscriptionRepository.SaveChangesAsync();

        return alerts;
    }

    #endregion

    #region Subscription Management

    /// <inheritdoc />
    public async Task<SubscriptionListResponseDto> GetSubscriptionsAsync(
        Guid userId, int page, int pageSize, SubscriptionStatus? status = null)
    {
        var (subscriptions, totalCount) = await _subscriptionRepository.GetPagedAsync(
            userId, page, pageSize, status);

        // Get counts by status
        var allSubs = await _subscriptionRepository.GetByStatusesAsync(
            userId, SubscriptionStatus.Active, SubscriptionStatus.Missing, SubscriptionStatus.Flagged);

        return new SubscriptionListResponseDto
        {
            Subscriptions = subscriptions.Select(MapToSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            ActiveCount = allSubs.Count(s => s.Status == SubscriptionStatus.Active),
            MissingCount = allSubs.Count(s => s.Status == SubscriptionStatus.Missing),
            FlaggedCount = allSubs.Count(s => s.Status == SubscriptionStatus.Flagged)
        };
    }

    /// <inheritdoc />
    public async Task<SubscriptionDetailDto?> GetSubscriptionAsync(Guid userId, Guid subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(userId, subscriptionId);
        return subscription is null ? null : MapToDetailDto(subscription);
    }

    /// <inheritdoc />
    public async Task<SubscriptionDetailDto> CreateSubscriptionAsync(
        Guid userId, CreateSubscriptionRequestDto request)
    {
        var now = DateTime.UtcNow;
        var currentMonth = DateOnly.FromDateTime(now).ToString("yyyy-MM");

        var subscription = new DetectedSubscription
        {
            UserId = userId,
            VendorName = request.VendorName,
            AverageAmount = request.ExpectedAmount,
            OccurrenceMonths = JsonSerializer.Serialize(new[] { currentMonth }),
            LastSeenDate = DateOnly.FromDateTime(now),
            ExpectedNextDate = CalculateNextExpectedDate(DateOnly.FromDateTime(now)),
            Status = SubscriptionStatus.Active,
            DetectionSource = DetectionSource.PatternMatch // Manual = PatternMatch source
        };

        await _subscriptionRepository.AddAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Created manual subscription {SubscriptionId} for vendor {VendorName}",
            subscription.Id, subscription.VendorName);

        return MapToDetailDto(subscription);
    }

    /// <inheritdoc />
    public async Task<SubscriptionDetailDto?> UpdateSubscriptionAsync(
        Guid userId, Guid subscriptionId, UpdateSubscriptionRequestDto request)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(userId, subscriptionId);
        if (subscription is null) return null;

        subscription.VendorName = request.VendorName;
        subscription.AverageAmount = request.ExpectedAmount;
        subscription.Status = request.Status;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        _logger.LogInformation("Updated subscription {SubscriptionId}", subscriptionId);

        return MapToDetailDto(subscription);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteSubscriptionAsync(Guid userId, Guid subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(userId, subscriptionId);
        if (subscription is null) return false;

        await _subscriptionRepository.DeleteAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        _logger.LogInformation("Deleted subscription {SubscriptionId}", subscriptionId);

        return true;
    }

    #endregion

    #region Alerts

    /// <inheritdoc />
    public async Task<SubscriptionAlertListResponseDto> GetAlertsAsync(
        Guid userId, bool includeAcknowledged = false)
    {
        // Get subscriptions with Missing or Flagged status for alerts
        var subscriptions = await _subscriptionRepository.GetByStatusesAsync(
            userId, SubscriptionStatus.Missing, SubscriptionStatus.Flagged);

        var alerts = subscriptions.Select(s => new SubscriptionAlertDto
        {
            Id = Guid.NewGuid(), // Generate alert ID
            SubscriptionId = s.Id,
            VendorName = s.VendorName,
            AlertType = s.Status == SubscriptionStatus.Missing
                ? SubscriptionAlertType.MissingCharge
                : SubscriptionAlertType.AmountVariance,
            Priority = s.Status == SubscriptionStatus.Missing ? AlertPriority.Medium : AlertPriority.Low,
            Message = s.Status == SubscriptionStatus.Missing
                ? $"Missing expected charge from {s.VendorName}"
                : $"Amount variance detected for {s.VendorName}",
            ExpectedAmount = s.AverageAmount,
            ExpectedDate = s.ExpectedNextDate,
            CreatedAt = s.UpdatedAt ?? s.CreatedAt,
            IsAcknowledged = false
        }).ToList();

        return new SubscriptionAlertListResponseDto
        {
            Alerts = alerts,
            TotalCount = alerts.Count,
            UnacknowledgedCount = alerts.Count(a => !a.IsAcknowledged)
        };
    }

    /// <inheritdoc />
    public async Task<int> AcknowledgeAlertsAsync(Guid userId, List<Guid> alertIds)
    {
        // For now, alerts are generated on-the-fly from subscription status
        // Acknowledging an alert updates the subscription status back to Active
        var count = 0;

        var subscriptions = await _subscriptionRepository.GetByStatusesAsync(
            userId, SubscriptionStatus.Missing, SubscriptionStatus.Flagged);

        foreach (var subscription in subscriptions)
        {
            subscription.Status = SubscriptionStatus.Active;
            subscription.UpdatedAt = DateTime.UtcNow;
            await _subscriptionRepository.UpdateAsync(subscription);
            count++;
        }

        await _subscriptionRepository.SaveChangesAsync();

        _logger.LogInformation("Acknowledged {Count} subscription alerts for user {UserId}", count, userId);

        return count;
    }

    /// <inheritdoc />
    public async Task<SubscriptionMonitoringSummaryDto> GetMonitoringSummaryAsync(Guid userId)
    {
        var allSubs = await _subscriptionRepository.GetByStatusesAsync(
            userId, SubscriptionStatus.Active, SubscriptionStatus.Missing, SubscriptionStatus.Flagged);

        var activeCount = allSubs.Count(s => s.Status == SubscriptionStatus.Active);
        var missingCount = allSubs.Count(s => s.Status == SubscriptionStatus.Missing);
        var flaggedCount = allSubs.Count(s => s.Status == SubscriptionStatus.Flagged);

        var monthlyExpected = allSubs
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Sum(s => s.AverageAmount);

        var alerts = await GetAlertsAsync(userId);

        return new SubscriptionMonitoringSummaryDto
        {
            TotalActiveSubscriptions = activeCount,
            MissingThisMonth = missingCount,
            FlaggedForReview = flaggedCount,
            NewDetections = 0, // Would require tracking new detections separately
            TotalMonthlyExpected = monthlyExpected,
            TotalMonthlyActual = 0, // Would require querying actual transactions
            RecentAlerts = alerts.Alerts.Take(5).ToList()
        };
    }

    #endregion

    #region Known Vendors

    /// <inheritdoc />
    public async Task<KnownSubscriptionVendor?> FindKnownVendorAsync(string vendorDescription)
    {
        if (string.IsNullOrWhiteSpace(vendorDescription)) return null;

        var normalizedDescription = vendorDescription.ToUpperInvariant();
        var knownVendors = await _subscriptionRepository.GetKnownVendorsAsync();

        foreach (var vendor in knownVendors)
        {
            if (normalizedDescription.Contains(vendor.VendorPattern.ToUpperInvariant()))
            {
                return vendor;
            }
        }

        return null;
    }

    #endregion

    #region Private Methods

    private async Task<SubscriptionDetectionResultDto> ProcessKnownVendorMatchAsync(
        Transaction transaction, KnownSubscriptionVendor knownVendor)
    {
        // Check if subscription already exists for this vendor
        var existingSubscription = await _subscriptionRepository.GetByVendorNameAsync(
            transaction.UserId, knownVendor.DisplayName);

        if (existingSubscription is not null)
        {
            return await UpdateExistingSubscriptionAsync(transaction, existingSubscription);
        }

        // Create new subscription from known vendor match
        var currentMonth = transaction.TransactionDate.ToString("yyyy-MM");

        var subscription = new DetectedSubscription
        {
            UserId = transaction.UserId,
            VendorName = knownVendor.DisplayName,
            AverageAmount = transaction.Amount,
            OccurrenceMonths = JsonSerializer.Serialize(new[] { currentMonth }),
            LastSeenDate = transaction.TransactionDate,
            ExpectedNextDate = CalculateNextExpectedDate(transaction.TransactionDate),
            Status = SubscriptionStatus.Active,
            DetectionSource = DetectionSource.SeedData
        };

        await _subscriptionRepository.AddAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Tier 1 - Created subscription {SubscriptionId} from known vendor {VendorName}",
            subscription.Id, knownVendor.DisplayName);

        return new SubscriptionDetectionResultDto
        {
            Detected = true,
            Subscription = MapToDetailDto(subscription),
            Action = SubscriptionDetectionAction.Created,
            DetectionSource = DetectionSource.SeedData,
            Confidence = 1.0m,
            Message = $"Detected subscription to {knownVendor.DisplayName} from known vendor list",
            RequiresReview = false
        };
    }

    private async Task<SubscriptionDetectionResultDto> UpdateExistingSubscriptionAsync(
        Transaction transaction, DetectedSubscription subscription)
    {
        var currentMonth = transaction.TransactionDate.ToString("yyyy-MM");
        var occurrenceMonths = DeserializeOccurrenceMonths(subscription.OccurrenceMonths);

        // Add current month if not already present
        if (!occurrenceMonths.Contains(currentMonth))
        {
            occurrenceMonths.Add(currentMonth);
        }

        // Check for amount variance
        var variancePercent = Math.Abs(transaction.Amount - subscription.AverageAmount) / subscription.AverageAmount;
        var isFlagged = variancePercent > AmountVarianceThreshold;

        // Recalculate average amount
        var allAmounts = new List<decimal> { transaction.Amount };
        // Note: In a full implementation, we'd track individual amounts
        var newAverage = (subscription.AverageAmount * (occurrenceMonths.Count - 1) + transaction.Amount) / occurrenceMonths.Count;

        subscription.AverageAmount = newAverage;
        subscription.OccurrenceMonths = JsonSerializer.Serialize(occurrenceMonths.OrderBy(m => m));
        subscription.LastSeenDate = transaction.TransactionDate;
        subscription.ExpectedNextDate = CalculateNextExpectedDate(transaction.TransactionDate);
        subscription.Status = isFlagged ? SubscriptionStatus.Flagged : SubscriptionStatus.Active;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _subscriptionRepository.UpdateAsync(subscription);
        await _subscriptionRepository.SaveChangesAsync();

        var action = isFlagged ? SubscriptionDetectionAction.Flagged : SubscriptionDetectionAction.Updated;

        _logger.LogInformation(
            "Tier 1 - Updated subscription {SubscriptionId}: {Action}, variance: {Variance:P1}",
            subscription.Id, action, variancePercent);

        return new SubscriptionDetectionResultDto
        {
            Detected = true,
            Subscription = MapToDetailDto(subscription),
            Action = action,
            DetectionSource = subscription.DetectionSource,
            Confidence = 1.0m,
            AmountVariancePercent = isFlagged ? variancePercent * 100 : null,
            Message = isFlagged
                ? $"Subscription charge varies {variancePercent:P1} from average"
                : $"Updated subscription occurrence for {subscription.VendorName}",
            RequiresReview = isFlagged
        };
    }

    private static List<string> DeserializeOccurrenceMonths(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static DateOnly CalculateNextExpectedDate(DateOnly lastSeenDate)
    {
        // Assume monthly subscription - next charge expected in ~30 days
        return lastSeenDate.AddMonths(1);
    }

    private static SubscriptionSummaryDto MapToSummaryDto(DetectedSubscription s)
    {
        var occurrenceMonths = DeserializeOccurrenceMonths(s.OccurrenceMonths);

        return new SubscriptionSummaryDto
        {
            Id = s.Id,
            VendorName = s.VendorName,
            AverageAmount = s.AverageAmount,
            LastSeenDate = s.LastSeenDate,
            ExpectedNextDate = s.ExpectedNextDate,
            Status = s.Status,
            Category = null, // Would need to track category
            OccurrenceCount = occurrenceMonths.Count
        };
    }

    private static SubscriptionDetailDto MapToDetailDto(DetectedSubscription s)
    {
        var occurrenceMonths = DeserializeOccurrenceMonths(s.OccurrenceMonths);

        return new SubscriptionDetailDto
        {
            Id = s.Id,
            VendorName = s.VendorName,
            AverageAmount = s.AverageAmount,
            MinAmount = s.AverageAmount, // Would need to track min/max separately
            MaxAmount = s.AverageAmount,
            OccurrenceMonths = occurrenceMonths,
            LastSeenDate = s.LastSeenDate,
            ExpectedNextDate = s.ExpectedNextDate,
            Status = s.Status,
            DetectionSource = s.DetectionSource,
            VendorAliasId = s.VendorAliasId,
            Category = null,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }

    #endregion
}
