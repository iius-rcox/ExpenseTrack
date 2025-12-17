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
        return await _context.ExpenseReports
            .AsNoTracking()
            .Include(r => r.Lines.OrderBy(l => l.LineOrder))
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
        return await _context.ExpenseLines
            .Include(l => l.Report)
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
}
