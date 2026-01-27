using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for managing recurring expense allowances.
/// </summary>
public class AllowanceService : IAllowanceService
{
    private readonly ExpenseFlowDbContext _context;
    private readonly ILogger<AllowanceService> _logger;

    public AllowanceService(
        ExpenseFlowDbContext context,
        ILogger<AllowanceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<AllowanceListResponse> GetByUserAsync(
        Guid userId,
        bool? activeOnly = null,
        CancellationToken ct = default)
    {
        var query = _context.RecurringAllowances
            .Where(a => a.UserId == userId);

        if (activeOnly == true)
        {
            query = query.Where(a => a.IsActive);
        }

        var allowances = await query
            .OrderBy(a => a.VendorName)
            .ToListAsync(ct);

        return new AllowanceListResponse
        {
            Items = allowances.Select(MapToResponse).ToList(),
            TotalCount = allowances.Count
        };
    }

    public async Task<AllowanceResponse?> GetByIdAsync(
        Guid userId,
        Guid allowanceId,
        CancellationToken ct = default)
    {
        var allowance = await _context.RecurringAllowances
            .FirstOrDefaultAsync(a => a.Id == allowanceId && a.UserId == userId, ct);

        return allowance == null ? null : MapToResponse(allowance);
    }

    private const int MaxAllowancesPerUser = 50;

    public async Task<AllowanceResponse> CreateAsync(
        Guid userId,
        CreateAllowanceRequest request,
        CancellationToken ct = default)
    {
        // Check maximum allowances per user to prevent abuse
        var existingCount = await _context.RecurringAllowances
            .CountAsync(a => a.UserId == userId && a.IsActive, ct);

        if (existingCount >= MaxAllowancesPerUser)
        {
            _logger.LogWarning(
                "User {UserId} attempted to exceed maximum allowance limit ({Max})",
                userId, MaxAllowancesPerUser);
            throw new InvalidOperationException(
                $"Maximum of {MaxAllowancesPerUser} active allowances per user. Please deactivate unused allowances.");
        }

        // Look up GL name if GL code provided
        string? glName = null;
        if (!string.IsNullOrEmpty(request.GLCode))
        {
            glName = await LookupGLNameAsync(request.GLCode, ct);
        }

        var allowance = new RecurringAllowance
        {
            UserId = userId,
            VendorName = request.VendorName,
            Amount = request.Amount,
            Frequency = request.Frequency,
            GLCode = request.GLCode,
            GLName = glName,
            DepartmentCode = request.DepartmentCode,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Created recurring allowance {AllowanceId} for user {UserId}: {VendorName}",
            allowance.Id, userId, allowance.VendorName);
        _logger.LogDebug(
            "Allowance {AllowanceId} details - Amount: {Amount}, Frequency: {Frequency}",
            allowance.Id, allowance.Amount, allowance.Frequency);

        return MapToResponse(allowance);
    }

    public async Task<AllowanceResponse?> UpdateAsync(
        Guid userId,
        Guid allowanceId,
        UpdateAllowanceRequest request,
        CancellationToken ct = default)
    {
        var allowance = await _context.RecurringAllowances
            .FirstOrDefaultAsync(a => a.Id == allowanceId && a.UserId == userId, ct);

        if (allowance == null)
        {
            _logger.LogWarning(
                "Allowance {AllowanceId} not found or not owned by user {UserId}",
                allowanceId, userId);
            return null;
        }

        // Apply partial updates
        if (request.VendorName != null)
        {
            allowance.VendorName = request.VendorName;
        }

        if (request.Amount.HasValue)
        {
            allowance.Amount = request.Amount.Value;
        }

        if (request.Frequency.HasValue)
        {
            allowance.Frequency = request.Frequency.Value;
        }

        if (request.GLCode != null)
        {
            allowance.GLCode = request.GLCode;
            allowance.GLName = await LookupGLNameAsync(request.GLCode, ct);
        }

        if (request.DepartmentCode != null)
        {
            allowance.DepartmentCode = request.DepartmentCode;
        }

        if (request.Description != null)
        {
            allowance.Description = request.Description;
        }

        allowance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated recurring allowance {AllowanceId} for user {UserId}",
            allowanceId, userId);

        return MapToResponse(allowance);
    }

    public async Task<bool> DeactivateAsync(
        Guid userId,
        Guid allowanceId,
        CancellationToken ct = default)
    {
        var allowance = await _context.RecurringAllowances
            .FirstOrDefaultAsync(a => a.Id == allowanceId && a.UserId == userId, ct);

        if (allowance == null)
        {
            _logger.LogWarning(
                "Cannot deactivate: Allowance {AllowanceId} not found or not owned by user {UserId}",
                allowanceId, userId);
            return false;
        }

        allowance.IsActive = false;
        allowance.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Deactivated recurring allowance {AllowanceId} for user {UserId} (soft delete)",
            allowanceId, userId);

        return true;
    }

    public async Task<List<RecurringAllowance>> GetActiveAllowancesForPeriodAsync(
        Guid userId,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken ct = default)
    {
        // Get all active allowances for the user
        var allowances = await _context.RecurringAllowances
            .Where(a => a.UserId == userId && a.IsActive)
            .ToListAsync(ct);

        var result = new List<RecurringAllowance>();

        foreach (var allowance in allowances)
        {
            var occurrences = GetOccurrencesForPeriod(allowance, periodStart, periodEnd);
            result.AddRange(occurrences);
        }

        _logger.LogDebug(
            "Found {Count} allowance occurrences for user {UserId} in period {Start:yyyy-MM-dd} to {End:yyyy-MM-dd}",
            result.Count, userId, periodStart, periodEnd);

        return result;
    }

    #region Private Helpers

    private static AllowanceResponse MapToResponse(RecurringAllowance allowance)
    {
        return new AllowanceResponse
        {
            Id = allowance.Id,
            UserId = allowance.UserId,
            VendorName = allowance.VendorName,
            Amount = allowance.Amount,
            Frequency = allowance.Frequency,
            GLCode = allowance.GLCode,
            GLName = allowance.GLName,
            DepartmentCode = allowance.DepartmentCode,
            Description = allowance.Description,
            IsActive = allowance.IsActive,
            CreatedAt = allowance.CreatedAt,
            UpdatedAt = allowance.UpdatedAt
        };
    }

    private async Task<string?> LookupGLNameAsync(string glCode, CancellationToken ct)
    {
        var glAccount = await _context.GLAccounts
            .FirstOrDefaultAsync(g => g.Code == glCode && g.IsActive, ct);

        return glAccount?.Name;
    }

    /// <summary>
    /// Returns the allowance multiple times based on frequency within the period.
    /// - Weekly: Returns one copy for each week in the period
    /// - Monthly: Returns one copy
    /// - Quarterly: Returns one copy only if month is Jan/Apr/Jul/Oct
    /// </summary>
    private static List<RecurringAllowance> GetOccurrencesForPeriod(
        RecurringAllowance allowance,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var result = new List<RecurringAllowance>();

        switch (allowance.Frequency)
        {
            case AllowanceFrequency.Weekly:
                // Count weeks in the period
                var weeks = CountWeeksInPeriod(periodStart, periodEnd);
                for (var i = 0; i < weeks; i++)
                {
                    result.Add(allowance);
                }
                break;

            case AllowanceFrequency.Monthly:
                // Monthly always returns once per month
                result.Add(allowance);
                break;

            case AllowanceFrequency.Quarterly:
                // Quarterly only in Jan (1), Apr (4), Jul (7), Oct (10)
                var quarterlyMonths = new[] { 1, 4, 7, 10 };
                if (quarterlyMonths.Contains(periodStart.Month))
                {
                    result.Add(allowance);
                }
                break;
        }

        return result;
    }

    /// <summary>
    /// Counts the number of weeks (or partial weeks) in a period.
    /// Uses the standard definition: 7 days = 1 week.
    /// </summary>
    private static int CountWeeksInPeriod(DateOnly start, DateOnly end)
    {
        var days = end.DayNumber - start.DayNumber + 1;
        // Ceiling division to count partial weeks
        return (days + 6) / 7;
    }

    #endregion
}
