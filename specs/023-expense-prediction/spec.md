# Feature Specification: Expense Prediction from Historical Reports

**Feature Branch**: `023-expense-prediction`
**Created**: 2026-01-02
**Status**: Draft
**Input**: User description: "Use previous reports to determine which transactions are likely expenses"

## Overview

ExpenseFlow currently categorizes transactions using a three-tier approach (vendor alias matching, embedding similarity, AI inference), but it doesn't leverage the most valuable signal: the user's own historical expense report decisions. When a user has previously submitted an expense report that included a specific vendor or transaction pattern, the system should learn from these decisions to automatically suggest that similar future transactions are likely business expenses.

This feature introduces a learning mechanism that analyzes approved expense reports to build a personalized expense prediction model for each user. When new transactions are imported, the system will use this historical knowledge to proactively identify which transactions are likely business expenses, reducing manual review effort and improving categorization accuracy.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Automatic Expense Badge on Transaction List (Priority: P1)

When a user views their transaction list, transactions that match patterns from their previously approved expense reports are automatically highlighted with a "Likely Expense" badge. This immediate visual feedback helps users quickly identify which transactions they should include in their next expense report.

**Why this priority**: This is the core value proposition - users see immediately which transactions match their historical expense patterns without taking any action. This reduces cognitive load and speeds up expense report creation.

**Independent Test**: Can be fully tested by importing transactions after having at least one approved expense report. The badge appears on matching transactions and delivers immediate value by highlighting likely expenses.

**Acceptance Scenarios**:

1. **Given** a user has an approved expense report containing a Starbucks transaction, **When** a new Starbucks transaction is imported, **Then** the transaction displays a "Likely Expense" badge in the transaction list
2. **Given** a user has no approved expense reports, **When** transactions are imported, **Then** no expense prediction badges are displayed (graceful degradation)
3. **Given** a user has approved reports with specific merchants, **When** viewing the transaction list, **Then** the system shows a prediction confidence level (High/Medium) based on match quality; Low confidence predictions are not displayed

---

### User Story 2 - Smart Expense Report Pre-Population (Priority: P2)

When generating a new expense report draft, the system pre-selects transactions that match historical expense patterns. Users can quickly review and confirm the suggestions rather than manually selecting each transaction from scratch.

**Why this priority**: Builds on User Story 1 by automating the selection process. This dramatically reduces time spent creating expense reports for users with consistent expense patterns.

**Independent Test**: Can be fully tested by generating a new draft report when predicted expenses exist. The draft includes pre-selected transactions based on historical patterns.

**Acceptance Scenarios**:

1. **Given** a user has historical expense patterns, **When** generating a new expense report draft, **Then** matching transactions are pre-selected with an "Auto-suggested" indicator
2. **Given** pre-selected transactions in a draft, **When** the user reviews them, **Then** they can easily remove any incorrectly suggested transactions with a single click
3. **Given** the draft includes both auto-suggested and manually added transactions, **When** viewing the report summary, **Then** the system shows a breakdown of auto-suggested vs manual selections

---

### User Story 3 - Pattern Learning Feedback Loop (Priority: P3)

When a user removes an auto-suggested transaction from a report (indicating a false positive), or adds a transaction that wasn't suggested (indicating a missed pattern), the system learns from this feedback to improve future predictions.

**Why this priority**: Continuous improvement is important but the base prediction (P1, P2) must work well first. This story ensures the system gets smarter over time.

**Independent Test**: Can be fully tested by submitting a report that differs from suggestions, then importing new similar transactions to verify improved predictions.

**Acceptance Scenarios**:

1. **Given** a user removes an auto-suggested Starbucks transaction from their report, **When** the next Starbucks transaction is imported, **Then** the prediction confidence is reduced (but not eliminated - may be legitimate business coffee)
2. **Given** a user manually adds a new vendor that was never expensed before, **When** the report is approved, **Then** future transactions from that vendor receive elevated prediction scores
3. **Given** multiple feedback signals over time, **When** viewing the analytics dashboard, **Then** users can see their prediction accuracy improving

---

### User Story 4 - Expense Pattern Dashboard (Priority: P4)

Users can view a summary of their learned expense patterns, showing which merchants and categories the system has identified as typical business expenses based on their history.

**Why this priority**: Transparency about what the system has learned. Nice-to-have but not essential for core functionality.

**Independent Test**: Can be fully tested by navigating to the pattern dashboard after having approved expense reports.

**Acceptance Scenarios**:

1. **Given** a user has approved expense reports, **When** viewing the expense patterns dashboard, **Then** they see a list of recognized expense vendors with frequency and average amounts
2. **Given** the patterns dashboard, **When** a user identifies an incorrect pattern, **Then** they can manually exclude a vendor from expense predictions

---

### Edge Cases

- What happens when a user has no approved expense reports? → Graceful degradation: no predictions shown, no badges, standard manual workflow
- What happens when a transaction matches multiple patterns with different confidence levels? → Use the highest confidence match and show the reasoning
- How does the system handle seasonal expenses (e.g., annual conference)? → **Deferred to future sprint**. Initial implementation uses vendor-only matching. Annual pattern detection (same vendor within ±30 days across 2+ consecutive years) will be considered in a follow-up enhancement.
- What happens when a transaction amount is significantly different from historical amounts for the same vendor? → Flag with lower confidence and note the amount variance
- How does the system handle shared vendors (e.g., Amazon for both personal and business)? → Use additional signals like amount ranges, day of week, and description keywords to differentiate

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST analyze approved expense reports to extract expense patterns (vendor, category, amount ranges, frequency)
- **FR-002**: System MUST store learned expense patterns per user account with appropriate privacy boundaries
- **FR-003**: System MUST apply learned patterns to newly imported transactions to generate expense predictions
- **FR-004**: System MUST display expense prediction badges on transactions in the transaction list view
- **FR-005**: System MUST show prediction confidence levels (High/Medium) based on pattern match quality; Low confidence predictions are suppressed from display
- **FR-006**: System MUST pre-select predicted expense transactions when generating new report drafts
- **FR-007**: System MUST allow users to override predictions by removing or adding transactions
- **FR-008**: System MUST learn from user overrides to improve future predictions
- **FR-009**: System MUST respect user privacy by keeping expense patterns isolated per user account
- **FR-010**: System MUST provide a way for users to view and manage their learned expense patterns
- **FR-011**: System MUST handle the cold-start scenario (no historical reports) gracefully without errors
- **FR-012**: System MUST recalculate predictions when new expense reports are approved
- **FR-013**: System MUST weight recent patterns more heavily than older patterns using exponential decay (recent reports weighted 2x more than older ones)
- **FR-014**: System MUST provide explicit confirm/reject buttons on each predicted expense transaction to capture user feedback
- **FR-015**: System MUST track prediction accuracy metrics (confirmed, rejected, ignored) for observability and model improvement

### Key Entities

- **ExpensePattern**: Represents a learned expense pattern for a user, including vendor name, category, typical amount range, frequency, and confidence score. Linked to the user account and derived from approved ExpenseReport data.
- **TransactionPrediction**: Represents a prediction that a specific transaction is a business expense, including the confidence score, matching pattern reference, and whether the user confirmed or rejected the prediction.
- **PredictionFeedback**: Captures user feedback signals (confirmations, rejections, manual additions) used to improve the prediction model over time.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users with at least 3 approved expense reports see prediction badges on 70%+ of transactions that match previously expensed vendors
- **SC-002**: Draft expense report generation time reduced by 50% for users with historical patterns (measuring time from "Generate Draft" to "Submit")
- **SC-003**: Prediction accuracy reaches 85%+ after 5 expense report cycles (measuring confirmed vs total predictions)
- **SC-004**: False positive rate remains below 15% (transactions incorrectly flagged as expenses that user removes)
- **SC-005**: System processes pattern matching for 1000 transactions in under 5 seconds
- **SC-006**: Users can view their expense patterns dashboard within 2 seconds of navigation
- **SC-007**: 80% of users with sufficient history report that expense predictions are helpful (based on in-app feedback)

## Clarifications

### Session 2026-01-02

- Q: How should pattern recency be weighted over time? → A: Exponential decay with recent reports weighted 2x more than older ones
- Q: How should prediction accuracy be tracked and surfaced? → A: Per-transaction feedback with explicit confirm/reject buttons on each prediction
- Q: What minimum confidence threshold should predictions meet before being displayed? → A: Only Medium and High confidence predictions are shown (Low confidence suppressed)

## Assumptions

- Users submit expense reports on a regular basis (monthly or more frequently)
- Approved expense reports represent ground truth for what constitutes a business expense
- Vendor names can be reliably matched across transactions using existing vendor alias normalization
- The existing three-tier categorization system provides a foundation that expense predictions can enhance
- Users are willing to provide implicit feedback through their report editing actions

## Out of Scope

- Cross-user pattern learning (learning from other users' expense patterns)
- Integration with external expense policy systems
- Automatic expense report submission (predictions inform but don't automate final submission)
- Expense policy compliance checking (that's a separate concern)
- Retroactive re-categorization of already-submitted reports

## Dependencies

- Existing ExpenseReport and ExpenseLine entities must be available for pattern extraction
- Existing VendorAlias system provides vendor name normalization
- Transaction import pipeline must support attaching prediction metadata
- UI components for badges and indicators in transaction list
