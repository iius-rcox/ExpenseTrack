# Feature Specification: Dual Theme System

**Feature Branch**: `015-dual-theme-system`
**Created**: 2025-12-23
**Status**: Draft
**Input**: User description: "Update themes (light and dark) based on themeing.md design system"

## Clarifications

### Session 2025-12-23

- Q: How should glassmorphism behave on browsers that don't support backdrop-filter? → A: Graceful fallback - dark mode shows semi-transparent cards without blur (still usable, slightly less "glassy")
- Q: How should existing "Refined Intelligence" design tokens (slate/copper) be handled? → A: Clean replacement - remove old tokens entirely and update all components to use new theme variables

## Overview

ExpenseFlow currently uses a "Refined Intelligence" design system with slate colors and copper accents. This feature replaces that system with the new dual-theme design defined in themeing.md:

- **Light Mode: "Luxury Minimalist"** - Premium, elegant aesthetic with deep emerald green (#2d5f4f) as the primary accent
- **Dark Mode: "Dark Cyber"** - Modern fintech aesthetic with cyan (#00bcd4), glassmorphism effects, and gradient text

Users will toggle between themes via a theme switcher in the navigation bar. Both themes share consistent spacing, typography scale, and interaction patterns while providing distinct visual identities.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Toggle Between Light and Dark Themes (Priority: P1)

As a user, I want to switch between light and dark themes so that I can use the application in my preferred visual style and reduce eye strain in different lighting conditions.

**Why this priority**: Theme switching is the core functionality that makes both themes usable. Without it, the dual-theme system has no value.

**Independent Test**: Click the theme toggle button in the navigation bar and verify the entire application updates to the selected theme within 1 second.

**Acceptance Scenarios**:

1. **Given** I am on any page in light mode, **When** I click the theme toggle button, **Then** the page transitions to dark mode with visible animation and all colors update to the Dark Cyber palette
2. **Given** I am on any page in dark mode, **When** I click the theme toggle button, **Then** the page transitions to light mode with visible animation and all colors update to the Luxury Minimalist palette
3. **Given** I have selected dark mode, **When** I close and reopen the browser, **Then** the application loads in dark mode (preference persisted)

---

### User Story 2 - Experience Luxury Minimalist Light Theme (Priority: P1)

As a user, I want the light theme to have a premium, elegant appearance so that ExpenseFlow feels like a sophisticated financial tool.

**Why this priority**: Light mode is the default experience and must deliver the intended luxury minimalist aesthetic from day one.

**Independent Test**: Load the dashboard in light mode and verify all visual elements match the Luxury Minimalist specification (emerald accents, off-white backgrounds, subtle shadows).

**Acceptance Scenarios**:

1. **Given** I am viewing the dashboard in light mode, **When** the page loads, **Then** I see off-white backgrounds (#fafaf8), white cards (#ffffff), and emerald green (#2d5f4f) accent colors
2. **Given** I am viewing stat cards in light mode, **When** I hover over a card, **Then** the card border turns emerald, the shadow intensifies, and the card subtly lifts (translateY -2px)
3. **Given** I am viewing the sidebar in light mode, **When** I navigate between menu items, **Then** active items show an emerald left border and subtle emerald background tint

---

### User Story 3 - Experience Dark Cyber Theme (Priority: P1)

As a user, I want the dark theme to have a modern fintech appearance with glassmorphism effects so that ExpenseFlow feels cutting-edge and visually distinct.

**Why this priority**: Dark mode is expected by modern users and must deliver the distinctive Dark Cyber aesthetic.

**Independent Test**: Load the dashboard in dark mode and verify all visual elements match the Dark Cyber specification (cyan accents, glassmorphism, gradient text).

**Acceptance Scenarios**:

1. **Given** I am viewing the dashboard in dark mode, **When** the page loads, **Then** I see dark navy backgrounds (#0f1419, #1a1f2e), cyan (#00bcd4) accent colors, and cards with glassmorphism effect (semi-transparent with backdrop blur)
2. **Given** I am viewing stat cards in dark mode, **When** I hover over a card, **Then** the card shows a cyan border glow, enhanced shadow with cyan tint, and a shine animation sweeps across
3. **Given** I am viewing large stat values in dark mode, **When** the page loads, **Then** the numbers display with a cyan-to-blue gradient text effect

---

### User Story 4 - Consistent Component Styling Across Themes (Priority: P2)

As a user, I want all UI components (buttons, inputs, tables, badges) to maintain consistent functionality while adapting their visual style to the active theme.

**Why this priority**: Builds on theme foundation (P1) to ensure all interactive elements work correctly in both themes.

**Independent Test**: Interact with forms, buttons, and tables in both themes and verify all interactions work correctly with appropriate visual feedback.

**Acceptance Scenarios**:

1. **Given** I click a primary button in light mode, **When** I hover, **Then** the button darkens slightly and shows a subtle shadow
2. **Given** I click a primary button in dark mode, **When** I hover, **Then** the button shows a cyan glow effect
3. **Given** I am filling a form in either theme, **When** I focus an input field, **Then** the field shows a themed focus ring (emerald in light, cyan in dark)
4. **Given** I view a data table in either theme, **When** I hover a row, **Then** the row highlights with a theme-appropriate background color

---

### User Story 5 - Respect System Theme Preference (Priority: P3)

As a user who has set a system-wide dark mode preference, I want ExpenseFlow to automatically use dark mode on first visit so I don't have to manually switch.

**Why this priority**: Nice-to-have enhancement that improves first-time user experience but is not essential for core functionality.

**Independent Test**: Clear browser storage, set system to dark mode, load ExpenseFlow, and verify it starts in Dark Cyber theme.

**Acceptance Scenarios**:

1. **Given** my operating system is set to dark mode, **When** I visit ExpenseFlow for the first time, **Then** the application loads in Dark Cyber theme
2. **Given** I have manually selected light mode, **When** my system changes to dark mode, **Then** my manual preference is preserved (system preference is only used for initial load)

---

### Edge Cases

- What happens when a user has JavaScript disabled? Theme toggle won't work, but light mode CSS should provide a usable fallback.
- How does the theme handle chart visualizations? Chart colors must use theme-appropriate palettes (emerald tones in light, cyan/blue tones in dark).
- What happens during theme transition? A smooth 300ms transition prevents jarring visual changes.
- How do images and icons adapt? Icons use currentColor to inherit theme colors; decorative images should work on both backgrounds.
- How does glassmorphism degrade on older browsers? Graceful fallback - dark mode shows semi-transparent cards (rgba background) without the blur effect; the UI remains fully functional.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a theme toggle control accessible from the main navigation bar
- **FR-002**: System MUST persist theme preference in browser storage and restore it on subsequent visits
- **FR-003**: System MUST apply the selected theme to all pages and components immediately upon toggle
- **FR-004**: System MUST transition between themes with a smooth animation (300ms duration)
- **FR-005**: Light mode MUST use the Luxury Minimalist palette:
  - Primary background: #fafaf8 (off-white)
  - Card background: #ffffff (white)
  - Primary accent: #2d5f4f (deep emerald)
  - Secondary accent: #4a8f75 (lighter emerald for hover)
  - Primary text: #1a1a1a (near-black)
  - Secondary text: #666666 (medium gray)
  - Tertiary text: #999999 (light gray)
  - Borders: #f0f0f0 (subtle), #e5e5e5 (emphasis)
- **FR-006**: Dark mode MUST use the Dark Cyber palette:
  - Primary background: #0f1419 (dark navy-black)
  - Secondary background: #1a1f2e (dark navy with blue tone)
  - Card background: #1e2438 with glassmorphism (rgba(255,255,255,0.08) + backdrop-blur)
  - Primary accent: #00bcd4 (bright cyan)
  - Secondary accent: #1e88e5 (electric blue for gradients)
  - Primary text: #e0e0e0 (light gray)
  - Secondary text: #b0b0b0 (medium gray)
  - Tertiary text: #808080 (dark gray)
  - Borders: rgba(255,255,255,0.1)
- **FR-007**: Stat cards in dark mode MUST include a hover animation with a shine sweep effect
- **FR-008**: Large stat values and H1 headings in dark mode MUST display with cyan-to-blue gradient text
- **FR-009**: System MUST detect and respect the user's operating system theme preference on first visit (when no stored preference exists)
- **FR-010**: All interactive elements (buttons, inputs, links) MUST have visible focus states in both themes for accessibility
- **FR-011**: Implementation MUST remove all existing "Refined Intelligence" design tokens (slate/copper palette) and replace with the new dual-theme token system - no backward compatibility layer required

### Key Entities

- **Theme Preference**: Stores the user's selected theme (light/dark) and whether it was manually chosen or auto-detected
- **Design Tokens**: Color palette, typography, spacing, and animation values for each theme
- **Theme Context**: Application-wide state that provides current theme to all components

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Theme toggle completes visual transition within 500ms of user click
- **SC-002**: Theme preference persists correctly across browser sessions (100% of the time)
- **SC-003**: All primary components (sidebar, navigation, cards, buttons, inputs, tables) correctly apply theme colors in both modes
- **SC-004**: Both themes maintain WCAG 2.1 AA contrast ratio (minimum 4.5:1 for body text, 3:1 for large text)
- **SC-005**: Users can identify the current theme and locate the toggle control within 5 seconds of viewing any page
- **SC-006**: Zero visual glitches during theme transition (no flash of wrong colors, no layout shifts)

## Assumptions

- The existing shadcn/ui component library supports theming through CSS variables and will adapt to the new color system
- The current Tailwind CSS setup can be extended with custom theme colors without major restructuring
- Custom fonts referenced in themeing.md (if different from current) will be available via Google Fonts or are already included
- The glassmorphism backdrop-filter effect is supported in all target browsers (modern Chrome, Firefox, Safari, Edge)

## Out of Scope

- Per-component theme overrides (e.g., forcing a specific section to always be dark)
- Multiple theme options beyond light/dark (e.g., high contrast, sepia)
- Animated theme backgrounds or particle effects
- Theme scheduling based on time of day
