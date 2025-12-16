using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for reference data operations.
/// </summary>
public class ReferenceDataService : IReferenceDataService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly IExternalDataSource _externalDataSource;
    private readonly ILogger<ReferenceDataService> _logger;

    public ReferenceDataService(
        ExpenseFlowDbContext dbContext,
        IExternalDataSource externalDataSource,
        ILogger<ReferenceDataService> logger)
    {
        _dbContext = dbContext;
        _externalDataSource = externalDataSource;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<GLAccount>> GetGLAccountsAsync(bool activeOnly = true)
    {
        var query = _dbContext.GLAccounts.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(g => g.IsActive);
        }
        return await query.OrderBy(g => g.Code).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(bool activeOnly = true)
    {
        var query = _dbContext.Departments.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(d => d.IsActive);
        }
        return await query.OrderBy(d => d.Code).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Project>> GetProjectsAsync(bool activeOnly = true)
    {
        var query = _dbContext.Projects.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(p => p.IsActive);
        }
        return await query.OrderBy(p => p.Code).ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(int GLAccounts, int Departments, int Projects)> SyncAllAsync()
    {
        _logger.LogInformation("Starting reference data sync");

        var glCount = await SyncGLAccountsAsync();
        var deptCount = await SyncDepartmentsAsync();
        var projCount = await SyncProjectsAsync();

        _logger.LogInformation(
            "Reference data sync complete: {GLAccounts} GL accounts, {Departments} departments, {Projects} projects",
            glCount, deptCount, projCount);

        return (glCount, deptCount, projCount);
    }

    private async Task<int> SyncGLAccountsAsync()
    {
        try
        {
            var externalAccounts = await _externalDataSource.GetGLAccountsAsync();
            var syncedAt = DateTime.UtcNow;
            var count = 0;

            foreach (var external in externalAccounts)
            {
                var existing = await _dbContext.GLAccounts.FirstOrDefaultAsync(g => g.Code == external.Code);
                if (existing is null)
                {
                    external.SyncedAt = syncedAt;
                    _dbContext.GLAccounts.Add(external);
                }
                else
                {
                    existing.Name = external.Name;
                    existing.Description = external.Description;
                    existing.IsActive = external.IsActive;
                    existing.SyncedAt = syncedAt;
                }
                count++;
            }

            // Mark accounts not in source as inactive
            var externalCodes = externalAccounts.Select(a => a.Code).ToHashSet();
            var toDeactivate = await _dbContext.GLAccounts
                .Where(g => g.IsActive && !externalCodes.Contains(g.Code))
                .ToListAsync();

            foreach (var account in toDeactivate)
            {
                account.IsActive = false;
                account.SyncedAt = syncedAt;
            }

            await _dbContext.SaveChangesAsync();
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync GL accounts");
            throw;
        }
    }

    private async Task<int> SyncDepartmentsAsync()
    {
        try
        {
            var externalDepts = await _externalDataSource.GetDepartmentsAsync();
            var syncedAt = DateTime.UtcNow;
            var count = 0;

            foreach (var external in externalDepts)
            {
                var existing = await _dbContext.Departments.FirstOrDefaultAsync(d => d.Code == external.Code);
                if (existing is null)
                {
                    external.SyncedAt = syncedAt;
                    _dbContext.Departments.Add(external);
                }
                else
                {
                    existing.Name = external.Name;
                    existing.Description = external.Description;
                    existing.IsActive = external.IsActive;
                    existing.SyncedAt = syncedAt;
                }
                count++;
            }

            // Mark departments not in source as inactive
            var externalCodes = externalDepts.Select(d => d.Code).ToHashSet();
            var toDeactivate = await _dbContext.Departments
                .Where(d => d.IsActive && !externalCodes.Contains(d.Code))
                .ToListAsync();

            foreach (var dept in toDeactivate)
            {
                dept.IsActive = false;
                dept.SyncedAt = syncedAt;
            }

            await _dbContext.SaveChangesAsync();
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync departments");
            throw;
        }
    }

    private async Task<int> SyncProjectsAsync()
    {
        try
        {
            var externalProjects = await _externalDataSource.GetProjectsAsync();
            var syncedAt = DateTime.UtcNow;
            var count = 0;

            foreach (var external in externalProjects)
            {
                var existing = await _dbContext.Projects.FirstOrDefaultAsync(p => p.Code == external.Code);
                if (existing is null)
                {
                    external.SyncedAt = syncedAt;
                    _dbContext.Projects.Add(external);
                }
                else
                {
                    existing.Name = external.Name;
                    existing.Description = external.Description;
                    existing.IsActive = external.IsActive;
                    existing.SyncedAt = syncedAt;
                }
                count++;
            }

            // Mark projects not in source as inactive
            var externalCodes = externalProjects.Select(p => p.Code).ToHashSet();
            var toDeactivate = await _dbContext.Projects
                .Where(p => p.IsActive && !externalCodes.Contains(p.Code))
                .ToListAsync();

            foreach (var project in toDeactivate)
            {
                project.IsActive = false;
                project.SyncedAt = syncedAt;
            }

            await _dbContext.SaveChangesAsync();
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync projects");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<GLAccount?> GetGLAccountByCodeAsync(string code)
    {
        return await _dbContext.GLAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Code == code);
    }

    /// <inheritdoc />
    public async Task<Department?> GetDepartmentByCodeAsync(string code)
    {
        return await _dbContext.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Code == code);
    }
}
