using System.Security.Claims;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for user management operations.
/// </summary>
public class UserService : IUserService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ILogger<UserService> _logger;

    public UserService(ExpenseFlowDbContext dbContext, ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<User> GetOrCreateUserAsync(ClaimsPrincipal principal)
    {
        var objectId = principal.GetObjectId()
            ?? throw new ArgumentException("Claims principal missing object identifier");

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.EntraObjectId == objectId);

        if (user is null)
        {
            // Auto-create user on first login (FR-003)
            user = new User
            {
                EntraObjectId = objectId,
                Email = principal.GetEmail() ?? throw new ArgumentException("Claims principal missing email"),
                DisplayName = principal.GetDisplayName() ?? "Unknown User",
                Department = principal.GetDepartment(),
                LastLoginAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Created new user profile for {Email} (ObjectId: {ObjectId})",
                user.Email,
                objectId);
        }
        else
        {
            // Update last login time
            await UpdateLastLoginAsync(user);
        }

        return user;
    }

    /// <inheritdoc />
    public async Task<User?> GetByEntraObjectIdAsync(string entraObjectId)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    /// <inheritdoc />
    public async Task UpdateLastLoginAsync(User user)
    {
        user.LastLoginAt = DateTime.UtcNow;
        _dbContext.Users.Update(user);
        await _dbContext.SaveChangesAsync();
    }
}
