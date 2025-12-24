# Feature Specification: Backend API User Preferences

**Feature Branch**: `016-user-preferences-api`
**Created**: 2025-12-23
**Status**: Draft
**Input**: User description: "Backend API User Preferences"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retrieve User Profile (Priority: P1)

A logged-in user navigates to the Settings page and sees their profile information (display name, email, department) loaded from the backend. This is essential for users to confirm their identity and for the application to personalize their experience.

**Why this priority**: Core identity feature - users need to see their own information to trust the system and confirm they're logged into the correct account.

**Independent Test**: Can be fully tested by logging in and navigating to Settings page - the profile section displays the user's name, email, and department without errors.

**Acceptance Scenarios**:

1. **Given** a user is authenticated, **When** they navigate to Settings, **Then** their display name, email, and department are shown.
2. **Given** a user is authenticated for the first time, **When** the system requests their profile, **Then** a profile is automatically created with information from their identity provider.
3. **Given** a user is authenticated, **When** the profile request fails due to network issues, **Then** a friendly error message is displayed with a retry option.

---

### User Story 2 - View User Preferences (Priority: P1)

A logged-in user can see their current application preferences (theme selection, default department, default project). This enables users to understand their current settings before making changes.

**Why this priority**: Foundation for preference management - users must see current preferences before they can change them.

**Independent Test**: Can be fully tested by logging in and navigating to Settings - the preferences section shows the user's current theme setting (light/dark/system), default department, and default project.

**Acceptance Scenarios**:

1. **Given** a user is authenticated, **When** they view Settings, **Then** their current theme preference is displayed.
2. **Given** a new user who has never set preferences, **When** they view Settings, **Then** system defaults are shown (system theme, no default department/project).
3. **Given** a user's preference data is unavailable, **When** they view Settings, **Then** reasonable defaults are applied without error.

---

### User Story 3 - Update Theme Preference (Priority: P1)

A user can select their preferred color theme (light, dark, or system) from the Settings page, and the choice persists across browser sessions and devices. This is the primary use case driving this feature, as the dual-theme system implementation needs backend persistence.

**Why this priority**: Direct user request - theme switching was implemented in frontend but preferences weren't persisted server-side, causing the "Failed to update theme" errors seen on staging.

**Independent Test**: Can be fully tested by selecting a different theme in Settings, logging out, logging back in on a different browser/device, and verifying the same theme is active.

**Acceptance Scenarios**:

1. **Given** a user is on the Settings page, **When** they select "Dark" theme and refresh, **Then** the dark theme remains active.
2. **Given** a user selects "System" theme on a device with dark mode, **When** they view the application, **Then** the dark theme is displayed.
3. **Given** a user changes their theme preference, **When** the update succeeds, **Then** a confirmation message appears and the theme switches immediately.
4. **Given** a user changes their theme preference, **When** the update fails, **Then** an error message appears and the previous theme remains active.

---

### User Story 4 - Update Default Department and Project (Priority: P2)

A user can set their default department and project, which pre-fill when creating new expense reports or categorizing receipts. This reduces repetitive data entry for users who consistently expense to the same cost center.

**Why this priority**: Convenience feature - improves efficiency but doesn't block core functionality.

**Independent Test**: Can be fully tested by setting a default department in Settings, then creating a new expense report and verifying the department is pre-selected.

**Acceptance Scenarios**:

1. **Given** a user sets a default department, **When** they create a new expense report, **Then** that department is pre-selected.
2. **Given** a user sets a default project, **When** they create a new expense report, **Then** that project is pre-selected (if compatible with the default department).
3. **Given** a user clears their default department, **When** they create a new expense report, **Then** no department is pre-selected.

---

### Edge Cases

- What happens when a user's identity provider doesn't provide a display name? System should fall back to email address.
- How does the system handle a user selecting a default project that belongs to a department they no longer have access to? The preference should be cleared or ignored.
- What happens when preferences are updated from multiple devices simultaneously? Last-write-wins with timestamp tracking.
- How are preferences handled when a user account is deactivated and later reactivated? Preferences should be preserved.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST return the authenticated user's profile information when requested.
- **FR-002**: System MUST automatically create a user profile on first authentication using identity provider claims.
- **FR-003**: System MUST return the authenticated user's preferences (theme, default department, default project) when requested.
- **FR-004**: System MUST provide default preferences for users who haven't explicitly set them (system theme, null department/project).
- **FR-005**: Users MUST be able to update their theme preference to one of: light, dark, or system.
- **FR-006**: Users MUST be able to update their default department preference.
- **FR-007**: Users MUST be able to update their default project preference.
- **FR-008**: System MUST validate that selected departments and projects exist before saving preferences.
- **FR-009**: System MUST persist preference changes so they survive browser sessions and are available across devices.
- **FR-010**: System MUST return appropriate error responses when preference updates fail validation.
- **FR-011**: System MUST support partial preference updates (updating only theme without affecting department/project).

### Key Entities

- **User Profile**: Represents the authenticated user's identity information (display name, email, department) sourced from the identity provider. Created automatically on first login.
- **User Preferences**: Represents configurable application settings for each user (theme choice, default department, default project). Linked to User Profile with a one-to-one relationship.
- **Department**: An organizational unit that expenses can be charged to. Users may be assigned a default.
- **Project**: A cost tracking entity within a department. Users may set a default for quick expense creation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view their profile information within 2 seconds of navigating to Settings.
- **SC-002**: Users can update their theme preference and see the change reflected within 1 second.
- **SC-003**: Theme preferences persist across browser sessions with 100% reliability.
- **SC-004**: Theme preferences sync across devices when users log in on a new device.
- **SC-005**: Zero "Failed to update theme" error messages when users change their theme (the specific error that prompted this feature).
- **SC-006**: New users see system defaults immediately without any preference-related errors on first login.
- **SC-007**: 95% of preference update operations complete successfully on the first attempt.

## Assumptions

- Users are authenticated via Entra ID (Azure AD), and identity claims (oid, email, name) are available.
- Departments and projects already exist in the system and have their own management interfaces.
- The existing User entity in the backend can be extended to support preferences without breaking changes.
- Client-side theme switching (via next-themes) will continue to work for immediate visual feedback, with server-side persistence as the source of truth on login.
- Preference data is not sensitive enough to require additional encryption beyond standard database encryption.

## Out of Scope

- User profile editing (changing display name, email) - these come from the identity provider.
- Department and project CRUD operations - covered by existing settings management.
- Notification preferences - may be added in a future iteration.
- Role-based access control for preferences - all authenticated users can manage their own preferences.
