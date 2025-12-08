using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Repositories;

/// <summary>
/// Entity Framework Core implementation of IStatementImportRepository.
/// Enforces row-level security by filtering on userId.
/// </summary>
public class StatementImportRepository : IStatementImportRepository
{
    private readonly ExpenseFlowDbContext _context;

    public StatementImportRepository(ExpenseFlowDbContext context)
    {
        _context = context;
    }

    public async Task<StatementImport?> GetByIdAsync(Guid userId, Guid importId)
    {
        return await _context.StatementImports
            .Include(i => i.Fingerprint)
            .FirstOrDefaultAsync(i => i.Id == importId && i.UserId == userId);
    }

    public async Task<(List<StatementImport> Imports, int TotalCount)> GetPagedAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        var query = _context.StatementImports
            .Where(i => i.UserId == userId);

        var totalCount = await query.CountAsync();

        var imports = await query
            .Include(i => i.Fingerprint)
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (imports, totalCount);
    }

    public async Task<StatementImport> AddAsync(StatementImport import)
    {
        _context.StatementImports.Add(import);
        await _context.SaveChangesAsync();
        return import;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
