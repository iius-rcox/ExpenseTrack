using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for month-over-month spending comparison and anomaly detection.
/// </summary>
public class MonthlyComparisonService : IComparisonService
{
    private readonly ExpenseFlowDbContext _context;
    private readonly ILogger<MonthlyComparisonService> _logger;

    // Threshold for "significant" change detection (50%)
    private const decimal SignificantChangeThreshold = 0.50m;

    public MonthlyComparisonService(
        ExpenseFlowDbContext context,
        ILogger<MonthlyComparisonService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MonthlyComparisonDto> GetComparisonAsync(
        Guid userId,
        string currentPeriod,
        string previousPeriod,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Generating MoM comparison for user {UserId}: {CurrentPeriod} vs {PreviousPeriod}",
            userId, currentPeriod, previousPeriod);

        // Parse periods into date ranges
        var currentRange = ParsePeriodRange(currentPeriod);
        var previousRange = ParsePeriodRange(previousPeriod);

        // Get spending by vendor for both periods
        var currentSpending = await GetVendorSpendingAsync(userId, currentRange.Start, currentRange.End, ct);
        var previousSpending = await GetVendorSpendingAsync(userId, previousRange.Start, previousRange.End, ct);

        // Check for recurring vendors (those in the period before previous)
        var beforePreviousRange = ParsePeriodRange(GetPreviousPeriod(previousPeriod));
        var beforePreviousSpending = await GetVendorSpendingAsync(userId, beforePreviousRange.Start, beforePreviousRange.End, ct);

        // Calculate comparison results
        var result = new MonthlyComparisonDto
        {
            CurrentPeriod = currentPeriod,
            PreviousPeriod = previousPeriod,
            Summary = CalculateSummary(currentSpending, previousSpending),
            NewVendors = FindNewVendors(currentSpending, previousSpending),
            MissingRecurring = FindMissingRecurring(currentSpending, previousSpending, beforePreviousSpending),
            SignificantChanges = FindSignificantChanges(currentSpending, previousSpending)
        };

        _logger.LogInformation(
            "MoM comparison complete for user {UserId}: {NewVendorCount} new, {MissingCount} missing, {SignificantCount} significant changes",
            userId, result.NewVendors.Count, result.MissingRecurring.Count, result.SignificantChanges.Count);

        return result;
    }

    private async Task<Dictionary<string, decimal>> GetVendorSpendingAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        // Query transactions grouped by description for the period
        // Note: Transaction uses positive amounts for expenses
        var spending = await _context.Transactions
            .Where(t => t.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate &&
                       t.Amount > 0) // Only expenses (positive amounts in this schema)
            .GroupBy(t => t.Description ?? t.OriginalDescription)
            .Select(g => new
            {
                Vendor = g.Key,
                Total = g.Sum(t => t.Amount)
            })
            .ToListAsync(ct);

        return spending.ToDictionary(x => x.Vendor, x => x.Total);
    }

    private static ComparisonSummaryDto CalculateSummary(
        Dictionary<string, decimal> currentSpending,
        Dictionary<string, decimal> previousSpending)
    {
        var currentTotal = currentSpending.Values.Sum();
        var previousTotal = previousSpending.Values.Sum();
        var change = currentTotal - previousTotal;
        var changePercent = previousTotal != 0
            ? Math.Round((change / previousTotal) * 100, 2)
            : (currentTotal > 0 ? 100m : 0m);

        return new ComparisonSummaryDto
        {
            CurrentTotal = currentTotal,
            PreviousTotal = previousTotal,
            Change = change,
            ChangePercent = changePercent
        };
    }

    private static List<VendorAmountDto> FindNewVendors(
        Dictionary<string, decimal> currentSpending,
        Dictionary<string, decimal> previousSpending)
    {
        return currentSpending
            .Where(c => !previousSpending.ContainsKey(c.Key))
            .OrderByDescending(c => c.Value)
            .Select(c => new VendorAmountDto
            {
                VendorName = c.Key,
                Amount = c.Value
            })
            .ToList();
    }

    private static List<VendorAmountDto> FindMissingRecurring(
        Dictionary<string, decimal> currentSpending,
        Dictionary<string, decimal> previousSpending,
        Dictionary<string, decimal> beforePreviousSpending)
    {
        // Find vendors that appeared in both previous AND before-previous periods
        // but are missing from current period
        var recurringVendors = previousSpending.Keys
            .Intersect(beforePreviousSpending.Keys);

        return recurringVendors
            .Where(vendor => !currentSpending.ContainsKey(vendor))
            .Select(vendor => new VendorAmountDto
            {
                VendorName = vendor,
                Amount = previousSpending[vendor] // Show their last known amount
            })
            .OrderByDescending(v => v.Amount)
            .ToList();
    }

    private static List<VendorChangeDto> FindSignificantChanges(
        Dictionary<string, decimal> currentSpending,
        Dictionary<string, decimal> previousSpending)
    {
        var result = new List<VendorChangeDto>();

        // Find vendors present in both periods
        var commonVendors = currentSpending.Keys.Intersect(previousSpending.Keys);

        foreach (var vendor in commonVendors)
        {
            var currentAmount = currentSpending[vendor];
            var previousAmount = previousSpending[vendor];
            var change = currentAmount - previousAmount;

            // Calculate percentage change
            var changePercent = previousAmount != 0
                ? Math.Round((change / previousAmount) * 100, 2)
                : 100m;

            // Check if change exceeds threshold (50%)
            if (Math.Abs(changePercent) >= SignificantChangeThreshold * 100)
            {
                result.Add(new VendorChangeDto
                {
                    VendorName = vendor,
                    CurrentAmount = currentAmount,
                    PreviousAmount = previousAmount,
                    Change = change,
                    ChangePercent = changePercent
                });
            }
        }

        return result
            .OrderByDescending(v => Math.Abs(v.ChangePercent))
            .ToList();
    }

    private static (DateOnly Start, DateOnly End) ParsePeriodRange(string period)
    {
        // Parse YYYY-MM format
        var parts = period.Split('-');
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var year) ||
            !int.TryParse(parts[1], out var month))
        {
            throw new ArgumentException($"Invalid period format: {period}. Expected YYYY-MM.");
        }

        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        return (startDate, endDate);
    }

    private static string GetPreviousPeriod(string period)
    {
        var parts = period.Split('-');
        var year = int.Parse(parts[0]);
        var month = int.Parse(parts[1]);

        if (month == 1)
        {
            return $"{year - 1}-12";
        }
        return $"{year}-{month - 1:D2}";
    }
}
