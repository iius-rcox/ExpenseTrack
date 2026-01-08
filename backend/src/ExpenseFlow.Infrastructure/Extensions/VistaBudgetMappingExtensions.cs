using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;

namespace ExpenseFlow.Infrastructure.Extensions;

/// <summary>
/// Extension methods for mapping Vista budget entities to DTOs.
/// </summary>
public static class VistaBudgetMappingExtensions
{
    /// <summary>
    /// Maps a VistaBudget entity to a full DTO.
    /// </summary>
    public static VistaBudgetDto ToDto(this VistaBudget entity)
    {
        return new VistaBudgetDto
        {
            Id = entity.Id,
            JCCo = entity.JCCo,
            Job = entity.Job,
            PhaseCode = entity.PhaseCode,
            CostType = entity.CostType,
            BudgetAmount = entity.BudgetAmount,
            FiscalYear = entity.FiscalYear,
            JobDescription = entity.JobDescription,
            IsActive = entity.IsActive,
            SyncedAt = entity.SyncedAt
        };
    }

    /// <summary>
    /// Maps a VistaBudget entity to a summary DTO for list displays.
    /// </summary>
    public static VistaBudgetSummaryDto ToSummaryDto(this VistaBudget entity)
    {
        return new VistaBudgetSummaryDto
        {
            Id = entity.Id,
            DisplayName = $"{entity.Job} - {entity.PhaseCode} ({entity.CostType})",
            Description = entity.JobDescription,
            BudgetAmount = entity.BudgetAmount,
            FiscalYear = entity.FiscalYear
        };
    }

    /// <summary>
    /// Maps a collection of VistaBudget entities to DTOs.
    /// </summary>
    public static IEnumerable<VistaBudgetDto> ToDto(this IEnumerable<VistaBudget> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    /// <summary>
    /// Maps a collection of VistaBudget entities to summary DTOs.
    /// </summary>
    public static IEnumerable<VistaBudgetSummaryDto> ToSummaryDto(this IEnumerable<VistaBudget> entities)
    {
        return entities.Select(e => e.ToSummaryDto());
    }

    /// <summary>
    /// Maps a BudgetComparison entity to a DTO, including Vista budget details.
    /// </summary>
    public static BudgetComparisonDto ToDto(this BudgetComparison entity, VistaBudget? vistaBudget = null)
    {
        var budget = vistaBudget ?? entity.VistaBudget;

        return new BudgetComparisonDto
        {
            Id = entity.Id,
            VistaBudgetId = entity.VistaBudgetId,
            Job = budget?.Job ?? string.Empty,
            PhaseCode = budget?.PhaseCode ?? string.Empty,
            CostType = budget?.CostType ?? string.Empty,
            JobDescription = budget?.JobDescription,
            PeriodStart = entity.PeriodStart,
            PeriodEnd = entity.PeriodEnd,
            BudgetAmount = entity.BudgetAmount,
            ActualAmount = entity.ActualAmount,
            VarianceAmount = entity.VarianceAmount,
            VariancePercent = entity.VariancePercent,
            CurrentMonthActual = entity.CurrentMonthActual,
            TransactionCount = entity.TransactionCount,
            Status = entity.Status,
            CalculatedAt = entity.CalculatedAt
        };
    }

    /// <summary>
    /// Maps a BudgetComparison to a variance DTO for detailed analysis.
    /// </summary>
    public static BudgetVarianceDto ToVarianceDto(
        this BudgetComparison entity,
        VistaBudget? vistaBudget = null,
        string? trend = null)
    {
        var budget = vistaBudget ?? entity.VistaBudget;
        var remaining = entity.BudgetAmount - entity.ActualAmount;
        var percentUsed = entity.BudgetAmount > 0
            ? (entity.ActualAmount / entity.BudgetAmount) * 100
            : (decimal?)null;

        return new BudgetVarianceDto
        {
            Job = budget?.Job ?? string.Empty,
            PhaseCode = budget?.PhaseCode ?? string.Empty,
            CostType = budget?.CostType ?? string.Empty,
            Budget = entity.BudgetAmount,
            Actual = entity.ActualAmount,
            Variance = entity.VarianceAmount,
            VariancePercent = entity.VariancePercent,
            Remaining = remaining,
            PercentUsed = percentUsed,
            Status = entity.Status,
            Trend = trend
        };
    }

    /// <summary>
    /// Maps a collection of BudgetComparison entities to DTOs.
    /// </summary>
    public static IEnumerable<BudgetComparisonDto> ToDto(this IEnumerable<BudgetComparison> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    /// <summary>
    /// Maps a BudgetSnapshot entity to a DTO.
    /// </summary>
    public static BudgetSnapshotDto ToDto(this BudgetSnapshot entity)
    {
        return new BudgetSnapshotDto
        {
            Id = entity.Id,
            Month = entity.SnapshotMonth,
            BudgetAmount = entity.BudgetAmount,
            ActualAmount = entity.ActualAmount,
            YtdBudget = entity.YtdBudget,
            YtdActual = entity.YtdActual,
            MonthlyVariance = entity.MonthlyVariance,
            YtdVariance = entity.YtdVariance,
            TransactionCount = entity.TransactionCount,
            IsFinalized = entity.IsFinalized
        };
    }

    /// <summary>
    /// Maps a collection of BudgetSnapshot entities to DTOs.
    /// </summary>
    public static IEnumerable<BudgetSnapshotDto> ToDto(this IEnumerable<BudgetSnapshot> entities)
    {
        return entities.Select(e => e.ToDto());
    }

    /// <summary>
    /// Creates a BudgetSummaryDto from a collection of comparisons.
    /// </summary>
    public static BudgetSummaryDto ToSummary(
        this IEnumerable<BudgetComparison> comparisons,
        DateTime? lastSyncedAt = null)
    {
        var list = comparisons.ToList();

        return new BudgetSummaryDto
        {
            TotalBudget = list.Sum(c => c.BudgetAmount),
            TotalActual = list.Sum(c => c.ActualAmount),
            TotalVariance = list.Sum(c => c.VarianceAmount),
            OnTrackCount = list.Count(c => c.Status == BudgetComparisonStatus.OnTrack),
            WarningCount = list.Count(c => c.Status == BudgetComparisonStatus.Warning),
            OverBudgetCount = list.Count(c => c.Status == BudgetComparisonStatus.OverBudget),
            LastSyncedAt = lastSyncedAt
        };
    }

    /// <summary>
    /// Creates a BudgetTrendDto from a VistaBudget and its snapshots.
    /// </summary>
    public static BudgetTrendDto ToTrendDto(
        this VistaBudget entity,
        IEnumerable<BudgetSnapshot> snapshots,
        IEnumerable<BudgetForecastPointDto>? forecast = null)
    {
        return new BudgetTrendDto
        {
            Job = entity.Job,
            PhaseCode = entity.PhaseCode,
            Snapshots = snapshots.Select(s => s.ToDto()).ToList(),
            Forecast = forecast?.ToList()
        };
    }
}
