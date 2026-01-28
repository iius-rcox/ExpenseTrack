using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IExpenseReportRepository.
/// Applies soft delete filter and row-level security by userId.
/// </summary>
public class ExpenseReportRepository : IExpenseReportRepository
{
    private readonly ExpenseFlowDbContext _context;

    public ExpenseReportRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<ExpenseReport?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ExpenseReports
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<ExpenseReport?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default)
    {
        // Note: We intentionally do NOT use AsNoTracking() here because ChildAllocations
        // is a self-referencing navigation property (ExpenseLine -> ExpenseLine).
        // AsNoTracking() prevents EF Core from "fixing up" these circular references,
        // which would cause ChildAllocations to be empty even when data exists.
        return await _context.ExpenseReports
            .Include(r => r.User)
            .Include(r => r.Lines.OrderBy(l => l.LineOrder))
                .ThenInclude(l => l.Receipt)
            .Include(r => r.Lines)
                .ThenInclude(l => l.ChildAllocations.OrderBy(c => c.AllocationOrder))
            .Where(r => !r.IsDeleted)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
    }

    public async Task<ExpenseReport?> GetDraftByUserAndPeriodAsync(Guid userId, string period, CancellationToken ct = default)
    {
        return await _context.ExpenseReports
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Where(r => r.UserId == userId && r.Period == period && r.Status == ReportStatus.Draft)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<ExpenseReport>> GetByUserAsync(
        Guid userId,
        ReportStatus? status,
        string? period,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.ExpenseReports
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .Where(r => r.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(period))
        {
            query = query.Where(r => r.Period == period);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountByUserAsync(
        Guid userId,
        ReportStatus? status,
        string? period,
        CancellationToken ct = default)
    {
        var query = _context.ExpenseReports
            .Where(r => !r.IsDeleted)
            .Where(r => r.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (!string.IsNullOrEmpty(period))
        {
            query = query.Where(r => r.Period == period);
        }

        return await query.CountAsync(ct);
    }

    public async Task<ExpenseReport> AddAsync(ExpenseReport report, CancellationToken ct = default)
    {
        _context.ExpenseReports.Add(report);
        await _context.SaveChangesAsync(ct);
        return report;
    }

    public async Task UpdateAsync(ExpenseReport report, CancellationToken ct = default)
    {
        report.UpdatedAt = DateTime.UtcNow;
        _context.ExpenseReports.Update(report);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var report = await _context.ExpenseReports
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);

        if (report != null)
        {
            report.IsDeleted = true;
            report.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<ExpenseLine?> GetLineByIdAsync(Guid reportId, Guid lineId, CancellationToken ct = default)
    {
        // NOTE: Do NOT use .Include(l => l.Report) here - it causes entity tracking conflicts
        // when the report is already tracked by the calling service. EF Core automatically
        // translates navigation property access in Where() to SQL JOINs.
        return await _context.ExpenseLines
            .Where(l => l.ReportId == reportId && l.Id == lineId)
            .Where(l => !l.Report.IsDeleted)
            .FirstOrDefaultAsync(ct);
    }

    public async Task UpdateLineAsync(ExpenseLine line, CancellationToken ct = default)
    {
        line.UpdatedAt = DateTime.UtcNow;
        _context.ExpenseLines.Update(line);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<ExpenseLine> AddLineAsync(ExpenseLine line, CancellationToken ct = default)
    {
        line.CreatedAt = DateTime.UtcNow;
        _context.ExpenseLines.Add(line);
        await _context.SaveChangesAsync(ct);
        return line;
    }

    public async Task<(bool Success, int ChildCount)> RemoveLineAsync(Guid reportId, Guid lineId, CancellationToken ct = default)
    {
        // Find the line to remove
        // NOTE: Do NOT use .Include(l => l.Report) here - it causes entity tracking conflicts
        // when the report is already tracked by the calling service. EF Core automatically
        // translates navigation property access in Where() to SQL JOINs.
        var line = await _context.ExpenseLines
            .Where(l => l.ReportId == reportId && l.Id == lineId)
            .Where(l => !l.Report.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (line == null)
            return (false, 0);

        var childCount = 0;

        // If this is a split parent, remove all child allocations first (cascade delete)
        if (line.IsSplitParent)
        {
            var childLines = await _context.ExpenseLines
                .Where(l => l.ParentLineId == lineId)
                .ToListAsync(ct);

            childCount = childLines.Count;
            _context.ExpenseLines.RemoveRange(childLines);
        }

        // Remove the line itself
        _context.ExpenseLines.Remove(line);
        await _context.SaveChangesAsync(ct);

        return (true, childCount);
    }

    public async Task<bool> IsTransactionOnAnyReportAsync(Guid userId, Guid transactionId, Guid? excludeReportId = null, CancellationToken ct = default)
    {
        var query = _context.ExpenseLines
            .Include(l => l.Report)
            .Where(l => l.TransactionId == transactionId)
            .Where(l => l.Report.UserId == userId)
            .Where(l => !l.Report.IsDeleted);

        if (excludeReportId.HasValue)
        {
            query = query.Where(l => l.ReportId != excludeReportId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task<int> GetMaxLineOrderAsync(Guid reportId, CancellationToken ct = default)
    {
        var maxOrder = await _context.ExpenseLines
            .Where(l => l.ReportId == reportId)
            .Select(l => (int?)l.LineOrder)
            .MaxAsync(ct);

        return maxOrder ?? 0;
    }

    public async Task<HashSet<Guid>> GetTransactionIdsOnReportsAsync(Guid userId, IEnumerable<Guid> transactionIds, CancellationToken ct = default)
    {
        var transactionIdList = transactionIds.ToList();
        if (transactionIdList.Count == 0)
            return new HashSet<Guid>();

        // Single query to get all transaction IDs that are already on active reports
        var usedIds = await _context.ExpenseLines
            .Include(l => l.Report)
            .Where(l => l.TransactionId != null && transactionIdList.Contains(l.TransactionId.Value))
            .Where(l => l.Report.UserId == userId)
            .Where(l => !l.Report.IsDeleted)
            .Select(l => l.TransactionId!.Value)
            .Distinct()
            .ToListAsync(ct);

        return usedIds.ToHashSet();
    }

    public async Task<List<ExpenseLine>> GetChildAllocationsAsync(Guid parentLineId, CancellationToken ct = default)
    {
        return await _context.ExpenseLines
            .Where(l => l.ParentLineId == parentLineId)
            .OrderBy(l => l.AllocationOrder)
            .ToListAsync(ct);
    }

    public async Task<bool> DeleteLineAsync(Guid lineId, CancellationToken ct = default)
    {
        var line = await _context.ExpenseLines.FindAsync(new object[] { lineId }, ct);
        if (line == null)
            return false;

        _context.ExpenseLines.Remove(line);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
