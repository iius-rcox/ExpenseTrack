using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for managing user preferences.
/// </summary>
public class UserPreferencesService : IUserPreferencesService
{
    private static readonly HashSet<string> ValidThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "light", "dark", "system"
    };

    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<UserPreferencesService> _logger;

    public UserPreferencesService(ExpenseFlowDbContext dbContext, ILogger<UserPreferencesService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<UserPreferences> GetOrCreateDefaultsAsync(Guid userId)
    {
        var prefs = await _dbContext.UserPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs is not null)
        {
            _logger.LogDebug("Retrieved existing preferences for user {UserId}", userId);
            return prefs;
        }

        // Return in-memory defaults without persisting
        // Preferences are only persisted on first PATCH
        _logger.LogDebug("Returning default preferences for user {UserId} (not yet persisted)", userId);
        return new UserPreferences
        {
            UserId = userId,
            Theme = "system"
        };
    }

    /// <inheritdoc />
    public async Task<UserPreferences> UpdateAsync(Guid userId, UpdatePreferencesRequest request)
    {
        var prefs = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs is null)
        {
            // Create new preferences record (upsert)
            prefs = new UserPreferences { UserId = userId };
            _dbContext.UserPreferences.Add(prefs);
            _logger.LogInformation("Creating preferences record for user {UserId}", userId);
        }

        // Apply partial updates - only update fields that were explicitly provided
        if (request.Theme is not null)
        {
            if (!ValidThemes.Contains(request.Theme))
            {
                throw new ArgumentException($"Invalid theme value: '{request.Theme}'. Must be one of: light, dark, system");
            }
            prefs.Theme = request.Theme.ToLowerInvariant();
        }

        // Validate and update DefaultDepartmentId if provided
        // Note: We track whether the field was explicitly set (even to null) vs omitted
        if (request.DefaultDepartmentIdProvided)
        {
            if (request.DefaultDepartmentId.HasValue)
            {
                // Validate department exists and is active in local cache
                var departmentExists = await _dbContext.Departments
                    .AnyAsync(d => d.Id == request.DefaultDepartmentId.Value && d.IsActive);

                if (!departmentExists)
                {
                    throw new ArgumentException(
                        $"Department with ID '{request.DefaultDepartmentId.Value}' not found or inactive");
                }
            }
            prefs.DefaultDepartmentId = request.DefaultDepartmentId;
        }

        // Validate and update DefaultProjectId if provided
        if (request.DefaultProjectIdProvided)
        {
            if (request.DefaultProjectId.HasValue)
            {
                // Validate project exists and is active in local cache
                var projectExists = await _dbContext.Projects
                    .AnyAsync(p => p.Id == request.DefaultProjectId.Value && p.IsActive);

                if (!projectExists)
                {
                    throw new ArgumentException(
                        $"Project with ID '{request.DefaultProjectId.Value}' not found or inactive");
                }
            }
            prefs.DefaultProjectId = request.DefaultProjectId;
        }

        prefs.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "Updated preferences for user {UserId}: Theme={Theme}",
            userId,
            prefs.Theme);

        return prefs;
    }
}
