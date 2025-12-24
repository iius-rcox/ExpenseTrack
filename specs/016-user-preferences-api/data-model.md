# Data Model: Backend API User Preferences

**Feature**: 016-user-preferences-api
**Date**: 2025-12-23

## Entity Relationship Diagram

```
┌─────────────────────┐       1:1        ┌─────────────────────────┐
│        User         │◄────────────────►│    UserPreferences      │
├─────────────────────┤                  ├─────────────────────────┤
│ Id: Guid (PK)       │                  │ Id: Guid (PK)           │
│ EntraObjectId: str  │                  │ UserId: Guid (FK, UK)   │
│ Email: str          │                  │ Theme: str (enum)       │
│ DisplayName: str    │                  │ DefaultDepartmentId: Guid?│
│ Department: str?    │                  │ DefaultProjectId: Guid? │
│ CreatedAt: DateTime │                  │ CreatedAt: DateTime     │
│ UpdatedAt: DateTime │                  │ UpdatedAt: DateTime     │
│ LastLoginAt: DateTime│                 └─────────────────────────┘
└─────────────────────┘                           │
                                                  │ References (not FK)
                                                  ▼
                                    ┌─────────────────────────┐
                                    │       Department        │
                                    ├─────────────────────────┤
                                    │ Id: Guid (PK)           │
                                    │ Name: str               │
                                    │ Code: str               │
                                    └─────────────────────────┘
                                                  │
                                                  ▼
                                    ┌─────────────────────────┐
                                    │        Project          │
                                    ├─────────────────────────┤
                                    │ Id: Guid (PK)           │
                                    │ Name: str               │
                                    │ Code: str               │
                                    │ DepartmentId: Guid (FK) │
                                    └─────────────────────────┘
```

## Entities

### UserPreferences (NEW)

Stores user-configurable application settings. One record per user, created lazily on first preference update.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | Guid | PK, auto-generated | Unique identifier |
| UserId | Guid | FK → User.Id, Unique | Owning user (1:1 relationship) |
| Theme | string | Not null, Default: "system" | Theme preference: "light", "dark", or "system" |
| DefaultDepartmentId | Guid? | Nullable | Default department for new expense reports |
| DefaultProjectId | Guid? | Nullable | Default project for new expense reports |
| CreatedAt | DateTime | Not null, auto-set | Record creation timestamp |
| UpdatedAt | DateTime | Not null, auto-set | Last modification timestamp |

**Indexes**:
- `IX_UserPreferences_UserId` (Unique) - Ensures 1:1 with User

**Validation Rules**:
- Theme must be one of: "light", "dark", "system"
- If DefaultDepartmentId is set, the department must exist
- If DefaultProjectId is set, the project must exist
- DefaultProjectId without DefaultDepartmentId is allowed (project may have null department)

### User (MODIFIED)

Add navigation property to UserPreferences.

| Field | Type | Change |
|-------|------|--------|
| Preferences | UserPreferences? | NEW - Navigation property |

```csharp
// Add to User.cs
public UserPreferences? Preferences { get; set; }
```

## Enums

### Theme

```csharp
namespace ExpenseFlow.Core.Enums;

/// <summary>
/// User's preferred color theme for the application.
/// </summary>
public enum Theme
{
    /// <summary>Light mode (Luxury Minimalist theme)</summary>
    Light,

    /// <summary>Dark mode (Dark Cyber theme)</summary>
    Dark,

    /// <summary>Follow system/OS preference</summary>
    System
}
```

**Storage**: Stored as lowercase string in PostgreSQL (e.g., "light", "dark", "system")

## DTOs

### UserPreferencesResponse

Response DTO for `GET /api/user/preferences`.

```csharp
namespace ExpenseFlow.Shared.DTOs;

public class UserPreferencesResponse
{
    public string Theme { get; set; } = "system";
    public Guid? DefaultDepartmentId { get; set; }
    public Guid? DefaultProjectId { get; set; }
}
```

### UpdatePreferencesRequest

Request DTO for `PATCH /api/user/preferences`.

```csharp
namespace ExpenseFlow.Shared.DTOs;

public class UpdatePreferencesRequest
{
    /// <summary>Theme preference. Valid values: "light", "dark", "system".</summary>
    public string? Theme { get; set; }

    /// <summary>Default department ID for new expense reports. Null to clear.</summary>
    public Guid? DefaultDepartmentId { get; set; }

    /// <summary>Default project ID for new expense reports. Null to clear.</summary>
    public Guid? DefaultProjectId { get; set; }
}
```

**Validation** (FluentValidation):
- Theme: If provided, must be "light", "dark", or "system" (case-insensitive)
- All fields optional for partial updates

### UserResponse (MODIFIED)

Add preferences to existing response.

```csharp
// Add to existing UserResponse.cs
public UserPreferencesResponse? Preferences { get; set; }
```

## EF Core Configuration

### UserPreferencesConfiguration

```csharp
namespace ExpenseFlow.Infrastructure.Data.Configurations;

public class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.Property(p => p.Theme)
            .HasConversion<string>()
            .HasDefaultValue(Theme.System)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // 1:1 relationship with User
        builder.HasOne<User>()
            .WithOne(u => u.Preferences)
            .HasForeignKey<UserPreferences>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

## Migration

Migration name: `AddUserPreferences`

```sql
-- Up
CREATE TABLE "UserPreferences" (
    "Id" uuid NOT NULL DEFAULT gen_random_uuid(),
    "UserId" uuid NOT NULL,
    "Theme" text NOT NULL DEFAULT 'system',
    "DefaultDepartmentId" uuid NULL,
    "DefaultProjectId" uuid NULL,
    "CreatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "PK_UserPreferences" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_UserPreferences_Users_UserId" FOREIGN KEY ("UserId")
        REFERENCES "Users" ("Id") ON DELETE CASCADE
);

CREATE UNIQUE INDEX "IX_UserPreferences_UserId" ON "UserPreferences" ("UserId");

-- Down
DROP TABLE "UserPreferences";
```

## State Transitions

UserPreferences has no complex state machine. Records are:
1. **Non-existent** → Created on first PATCH
2. **Existing** → Updated on subsequent PATCHes
3. **Deleted** → Cascade deleted when User is deleted

## Data Integrity

| Rule | Enforcement |
|------|-------------|
| One preferences per user | Unique index on UserId |
| Valid theme values | C# enum + EF conversion |
| Valid department/project | Application-level validation (soft check) |
| Orphan cleanup | Cascade delete when user deleted |
