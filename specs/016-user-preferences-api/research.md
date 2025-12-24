# Research: Backend API User Preferences

**Feature**: 016-user-preferences-api
**Date**: 2025-12-23

## Research Summary

This document captures technical decisions made during the planning phase for implementing user preferences API endpoints.

---

## 1. Preference Storage Strategy

**Decision**: Separate `UserPreferences` entity with one-to-one relationship to `User`

**Rationale**:
- Clean separation of identity data (from IdP) vs application preferences (user-controlled)
- Allows independent evolution of preferences without touching core User entity
- Supports future extensibility (add notification prefs, display prefs) without schema changes to User
- Matches frontend's API expectations (`/user/me` for profile, `/user/preferences` for settings)

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Embed preferences as columns in User table | Mixes IdP-sourced data with user-editable data; harder to extend |
| JSONB column on User | Less type-safe; harder to query/index specific preferences |
| Separate Preferences table per preference type | Over-engineered for current scope |

---

## 2. API Route Design

**Decision**: Add endpoints to existing `UsersController` under `/api/user/` (singular)

**Rationale**:
- Frontend already expects `/api/user/preferences` (per `use-settings.ts` hooks)
- Keeps user-related operations cohesive in one controller
- Matches RESTful pattern: `/user/me` = current user, `/user/preferences` = current user's preferences

**Endpoints**:
| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/user/me` | Get current user profile (exists, needs `preferences` property) |
| GET | `/api/user/preferences` | Get current user's preferences |
| PATCH | `/api/user/preferences` | Partial update of preferences |

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| `/api/users/{id}/preferences` | Requires user ID in URL; frontend uses implicit "me" pattern |
| Separate `PreferencesController` | Fragments user-related operations; adds routing complexity |
| PUT instead of PATCH | PATCH better supports partial updates (only theme, only department) |

---

## 3. Theme Value Representation

**Decision**: Store theme as `string` enum with values: `"light"`, `"dark"`, `"system"`

**Rationale**:
- Direct mapping to frontend `next-themes` library values
- String storage is database-agnostic and human-readable
- Enum constraint applied at C# level for type safety

**Implementation**:
```csharp
public enum Theme
{
    Light,
    Dark,
    System
}

// In entity
public Theme Theme { get; set; } = Theme.System;
```

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Integer enum values | Less readable in database; requires mapping |
| Boolean `isDarkMode` | Doesn't support "system" preference |

---

## 4. Default Preference Creation

**Decision**: Create `UserPreferences` record lazily on first read, not at user creation

**Rationale**:
- Avoids adding complexity to existing `GetOrCreateUserAsync` method
- Preferences service handles null case gracefully (returns defaults)
- Most users never change defaults; avoids unnecessary database writes

**Behavior**:
- `GET /user/preferences` returns default values if no record exists (system theme, null dept/project)
- `PATCH /user/preferences` creates record if not exists (upsert pattern)

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Create preferences at user creation | Adds coupling to user creation flow; unnecessary writes |
| Require preferences to exist before read | Frontend would need separate "initialize" call |

---

## 5. Department/Project Validation

**Decision**: Validate that referenced Department/Project IDs exist and are active before saving

**Rationale**:
- Maintains referential integrity without foreign key constraints
- Validates against local PostgreSQL cache (not Vista directly)
- Auto-clears preferences when referenced records become inactive during daily sync
- Clear error messages for invalid references

**Viewpoint Vista Source** (per constitution):
| Data | Vista Table | Filter |
|------|-------------|--------|
| Departments | PRDP | PRCo = 1, ActiveYN = 'Y' |
| Projects/Jobs | JCCM | JCCo = 1, ContractStatus = 0 (active contracts only) |

**User Restrictions**: None - any user can set any active department/job as default

**Display Format**: First 25 chars of name + code in parentheses (e.g., "Highway Construction Pr... (2024-001)")

**Implementation**:
- Check `Departments.AnyAsync(d => d.Id == request.DefaultDepartmentId && d.IsActive)` before saving
- Return 400 Bad Request with validation errors if not found or inactive
- Clear preference (set to null) if user explicitly wants to remove default
- During daily sync: auto-clear preferences pointing to newly-inactive records

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| Foreign key constraints | Would fail on department deletion; need cascade/nullify logic |
| No validation | Users could set invalid defaults; silent failures |
| Real-time Vista queries | Too slow; daily sync with local cache is sufficient |

---

## 6. Concurrent Update Handling

**Decision**: Last-write-wins with no explicit optimistic locking

**Rationale**:
- Preferences are single-user owned; concurrent edits from same user are rare
- Simple implementation; user's most recent action always reflects current state
- For this scope, ETag/version complexity not justified

**Future Consideration**: If audit logging required, add `UpdatedAt` timestamp for debugging.

**Alternatives Considered**:
| Alternative | Why Rejected |
|-------------|--------------|
| ETag-based optimistic locking | Over-engineered for single-user preferences |
| Pessimistic locking | Would block concurrent page loads |

---

## 7. Testing Strategy

**Decision**: Unit tests for service logic, integration tests for API endpoints

**Rationale**:
- Service unit tests: Fast, isolated validation of business rules
- Integration tests: Verify full request/response cycle including auth

**Test Coverage**:
| Layer | Test Type | Focus |
|-------|-----------|-------|
| UserPreferencesService | Unit | GetOrCreateDefaults, UpdatePartial, Validation |
| UsersController | Integration | Auth, Request/Response mapping, HTTP status codes |

---

## Dependencies Resolved

All "NEEDS CLARIFICATION" items from technical context have been resolved through this research:

| Item | Resolution |
|------|------------|
| Storage strategy | Separate UserPreferences entity |
| API route structure | `/api/user/preferences` (singular) |
| Theme values | String enum: light/dark/system |
| Default handling | Lazy creation on first PATCH |
| Validation approach | Soft validation (check exists, no FK) |
