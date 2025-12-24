# Viewpoint Vista Integration Reference

## Overview

ExpenseFlow integrates with Viewpoint Vista ERP for reference data synchronization. This document defines the authoritative patterns for all Vista data access.

## Connection Details

| Setting | Value |
|---------|-------|
| Access Method | Direct SQL queries to Vista database |
| Authentication | SQL Authentication |
| Credential Storage | Azure Key Vault (`SqlServer` connection string) |
| Target Company | JCCo = 1 / PRCo = 1 |

## Source Tables

### Departments (PRDP)

**Vista Table**: `dbo.PRDP` (Payroll Department)

| Vista Column | ExpenseFlow Field | Notes |
|--------------|-------------------|-------|
| PRCo | - | Filter: PRCo = 1 |
| PRDept | Code | Department code (e.g., "07") |
| Description | Name | Department name |
| - | Description | Can derive from Name or leave null |
| ActiveYN | IsActive | 'Y' = active, 'N' = inactive |

**Query Pattern**:
```sql
SELECT
    PRDept AS Code,
    Description AS Name,
    CASE WHEN ActiveYN = 'Y' THEN 1 ELSE 0 END AS IsActive
FROM dbo.PRDP
WHERE PRCo = 1
```

### Projects/Jobs (JCCM)

**Vista Table**: `dbo.JCCM` (Job Cost Contract Master)

| Vista Column | ExpenseFlow Field | Notes |
|--------------|-------------------|-------|
| JCCo | - | Filter: JCCo = 1 |
| Contract | Code | Job/Contract number |
| Description | Name | Job description |
| ContractStatus | IsActive | 0 = Open (active), others = closed |

**Query Pattern**:
```sql
SELECT
    Contract AS Code,
    Description AS Name,
    NULL AS Description,
    CASE WHEN ContractStatus = 0 THEN 1 ELSE 0 END AS IsActive
FROM dbo.JCCM
WHERE JCCo = 1
  AND ContractStatus = 0  -- Active contracts only
```

## Sync Configuration

| Setting | Value |
|---------|-------|
| Frequency | Daily (overnight) |
| Job Type | Hangfire recurring job |
| Failure Action | Alert ops team immediately, continue with stale cache |
| Stale Data Handling | Clear user preferences referencing inactive records |

### Sync Process

1. Query Vista for current active records
2. Upsert into PostgreSQL by Code (match existing, insert new)
3. Mark records not in source as `IsActive = false`
4. Clear any `UserPreferences.DefaultDepartmentId` or `DefaultProjectId` referencing now-inactive records
5. Log sync statistics

## Validation Rules

### User Preference Validation

When a user sets `DefaultDepartmentId` or `DefaultProjectId`:

| Rule | Enforcement |
|------|-------------|
| Must exist | Check against local PostgreSQL cache |
| Must be active | `IsActive = true` required |
| User restriction | None - any user can select any active dept/job |
| Job type filter | Active contracts only (already filtered at sync) |

### Display Format

When showing department/project to users:
```
{Name.Substring(0, 25)} ({Code})
```

Examples:
- `Engineering Department (07)`
- `Highway Construction Pr... (2024-001)`

Truncate name to 25 characters if longer, append ellipsis.

## Error Handling

### Vista Connection Failure

```csharp
// Retry policy (already implemented in SqlServerDataSource)
Policy
    .Handle<SqlException>(ex => IsTransient(ex))
    .Or<TimeoutException>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)))
```

If all retries fail:
1. Send immediate alert to ops team
2. Log error with full exception details
3. Continue using stale cached data
4. Do NOT block user operations

### Invalid Reference Cleanup

During daily sync, if a department/project becomes inactive:

```csharp
// Clear user preferences pointing to inactive records
var stalePrefs = await _dbContext.UserPreferences
    .Where(p => p.DefaultDepartmentId != null
        && !_dbContext.Departments.Any(d => d.Id == p.DefaultDepartmentId && d.IsActive))
    .ToListAsync();

foreach (var pref in stalePrefs)
{
    pref.DefaultDepartmentId = null;
    pref.UpdatedAt = DateTime.UtcNow;
}
```

## Related Files

| File | Purpose |
|------|---------|
| `SqlServerDataSource.cs` | Vista database access |
| `ReferenceDataService.cs` | Sync orchestration |
| `ReferenceDataSyncJob.cs` | Hangfire job definition |
| `Department.cs` | Department entity |
| `Project.cs` | Project entity |

## Version History

| Date | Change |
|------|--------|
| 2025-12-23 | Initial documentation from Vista integration Q&A |
