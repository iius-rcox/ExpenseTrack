# Feature Specification: ExpenseFlow End User Guide

**Feature Branch**: `018-end-user-guide`
**Created**: 2025-12-30
**Status**: Draft
**Input**: User description: "Create a highly detailed user guide. This is not a deployment guide, but an end user guide"

## Clarifications

### Session 2025-12-30

- Q: How should the documentation be organized for navigation? → A: Workflow-based (organized as sequential guides: Getting Started → Daily Use → Monthly Close)
- Q: What format should the documentation be delivered in? → A: Markdown files in the repository (rendered by GitHub or a simple docs viewer)
- Q: What visual aid strategy should be used? → A: Annotated screenshots (with numbered callouts, arrows, and highlights)
- Q: Are fingerprint/template workflows fully covered? → A: Added acceptance scenarios 3-5 to User Story 3 for saving, selecting, and managing fingerprints
- Q: Is matching improvement/training covered? → A: Added acceptance scenarios 6-8 to User Story 4 explaining how user actions train the AI
- Q: What additional workflows are missing? → A: Added User Stories 7-9 covering expense splitting, subscription alerts, and travel periods
- Q: What other features need documentation? → A: Added User Stories 10-12 for dashboard understanding, keyboard shortcuts/power user features, and categorization confirmation; enhanced US-13 (troubleshooting) and US-15 (mobile with swipe actions)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - New User Getting Started (Priority: P1)

A new employee receives access to ExpenseFlow and needs to understand how to use the system from their first login through submitting their first expense report. This story covers the complete onboarding journey from authentication to basic expense workflow completion.

**Why this priority**: Without understanding the basic workflow, users cannot accomplish any expense management tasks. This is the foundational knowledge that enables all other features.

**Independent Test**: Can be fully tested by a new user reading the Getting Started section and successfully uploading a receipt, matching it to a transaction, and understanding the dashboard within 15 minutes.

**Acceptance Scenarios**:

1. **Given** a new user with system access, **When** they read the Getting Started section, **Then** they can successfully sign in using their company credentials within 2 minutes
2. **Given** a signed-in new user, **When** they follow the Quick Start guide, **Then** they can upload their first receipt and view the extracted data within 5 minutes
3. **Given** a user who has uploaded a receipt, **When** they consult the Dashboard Overview section, **Then** they understand where to find pending actions and their expense status

---

### User Story 2 - Receipt Upload and AI Processing (Priority: P1)

An employee returns from a business trip with multiple receipts and needs to upload them efficiently, understand the AI extraction results, correct any errors, and prepare them for matching with their credit card statement.

**Why this priority**: Receipt processing is the primary entry point for expense data. Users interact with this feature frequently, and understanding AI confidence scores prevents data quality issues.

**Independent Test**: Can be fully tested by uploading 5 receipts of varying quality, reviewing AI extractions, correcting errors, and understanding status indicators.

**Acceptance Scenarios**:

1. **Given** a user with physical receipts, **When** they read the Receipt Upload section, **Then** they can drag-and-drop or photograph multiple receipts using the documented methods
2. **Given** receipts with AI extraction complete, **When** the user reads the AI Extraction section, **Then** they understand confidence scores (green/amber/red) and can identify which fields need manual review
3. **Given** a receipt with extraction errors, **When** the user follows the editing instructions, **Then** they can correct merchant name, amount, or date using inline editing
4. **Given** a failed upload, **When** the user reads the Troubleshooting section, **Then** they can identify the cause (file size, format) and retry successfully

---

### User Story 3 - Statement Import and Transaction Management (Priority: P1)

A user downloads their monthly credit card statement and needs to import it into ExpenseFlow, properly map the columns, and manage the resulting transactions including filtering, searching, and categorizing.

**Why this priority**: Statement import is the second major data source. Incorrect column mapping causes widespread data issues, making clear documentation essential.

**Independent Test**: Can be fully tested by importing a CSV statement file, completing column mapping, and using filters to find specific transactions.

**Acceptance Scenarios**:

1. **Given** a CSV bank statement, **When** the user follows the Statement Import section, **Then** they successfully complete the 3-step import wizard (Upload, Map, Complete)
2. **Given** the column mapping step, **When** the user reads the mapping guidance, **Then** they correctly identify Date, Amount, and Description columns and configure the amount sign convention
3. **Given** a completed column mapping, **When** the user reads the Fingerprint section, **Then** they understand how to save the mapping as a reusable template for future imports from the same bank
4. **Given** a previously saved fingerprint, **When** the user imports a new statement from the same bank, **Then** they can select the saved template to auto-apply column mappings
5. **Given** multiple saved fingerprints, **When** the user reads the template management guidance, **Then** they can view, rename, or delete saved fingerprints from Settings
6. **Given** imported transactions, **When** the user reads the Transaction Management section, **Then** they can filter by date range, category, or search terms to locate specific expenses
7. **Given** multiple transactions, **When** the user follows bulk operation instructions, **Then** they can categorize or tag multiple transactions simultaneously

---

### User Story 4 - Receipt-Transaction Matching (Priority: P2)

A user with both receipts and imported transactions needs to understand how the AI matching engine works, review proposed matches, confirm or reject suggestions, manually create matches when automatic matching fails, and understand how their actions improve matching accuracy over time.

**Why this priority**: Matching is where receipt data connects to transaction data. Understanding confidence thresholds, matching factors, and how feedback trains the AI enables users to make informed decisions and improve system accuracy.

**Independent Test**: Can be fully tested by reviewing 10 proposed matches, confirming high-confidence matches, rejecting incorrect ones, manually matching one unmatched pair, and understanding what actions improve future matching.

**Acceptance Scenarios**:

1. **Given** proposed matches exist, **When** the user reads the Match Review section, **Then** they understand how to interpret confidence scores and matching factors
2. **Given** a high-confidence match (90%+), **When** the user follows confirmation instructions, **Then** they can approve using keyboard shortcuts or buttons
3. **Given** an incorrect match proposal, **When** the user reads the rejection guidance, **Then** they can reject and understand the proposal will not reappear
4. **Given** an unmatched receipt and transaction, **When** the user follows manual matching instructions, **Then** they can create a link between them manually
5. **Given** many pending matches, **When** the user reads the batch approval section, **Then** they can set a confidence threshold and approve all matches above it
6. **Given** the "Improving Match Accuracy" section, **When** the user reads it, **Then** they understand that confirming/rejecting matches trains the AI to make better future suggestions
7. **Given** tips for better matching, **When** the user reads the best practices guidance, **Then** they understand which receipt fields (merchant name, exact amount) most improve matching accuracy
8. **Given** the matching factors breakdown, **When** the user reads the explanation, **Then** they understand why a match was proposed and what factors contributed to the confidence score

---

### User Story 5 - Expense Report Generation and Export (Priority: P2)

A user at month-end needs to generate an expense report from their matched expenses, review the draft, make corrections, and export it in their company's required format (PDF or Excel) for submission.

**Why this priority**: Report generation is the output of the expense workflow. Users need to understand report states and export options for successful submission.

**Independent Test**: Can be fully tested by generating a monthly report, reviewing the draft, and exporting to both PDF and Excel formats.

**Acceptance Scenarios**:

1. **Given** matched expenses exist, **When** the user reads the Report Generation section, **Then** they can create a new monthly expense report through the dialog
2. **Given** a draft report, **When** the user follows the review instructions, **Then** they understand how to view included items and add notes
3. **Given** report status indicators, **When** the user reads the status definitions, **Then** they understand Draft, Submitted, Approved, and Rejected states
4. **Given** an approved report, **When** the user follows export instructions, **Then** they can download the report as PDF or Excel

---

### User Story 6 - Spending Analytics and Insights (Priority: P2)

A user wants to understand their spending patterns, identify trends, compare periods, and discover recurring subscriptions to better manage their expenses.

**Why this priority**: Analytics provide value beyond basic expense tracking. Users who understand these features can proactively manage spending.

**Independent Test**: Can be fully tested by accessing analytics, selecting date ranges, comparing two periods, and identifying at least one spending trend or subscription.

**Acceptance Scenarios**:

1. **Given** historical expense data, **When** the user reads the Analytics Overview section, **Then** they can navigate to the analytics dashboard and select date ranges
2. **Given** the analytics dashboard, **When** the user follows the comparison instructions, **Then** they can compare spending between two time periods and interpret the percentage change
3. **Given** the category breakdown view, **When** the user reads the chart interpretation guide, **Then** they understand how to read pie/bar charts and identify top spending categories
4. **Given** the subscriptions tab, **When** the user reads the subscription detection section, **Then** they understand confidence levels and can identify recurring charges

---

### User Story 7 - Expense Splitting (Priority: P2)

A user has a single expense (e.g., a project dinner or shared equipment purchase) that needs to be allocated across multiple departments or cost centers. They need to understand how to split expenses and save reusable split patterns.

**Why this priority**: Expense splitting is essential for accurate cost allocation in organizations with multiple projects or departments. Without understanding this feature, users submit incorrectly allocated expenses.

**Independent Test**: Can be fully tested by splitting one expense across two departments, saving the split as a pattern, and reusing that pattern on another expense.

**Acceptance Scenarios**:

1. **Given** an expense needing allocation, **When** the user reads the Expense Splitting section, **Then** they understand how to divide the expense across multiple cost centers with percentage allocations
2. **Given** a frequently used split (e.g., 50/50 between two projects), **When** the user reads the Split Patterns section, **Then** they can save the allocation as a reusable pattern with a descriptive name
3. **Given** a saved split pattern, **When** the user reads the pattern application guidance, **Then** they can apply the pattern to new expenses with one click
4. **Given** an incorrect split, **When** the user reads the split management guidance, **Then** they can remove or modify the split allocation

---

### User Story 8 - Subscription Alerts and Management (Priority: P2)

A user wants to monitor recurring charges, receive alerts for missing or unexpected subscriptions, and manage their subscription tracking to avoid unnecessary charges.

**Why this priority**: Subscription management is a proactive cost-saving feature. Users who understand alerts can catch billing errors or forgotten services.

**Independent Test**: Can be fully tested by viewing detected subscriptions, acknowledging an alert, and creating a manual subscription entry.

**Acceptance Scenarios**:

1. **Given** the Subscriptions tab in Analytics, **When** the user reads the Subscription Detection section, **Then** they understand how the system automatically identifies recurring charges with confidence levels
2. **Given** subscription alerts exist, **When** the user reads the Alert Management section, **Then** they understand what triggers alerts (missing expected charge, unexpected new charge) and how to acknowledge them
3. **Given** a recurring charge not detected automatically, **When** the user reads the manual subscription guidance, **Then** they can create a manual subscription entry with expected frequency and amount
4. **Given** the subscription summary, **When** the user reads the monitoring guidance, **Then** they understand estimated monthly/annual costs and next expected charge predictions

---

### User Story 9 - Travel Period Organization (Priority: P3)

A user wants to organize expenses from a business trip into a cohesive travel period, linking related receipts and transactions for easier reporting and review.

**Why this priority**: Travel period organization improves expense report clarity but is optional. Users can submit expenses without grouping them by trip.

**Independent Test**: Can be fully tested by creating a travel period, viewing linked expenses, and understanding the timeline view.

**Acceptance Scenarios**:

1. **Given** expenses from a business trip, **When** the user reads the Travel Periods section, **Then** they understand how to create a travel period with start/end dates and a description
2. **Given** the system has detected a potential trip, **When** the user reads the auto-detection guidance, **Then** they understand how receipts from hotels/airlines trigger travel detection suggestions
3. **Given** a created travel period, **When** the user reads the timeline view section, **Then** they understand how to view all linked expenses, daily summaries, and period totals
4. **Given** the travel period list, **When** the user reads the management guidance, **Then** they can edit, delete, or filter travel periods by date range

---

### User Story 10 - Dashboard Understanding (Priority: P2)

A user wants to understand their dashboard, including the action queue that shows prioritized pending tasks and the activity feed that displays recent expense events, so they can efficiently manage their daily workflow.

**Why this priority**: The dashboard is the first thing users see after login. Understanding action priorities and activity context helps users focus on what matters most.

**Independent Test**: Can be fully tested by viewing the dashboard, understanding action queue priorities, and interpreting activity feed events.

**Acceptance Scenarios**:

1. **Given** the dashboard, **When** the user reads the Action Queue section, **Then** they understand priority levels (high/medium/low) and action types (review match, correct extraction, approve report, categorize)
2. **Given** action queue items, **When** the user reads the priority guidance, **Then** they understand which items need immediate attention vs. can wait
3. **Given** the Activity Feed (Expense Stream), **When** the user reads the event interpretation section, **Then** they understand event types (receipt uploaded, match proposed, report generated) and their status indicators
4. **Given** the metrics row, **When** the user reads the metrics explanation, **Then** they understand pending counts, monthly spending, and month-over-month comparisons

---

### User Story 11 - Keyboard Shortcuts and Power User Features (Priority: P3)

A power user processing many receipts and matches wants to work efficiently using keyboard shortcuts and understand advanced controls like image manipulation and undo/redo functionality.

**Why this priority**: Keyboard shortcuts significantly speed up workflows but are discoverable through the UI. Users can function without them but benefit greatly from learning them.

**Independent Test**: Can be fully tested by navigating match review using only keyboard and using image viewer controls on a receipt.

**Acceptance Scenarios**:

1. **Given** the match review workspace, **When** the user reads the Keyboard Shortcuts section, **Then** they understand A=approve, R=reject, J/K or arrows for navigation, M=manual match, Escape=close help
2. **Given** a receipt image, **When** the user reads the Image Viewer Controls section, **Then** they can zoom in/out, rotate, pan, and use fullscreen mode
3. **Given** an AI-extracted field being edited, **When** the user reads the Undo/Redo section, **Then** they understand how to undo corrections (up to 10 steps) and clear edit history
4. **Given** the shortcuts help panel, **When** the user reads the access instructions, **Then** they know how to open the keyboard shortcuts reference within the app

---

### User Story 12 - Categorization Confirmation (Priority: P3)

A user receives AI-suggested GL codes and department assignments for transactions and needs to understand how to confirm, modify, or skip these suggestions to ensure accurate expense allocation.

**Why this priority**: Categorization is semi-automated. Users who understand suggestions can confirm quickly; those who don't may accept incorrect allocations.

**Independent Test**: Can be fully tested by reviewing a transaction with AI suggestions, confirming one, modifying another, and skipping a third.

**Acceptance Scenarios**:

1. **Given** a transaction with AI suggestions, **When** the user reads the GL Code Suggestions section, **Then** they understand how the system proposes GL codes based on vendor patterns and spending history
2. **Given** multiple GL code options, **When** the user reads the selection guidance, **Then** they can compare suggested codes with confidence scores and select the appropriate one
3. **Given** an unclear transaction, **When** the user reads the Skip workflow section, **Then** they understand how to skip categorization for later review without blocking other work
4. **Given** a confirmed categorization, **When** the user reads the learning section, **Then** they understand that confirmations improve future suggestions for similar transactions

---

### User Story 13 - Troubleshooting and Recovery (Priority: P3)

A user encounters issues such as failed receipt uploads, processing errors, or unexpected system behavior and needs to understand how to resolve problems independently.

**Why this priority**: Self-service troubleshooting reduces support burden and user frustration, but core workflows should work without needing this section.

**Independent Test**: Can be fully tested by intentionally uploading a corrupted file, following troubleshooting steps, and successfully retrying with a corrected file.

**Acceptance Scenarios**:

1. **Given** a failed receipt upload, **When** the user reads the Upload Troubleshooting section, **Then** they understand common causes (file size, format, network) and how to retry
2. **Given** a receipt stuck in "Processing" status, **When** the user reads the Processing Issues section, **Then** they understand how to trigger a reprocess and when to wait vs. take action
3. **Given** zero AI extraction confidence, **When** the user reads the Extraction Failures section, **Then** they understand how to manually enter receipt data and when to re-upload a clearer image
4. **Given** an accidentally rejected match, **When** the user reads the Recovery section, **Then** they understand that manual matching can link the correct receipt and transaction

---

### User Story 14 - Settings and Personalization (Priority: P3)

A user wants to customize their ExpenseFlow experience by setting defaults, managing custom categories, and configuring appearance preferences.

**Why this priority**: Settings customization improves efficiency but is not required for basic functionality. Users can be productive without changing defaults.

**Independent Test**: Can be fully tested by changing theme, setting default department/project, and creating a custom expense category.

**Acceptance Scenarios**:

1. **Given** the settings page, **When** the user reads the Appearance section, **Then** they can switch between Light, Dark, and System themes
2. **Given** the default selections section, **When** the user reads the preferences guide, **Then** they can set a default department and project for new expenses
3. **Given** the categories section, **When** the user follows category management instructions, **Then** they can create, edit, activate/deactivate, and delete custom categories

---

### User Story 15 - Mobile Usage (Priority: P3)

A user needs to capture receipts while traveling using their mobile device, understanding the mobile-specific interface elements, swipe gestures, and photo capture workflow.

**Why this priority**: Mobile access extends functionality but the core workflow can be completed on desktop. Mobile-specific guidance enhances the experience.

**Independent Test**: Can be fully tested by accessing ExpenseFlow on a mobile device, capturing a receipt photo, using swipe actions, and navigating using the bottom navigation bar.

**Acceptance Scenarios**:

1. **Given** mobile device access, **When** the user reads the Mobile Usage section, **Then** they understand the bottom navigation bar and its pending count badges
2. **Given** the receipts page on mobile, **When** the user follows photo capture instructions, **Then** they can use their camera to capture and upload a receipt
3. **Given** the mobile dashboard, **When** the user reads the mobile layout guide, **Then** they understand the summary bar and how to access detailed views
4. **Given** a transaction or receipt row on mobile, **When** the user reads the Swipe Actions section, **Then** they understand left-swipe reveals delete/reject and right-swipe reveals edit/approve actions
5. **Given** the upload queue on mobile, **When** the user reads the progress tracking section, **Then** they understand per-file status indicators and how to retry failed uploads

---

### Edge Cases

- What happens when a user uploads an unsupported file type (e.g., .doc instead of image/PDF)?
- How does the system handle duplicate receipt uploads?
- What occurs when statement import encounters duplicate transactions already in the system?
- How does matching behave when multiple receipts could match a single transaction?
- What happens when a user's session expires during a multi-step workflow?
- How should users handle receipts with multiple items that need split allocation?
- What if AI extraction returns zero confidence for all fields?
- How do users recover from accidentally rejecting a correct match proposal?
- What happens when split allocations don't sum to exactly 100%?
- How does the system handle a subscription that changes amount month-to-month?
- What if a travel period overlaps with another existing travel period?
- What happens when a user tries to reprocess a receipt that's already matched?
- How should users handle receipts from foreign currencies during travel?
- What happens when keyboard shortcuts are pressed while typing in a form field?
- How does the system behave when the user loses network connectivity mid-upload?
- What if the AI suggests the same GL code for two very different transaction types?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Documentation MUST provide step-by-step instructions for first-time user onboarding including sign-in and initial navigation
- **FR-002**: Documentation MUST explain all receipt upload methods (drag-and-drop, file browser, mobile camera capture)
- **FR-003**: Documentation MUST describe AI extraction results including confidence score interpretation (color-coded: green 90%+, amber 70-89%, red below 70%)
- **FR-004**: Documentation MUST provide guidance on editing AI-extracted fields with inline editing controls
- **FR-005**: Documentation MUST explain the complete 3-step statement import wizard (Upload, Column Mapping, Completion)
- **FR-006**: Documentation MUST describe column mapping with visual examples showing sample data preview
- **FR-007**: Documentation MUST explain amount sign configuration (negative vs positive for charges)
- **FR-008**: Documentation MUST cover fingerprint/template creation for repeated statement imports, including saving, selecting, and managing saved templates
- **FR-009**: Documentation MUST explain transaction filtering including all filter types (date range, category, amount, match status, tags)
- **FR-010**: Documentation MUST describe bulk operations (categorize, tag, export, delete) with selection methods
- **FR-011**: Documentation MUST explain match confidence scores and matching factors breakdown
- **FR-012**: Documentation MUST describe both Review Mode (keyboard-driven split-pane) and List Mode for match review
- **FR-013**: Documentation MUST explain batch approval using confidence threshold slider
- **FR-014**: Documentation MUST describe manual matching workflow for unmatched pairs
- **FR-015**: Documentation MUST explain how user confirmations and rejections train the AI to improve future matching accuracy
- **FR-016**: Documentation MUST provide best practices for improving match accuracy (which receipt fields matter most, timing of imports)
- **FR-017**: Documentation MUST explain expense report creation including month selection and notes
- **FR-018**: Documentation MUST describe report status workflow (Draft, Submitted, Approved, Rejected)
- **FR-019**: Documentation MUST explain export options (PDF, Excel) with use cases for each format
- **FR-020**: Documentation MUST describe all analytics dashboard components (summary metrics, trend charts, category breakdown, merchant analysis, subscriptions)
- **FR-021**: Documentation MUST explain date range selection and comparison mode functionality
- **FR-022**: Documentation MUST describe subscription detection including confidence levels
- **FR-023**: Documentation MUST explain all settings sections (theme, default selections, custom categories)
- **FR-024**: Documentation MUST describe mobile-specific interface elements (bottom navigation, summary bar, photo capture)
- **FR-025**: Documentation MUST include a comprehensive glossary defining all system terms (receipt, transaction, match, fingerprint, confidence score, split pattern, subscription alert, travel period, etc.)
- **FR-026**: Documentation MUST provide troubleshooting guidance for common issues (upload failures, import errors, matching problems)
- **FR-027**: Documentation MUST include annotated screenshots (with numbered callouts, arrows, and highlights) for complex workflows
- **FR-028**: Documentation MUST explain expense splitting including percentage allocations, split patterns, and pattern reuse
- **FR-029**: Documentation MUST describe subscription alert management including alert triggers, acknowledgment, and manual subscription creation
- **FR-030**: Documentation MUST explain travel period creation, auto-detection from receipts, timeline view, and expense linking
- **FR-031**: Documentation MUST describe receipt retry and reprocessing for failed uploads and stuck processing states
- **FR-032**: Documentation MUST explain dashboard components including action queue priorities (high/medium/low), activity feed event types, and metrics interpretation
- **FR-033**: Documentation MUST provide a keyboard shortcuts reference including match review shortcuts (A/R/J/K/M/Escape) and general navigation
- **FR-034**: Documentation MUST explain receipt image viewer controls including zoom in/out, rotate, pan, and fullscreen mode
- **FR-035**: Documentation MUST describe undo/redo functionality for AI-extracted field corrections (up to 10 steps)
- **FR-036**: Documentation MUST explain GL code and department suggestion workflow including confidence scores, selection, and skip options
- **FR-037**: Documentation MUST describe mobile swipe actions (left-swipe for delete/reject, right-swipe for edit/approve)
- **FR-038**: Documentation MUST explain transaction sorting options (date, amount, merchant, category) and sort direction toggles
- **FR-039**: Documentation MUST describe upload queue progress tracking including per-file status, retry, and cancel operations

### Key Entities

- **User Guide**: A documentation artifact organized as sequential workflow guides (Getting Started → Daily Use → Monthly Close) that mirror the user adoption journey
- **Workflow Guide**: A sequential section covering a phase of user adoption with numbered steps and decision points
- **Glossary Term**: A definition entry explaining ExpenseFlow-specific terminology
- **Troubleshooting Entry**: A problem-solution pair addressing common user issues
- **Quick Reference**: A condensed summary of keyboard shortcuts, status indicators, and common actions

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: New users can complete the first-time experience (sign-in through first receipt upload) within 15 minutes using only the documentation
- **SC-002**: Users can locate information about any specific feature within 60 seconds using the table of contents or search
- **SC-003**: 90% of common support questions can be answered by referencing documentation sections
- **SC-004**: Users can complete the statement import wizard without errors on first attempt after reading the guide
- **SC-005**: Documentation covers 100% of user-accessible interface elements with descriptions and purpose explanations
- **SC-006**: All complex workflows (multi-step processes) include visual aids or step indicators
- **SC-007**: Troubleshooting section addresses at least 10 common user issues with resolution steps
- **SC-008**: Mobile-specific guidance enables users to capture and upload receipts using phone camera within 3 minutes
- **SC-009**: Glossary includes definitions for all system-specific terminology (minimum 20 terms)
- **SC-010**: Each major section includes a "Quick Start" summary for experienced users who need reminders

## Assumptions

- Users have basic computer literacy and web browser familiarity
- Users access ExpenseFlow through a modern web browser (Chrome, Firefox, Safari, Edge)
- Users authenticate using their company Microsoft Entra ID credentials (single sign-on)
- Users have authorized access granted by their organization
- Documentation will be delivered as Markdown files in the repository, rendered by GitHub or a docs viewer
- Screenshots and visual aids will be updated when significant UI changes occur
- The guide focuses on end-user workflows, not administrative or deployment tasks

## Out of Scope

- System administration and user provisioning
- API integration documentation
- Developer setup and contribution guides
- Infrastructure deployment procedures
- Database schema or technical architecture
- Viewpoint Vista ERP configuration
- Troubleshooting server-side or infrastructure issues
