# Quickstart: Backend API User Preferences

**Feature**: 016-user-preferences-api
**Date**: 2025-12-23

## Prerequisites

- .NET 8 SDK installed
- PostgreSQL 15+ running (Supabase or local)
- Access to ExpenseFlow backend repository
- Azure AD / Entra ID credentials for testing

## Quick Implementation Steps

### Step 1: Create the UserPreferences Entity

Create `backend/src/ExpenseFlow.Core/Entities/UserPreferences.cs`:

```csharp
namespace ExpenseFlow.Core.Entities;

/// <summary>
/// User-configurable application preferences.
/// </summary>
public class UserPreferences : BaseEntity
{
    /// <summary>Foreign key to the owning user.</summary>
    public Guid UserId { get; set; }

    /// <summary>Preferred color theme: "light", "dark", or "system".</summary>
    public string Theme { get; set; } = "system";

    /// <summary>Default department for new expense reports.</summary>
    public Guid? DefaultDepartmentId { get; set; }

    /// <summary>Default project for new expense reports.</summary>
    public Guid? DefaultProjectId { get; set; }
}
```

### Step 2: Add Navigation to User Entity

Update `backend/src/ExpenseFlow.Core/Entities/User.cs`:

```csharp
// Add at end of class
public UserPreferences? Preferences { get; set; }
```

### Step 3: Create DTOs

Create `backend/src/ExpenseFlow.Shared/DTOs/UserPreferencesResponse.cs`:

```csharp
namespace ExpenseFlow.Shared.DTOs;

public class UserPreferencesResponse
{
    public string Theme { get; set; } = "system";
    public Guid? DefaultDepartmentId { get; set; }
    public Guid? DefaultProjectId { get; set; }
}
```

Create `backend/src/ExpenseFlow.Shared/DTOs/UpdatePreferencesRequest.cs`:

```csharp
namespace ExpenseFlow.Shared.DTOs;

public class UpdatePreferencesRequest
{
    public string? Theme { get; set; }
    public Guid? DefaultDepartmentId { get; set; }
    public Guid? DefaultProjectId { get; set; }
}
```

### Step 4: Add DbSet and Configuration

Update `backend/src/ExpenseFlow.Infrastructure/Data/ExpenseFlowDbContext.cs`:

```csharp
// Add DbSet
public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

// In OnModelCreating, add:
modelBuilder.Entity<UserPreferences>(entity =>
{
    entity.HasIndex(e => e.UserId).IsUnique();
    entity.Property(e => e.Theme).HasDefaultValue("system");
    entity.HasOne<User>()
        .WithOne(u => u.Preferences)
        .HasForeignKey<UserPreferences>(p => p.UserId)
        .OnDelete(DeleteBehavior.Cascade);
});
```

### Step 5: Create Service Interface

Create `backend/src/ExpenseFlow.Core/Interfaces/IUserPreferencesService.cs`:

```csharp
namespace ExpenseFlow.Core.Interfaces;

public interface IUserPreferencesService
{
    Task<UserPreferences> GetOrCreateDefaultsAsync(Guid userId);
    Task<UserPreferences> UpdateAsync(Guid userId, UpdatePreferencesRequest request);
}
```

### Step 6: Implement Service

Create `backend/src/ExpenseFlow.Infrastructure/Services/UserPreferencesService.cs`:

```csharp
namespace ExpenseFlow.Infrastructure.Services;

public class UserPreferencesService : IUserPreferencesService
{
    private readonly ExpenseFlowDbContext _dbContext;

    public UserPreferencesService(ExpenseFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UserPreferences> GetOrCreateDefaultsAsync(Guid userId)
    {
        var prefs = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs is null)
        {
            // Return in-memory defaults (don't persist until PATCH)
            return new UserPreferences
            {
                UserId = userId,
                Theme = "system"
            };
        }

        return prefs;
    }

    public async Task<UserPreferences> UpdateAsync(Guid userId, UpdatePreferencesRequest request)
    {
        var prefs = await _dbContext.UserPreferences
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (prefs is null)
        {
            prefs = new UserPreferences { UserId = userId };
            _dbContext.UserPreferences.Add(prefs);
        }

        // Apply partial updates
        if (request.Theme is not null)
            prefs.Theme = request.Theme.ToLowerInvariant();

        if (request.DefaultDepartmentId.HasValue || request.DefaultDepartmentId is null)
            prefs.DefaultDepartmentId = request.DefaultDepartmentId;

        if (request.DefaultProjectId.HasValue || request.DefaultProjectId is null)
            prefs.DefaultProjectId = request.DefaultProjectId;

        await _dbContext.SaveChangesAsync();
        return prefs;
    }
}
```

### Step 7: Add Controller Endpoints

Update `backend/src/ExpenseFlow.Api/Controllers/UsersController.cs`:

```csharp
private readonly IUserPreferencesService _preferencesService;

// Add to constructor
public UsersController(
    IUserService userService,
    IUserPreferencesService preferencesService,
    ILogger<UsersController> logger)
{
    _userService = userService;
    _preferencesService = preferencesService;
    _logger = logger;
}

// Update GetCurrentUser to include preferences
[HttpGet("me")]
public async Task<ActionResult<UserResponse>> GetCurrentUser()
{
    var user = await _userService.GetOrCreateUserAsync(User);
    var prefs = await _preferencesService.GetOrCreateDefaultsAsync(user.Id);

    return Ok(new UserResponse
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Department = user.Department,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        Preferences = new UserPreferencesResponse
        {
            Theme = prefs.Theme,
            DefaultDepartmentId = prefs.DefaultDepartmentId,
            DefaultProjectId = prefs.DefaultProjectId
        }
    });
}

// Add new endpoints
[HttpGet("preferences")]
public async Task<ActionResult<UserPreferencesResponse>> GetPreferences()
{
    var user = await _userService.GetOrCreateUserAsync(User);
    var prefs = await _preferencesService.GetOrCreateDefaultsAsync(user.Id);

    return Ok(new UserPreferencesResponse
    {
        Theme = prefs.Theme,
        DefaultDepartmentId = prefs.DefaultDepartmentId,
        DefaultProjectId = prefs.DefaultProjectId
    });
}

[HttpPatch("preferences")]
public async Task<ActionResult<UserPreferencesResponse>> UpdatePreferences(
    [FromBody] UpdatePreferencesRequest request)
{
    var user = await _userService.GetOrCreateUserAsync(User);
    var prefs = await _preferencesService.UpdateAsync(user.Id, request);

    return Ok(new UserPreferencesResponse
    {
        Theme = prefs.Theme,
        DefaultDepartmentId = prefs.DefaultDepartmentId,
        DefaultProjectId = prefs.DefaultProjectId
    });
}
```

### Step 8: Register Service in DI

Update `backend/src/ExpenseFlow.Api/Program.cs` or service registration:

```csharp
builder.Services.AddScoped<IUserPreferencesService, UserPreferencesService>();
```

### Step 9: Create Migration

```bash
cd backend/src/ExpenseFlow.Api
dotnet ef migrations add AddUserPreferences --project ../ExpenseFlow.Infrastructure
dotnet ef database update --project ../ExpenseFlow.Infrastructure
```

### Step 10: Test

```bash
# Get current user (should include preferences now)
curl -H "Authorization: Bearer $TOKEN" https://localhost:5001/api/user/me

# Get preferences only
curl -H "Authorization: Bearer $TOKEN" https://localhost:5001/api/user/preferences

# Update theme
curl -X PATCH -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"theme": "dark"}' \
  https://localhost:5001/api/user/preferences
```

## Validation Checklist

- [ ] `GET /api/user/me` returns user with preferences
- [ ] `GET /api/user/preferences` returns current preferences (defaults if none set)
- [ ] `PATCH /api/user/preferences` updates theme successfully
- [ ] `PATCH /api/user/preferences` with invalid theme returns 400
- [ ] Theme persists across requests
- [ ] New user gets system defaults
- [ ] Frontend "Failed to update theme" error is resolved

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Migration fails | Ensure PostgreSQL is running and connection string is correct |
| 401 on endpoints | Verify JWT token is valid and [Authorize] attribute is present |
| Theme not persisting | Check DbContext.SaveChangesAsync is called |
| Preferences null on /me | Ensure navigation property is configured correctly |
