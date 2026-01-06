using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IReportJobRepository.
/// </summary>
public class ReportJobRepository : IReportJobRepository
{
    private readonly ExpenseFlowDbContext _context;

    public ReportJobRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<ReportGenerationJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.ReportGenerationJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == id, ct);
    }

    public async Task<ReportGenerationJob?> GetActiveByUserAndPeriodAsync(Guid userId, string period, CancellationToken ct = default)
    {
        // Active = not in terminal state (Completed, Failed, Cancelled)
        return await _context.ReportGenerationJobs
            .AsNoTracking()
            .Where(j => j.UserId == userId && j.Period == period)
            .Where(j => j.Status != ReportJobStatus.Completed
                     && j.Status != ReportJobStatus.Failed
                     && j.Status != ReportJobStatus.Cancelled)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<ReportGenerationJob>> GetByUserAsync(
        Guid userId,
        ReportJobStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.ReportGenerationJobs
            .AsNoTracking()
            .Where(j => j.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        return await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<int> GetCountByUserAsync(
        Guid userId,
        ReportJobStatus? status,
        CancellationToken ct = default)
    {
        var query = _context.ReportGenerationJobs
            .Where(j => j.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(j => j.Status == status.Value);
        }

        return await query.CountAsync(ct);
    }

    public async Task<ReportGenerationJob> AddAsync(ReportGenerationJob job, CancellationToken ct = default)
    {
        _context.ReportGenerationJobs.Add(job);
        await _context.SaveChangesAsync(ct);
        return job;
    }

    public async Task UpdateAsync(ReportGenerationJob job, CancellationToken ct = default)
    {
        _context.ReportGenerationJobs.Update(job);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateProgressAsync(
        Guid jobId,
        int processedLines,
        int failedLines,
        DateTime? estimatedCompletionAt,
        CancellationToken ct = default)
    {
        // Use ExecuteUpdate for efficient atomic update without loading entity
        await _context.ReportGenerationJobs
            .Where(j => j.Id == jobId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(j => j.ProcessedLines, processedLines)
                .SetProperty(j => j.FailedLines, failedLines)
                .SetProperty(j => j.EstimatedCompletionAt, estimatedCompletionAt),
                ct);
    }

    public async Task<bool> IsCancellationRequestedAsync(Guid jobId, CancellationToken ct = default)
    {
        var status = await _context.ReportGenerationJobs
            .Where(j => j.Id == jobId)
            .Select(j => j.Status)
            .FirstOrDefaultAsync(ct);

        return status == ReportJobStatus.CancellationRequested;
    }

    public async Task<int> DeleteOlderThanAsync(DateTime olderThan, CancellationToken ct = default)
    {
        // Delete completed jobs older than the cutoff date
        return await _context.ReportGenerationJobs
            .Where(j => j.CompletedAt != null && j.CompletedAt < olderThan)
            .ExecuteDeleteAsync(ct);
    }
}
