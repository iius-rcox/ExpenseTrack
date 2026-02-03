using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of ITransactionPredictionRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class TransactionPredictionRepository : ITransactionPredictionRepository
{
    private readonly ExpenseFlowDbContext _context;

    public TransactionPredictionRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionPrediction?> GetByIdAsync(Guid userId, Guid predictionId)
    {
        return await _context.TransactionPredictions
            .Include(p => p.Pattern)
            .Include(p => p.Transaction)
            .FirstOrDefaultAsync(p => p.Id == predictionId && p.UserId == userId);
    }

    public async Task<TransactionPrediction?> GetByTransactionIdAsync(Guid userId, Guid transactionId)
    {
        return await _context.TransactionPredictions
            .Include(p => p.Pattern)
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId && p.UserId == userId);
    }

    public async Task<(List<TransactionPrediction> Predictions, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize,
        PredictionStatus? status = null,
        PredictionConfidence? minConfidence = null)
    {
        var query = _context.TransactionPredictions
            .Include(p => p.Pattern)
            .Include(p => p.Transaction)
            .Where(p => p.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        if (minConfidence.HasValue)
        {
            query = query.Where(p => p.ConfidenceLevel >= minConfidence.Value);
        }

        var totalCount = await query.CountAsync();

        var predictions = await query
            .OrderByDescending(p => p.ConfidenceScore)
            .ThenByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (predictions, totalCount);
    }

    public async Task<List<TransactionPrediction>> GetPendingAsync(
        Guid userId,
        PredictionConfidence minConfidence = PredictionConfidence.Medium)
    {
        return await _context.TransactionPredictions
            .Include(p => p.Pattern)
            .Include(p => p.Transaction)
            .Where(p => p.UserId == userId &&
                       p.Status == PredictionStatus.Pending &&
                       p.ConfidenceLevel >= minConfidence)
            .OrderByDescending(p => p.ConfidenceScore)
            .ToListAsync();
    }

    public async Task<List<TransactionPrediction>> GetByPatternIdAsync(Guid patternId)
    {
        return await _context.TransactionPredictions
            .Include(p => p.Transaction)
            .Where(p => p.PatternId == patternId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<Guid, TransactionPrediction>> GetByTransactionIdsAsync(Guid userId, IEnumerable<Guid> transactionIds)
    {
        var ids = transactionIds.ToList();
        var predictions = await _context.TransactionPredictions
            .Include(p => p.Pattern)
            .Where(p => p.UserId == userId && ids.Contains(p.TransactionId))
            .ToListAsync();

        return predictions.ToDictionary(p => p.TransactionId, p => p);
    }

    public async Task<Dictionary<PredictionStatus, int>> GetStatusCountsAsync(Guid userId)
    {
        var counts = await _context.TransactionPredictions
            .Where(p => p.UserId == userId)
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(c => c.Status, c => c.Count);
    }

    public async Task<Dictionary<PredictionConfidence, int>> GetPendingConfidenceCountsAsync(Guid userId)
    {
        var counts = await _context.TransactionPredictions
            .Where(p => p.UserId == userId && p.Status == PredictionStatus.Pending)
            .GroupBy(p => p.ConfidenceLevel)
            .Select(g => new { Confidence = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(c => c.Confidence, c => c.Count);
    }

    public async Task<bool> ExistsForTransactionAsync(Guid transactionId)
    {
        return await _context.TransactionPredictions
            .AnyAsync(p => p.TransactionId == transactionId);
    }

    public async Task AddAsync(TransactionPrediction prediction)
    {
        await _context.TransactionPredictions.AddAsync(prediction);
    }

    public async Task AddRangeAsync(IEnumerable<TransactionPrediction> predictions)
    {
        await _context.TransactionPredictions.AddRangeAsync(predictions);
    }

    public Task UpdateAsync(TransactionPrediction prediction)
    {
        _context.TransactionPredictions.Update(prediction);
        return Task.CompletedTask;
    }

    public Task UpdateRangeAsync(IEnumerable<TransactionPrediction> predictions)
    {
        _context.TransactionPredictions.UpdateRange(predictions);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TransactionPrediction prediction)
    {
        _context.TransactionPredictions.Remove(prediction);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
