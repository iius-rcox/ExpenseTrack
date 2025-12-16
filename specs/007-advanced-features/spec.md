# Feature Specification: Advanced Features

**Feature Branch**: `007-advanced-features`
**Created**: 2025-12-16
**Status**: Draft
**Input**: User description: "Sprint 7: Advanced Features - Travel detection, subscription identification, and expense splitting—all optimized for cost using rule-based detection with AI fallback only for complex scenarios"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Travel Period Detection (Priority: P1)

As an expense report user, I want the system to automatically detect when I'm on a business trip based on my flight and hotel receipts, so that related expenses during that period are automatically flagged as travel expenses (GL code 66300) without manual intervention.

**Why this priority**: Travel expenses are the most common category requiring special handling. Automatic detection eliminates the tedious task of manually identifying and tagging each expense made during a trip, reducing processing time significantly.

**Independent Test**: Upload a flight receipt and hotel receipt for the same destination. Verify the system creates a travel period spanning those dates and automatically suggests GL code 66300 for any expenses (meals, transportation) that occur within that date range.

**Acceptance Scenarios**:

1. **Given** a user uploads a flight receipt with vendor "Delta Airlines" and travel date January 10, **When** the system processes the receipt, **Then** a travel period is created starting on January 10 with source "Flight"

2. **Given** a travel period exists starting January 10, **When** the user uploads a hotel receipt for "Marriott" with check-in January 10 and 3 nights, **Then** the travel period is extended to end January 13

3. **Given** a travel period exists from January 10-13, **When** a transaction for "Starbucks" dated January 11 is processed, **Then** the system suggests GL code 66300 (Travel) and indicates "Within travel period"

4. **Given** a travel period exists from January 10-13, **When** a transaction dated January 15 is processed, **Then** no travel-related suggestions are made for that transaction

5. **Given** a complex itinerary with multi-city flights and multiple hotel stays, **When** rule-based detection cannot determine the travel period, **Then** the item is flagged for AI-assisted review

---

### User Story 2 - Subscription Detection (Priority: P2)

As an expense report user, I want the system to automatically identify recurring subscription charges (like software licenses, cloud services, and professional memberships), so that I can ensure these are included each month and apply consistent categorization.

**Why this priority**: Subscriptions often get missed or categorized inconsistently. Automatic detection ensures completeness and consistency month over month, and alerts users when expected subscriptions don't appear.

**Independent Test**: Import 3 months of credit card statements containing charges from "OpenAI" around the same amount each month. Verify the system identifies this as a subscription and flags it in future months.

**Acceptance Scenarios**:

1. **Given** transactions from the same vendor with similar amounts for 2+ consecutive months, **When** the subscription detection runs, **Then** the vendor is identified as a subscription

2. **Given** a vendor is identified as a subscription, **When** the user views detected subscriptions, **Then** they see the vendor name, average monthly amount, and months of occurrence

3. **Given** a known subscription vendor (from seed data like "Claude.AI", "OpenAI", "Cursor"), **When** a transaction matches that vendor, **Then** it is immediately flagged as a subscription without waiting for pattern detection

4. **Given** a subscription was detected in previous months, **When** it does not appear in the current month, **Then** the system alerts the user about the missing expected charge

5. **Given** a subscription with an amount that varies by more than $5 from the average, **When** the charge appears, **Then** the system flags it for review as an unusual subscription amount

---

### User Story 3 - Expense Splitting (Priority: P2)

As an expense report user, I want to split a single expense across multiple GL codes or departments, so that shared costs like office supplies or team meals can be properly allocated according to company policy.

**Why this priority**: Many business expenses need to be split across cost centers or categories. Manual splitting is error-prone and time-consuming. Pattern-based suggestions eliminate repetitive work for recurring split scenarios.

**Independent Test**: Create an expense for "Amazon" purchase of $500, split it 60% to GL 64100 (Office Supplies) for Department 07 and 40% to GL 65100 (Computer Equipment) for Department 12. Confirm the next "Amazon" expense of similar amount suggests the same split pattern.

**Acceptance Scenarios**:

1. **Given** a user wants to split an expense, **When** they access the split function, **Then** they can allocate percentages or fixed amounts to multiple GL code/department combinations

2. **Given** an expense of $100 is split 70%/30% across two GL codes, **When** the split is saved, **Then** two expense lines are created for $70 and $30 respectively

3. **Given** a user confirms a split for vendor "Amazon", **When** the split pattern is saved, **Then** future Amazon expenses suggest the same split allocation

4. **Given** a split pattern exists for a vendor, **When** a new expense from that vendor is created, **Then** the system suggests applying the saved split pattern (Tier 1, no AI)

5. **Given** all split allocations total 100%, **When** the split is saved, **Then** the expense is marked as fully allocated

6. **Given** split allocations total more or less than 100%, **When** the user attempts to save, **Then** the system prevents saving and displays a validation error

---

### User Story 4 - Travel Timeline Visualization (Priority: P3)

As an expense report user, I want to see a visual timeline of my detected travel periods alongside related expenses, so that I can quickly verify all trip expenses are captured and properly categorized.

**Why this priority**: Visual confirmation helps users catch missing expenses and validate automatic categorization. While valuable, this is an enhancement to the core detection functionality.

**Independent Test**: After uploading travel receipts that create a travel period, navigate to the travel timeline view and verify it shows the trip dates, destination, source documents, and all expenses linked to that period.

**Acceptance Scenarios**:

1. **Given** one or more travel periods exist for a user, **When** they view the travel timeline, **Then** they see a visual representation with start/end dates and destination

2. **Given** a travel period is displayed, **When** the user clicks on it, **Then** they see all expenses linked to that period with their categories

3. **Given** the timeline is displayed, **When** expenses exist within a travel period that aren't linked to travel, **Then** they are highlighted for user review

---

### Edge Cases

- What happens when a flight receipt cannot be parsed (airline not recognized)? System should flag for manual review and allow user to create travel period manually.
- How does the system handle overlapping travel periods (return from trip A same day as departure for trip B)? System should merge adjacent periods or keep separate based on destination.
- What happens when a subscription amount changes significantly (e.g., annual billing vs monthly)? System should flag unusual amount but not lose subscription tracking.
- How does the system handle partial expense splits that don't equal 100%? Validation prevents saving until allocation is complete.
- What if the same vendor has both subscription and non-subscription purchases (e.g., one-time vs recurring Amazon purchases)? System tracks subscription pattern separately; transactions within ±20% of average subscription amount are matched to subscription, outside that range treated as one-time purchases.

## Requirements *(mandatory)*

### Functional Requirements

**Travel Detection**:
- **FR-001**: System MUST detect travel periods from flight receipts by identifying airline vendors (Delta, United, American, Southwest, Alaska, JetBlue, and international carriers)
- **FR-002**: System MUST detect hotel stays from lodging receipts by identifying hotel vendors (Marriott, Hilton, Hyatt, IHG, Airbnb, VRBO, and similar)
- **FR-003**: System MUST create travel period records with start date, end date, destination (flight arrival city primary, hotel location as fallback), and source (Flight/Hotel)
- **FR-004**: System MUST extend existing travel periods when hotel check-out extends beyond current end date
- **FR-005**: System MUST automatically suggest GL code 66300 for expenses occurring within detected travel periods
- **FR-006**: System MUST flag complex itineraries for AI-assisted review when rule-based detection is insufficient
- **FR-007**: System MUST allow users to manually create, edit, or delete travel periods

**Subscription Detection**:
- **FR-008**: System MUST identify subscriptions as recurring charges from the same vendor with similar amounts (variance < $5) appearing in 2+ consecutive months
- **FR-009**: System MUST maintain a list of known subscription vendors (seed data) for immediate recognition
- **FR-010**: System MUST track detected subscriptions per user with vendor name, average amount, and occurrence history
- **FR-011**: System MUST alert users when previously detected subscriptions do not appear by calendar month end
- **FR-012**: System MUST flag subscriptions with unusual amount variations for user review

**Expense Splitting**:
- **FR-013**: System MUST allow users to split a single expense across multiple GL codes and departments
- **FR-014**: System MUST support allocation by percentage or fixed amount
- **FR-015**: System MUST validate that split allocations total exactly 100%
- **FR-016**: System MUST store split patterns associated with vendor aliases for future suggestions
- **FR-017**: System MUST suggest previously used split patterns when new expenses from the same vendor are processed (Tier 1 suggestion), pre-filling the split form for user modification before saving
- **FR-018**: System MUST track split pattern usage count for optimization insights

**General**:
- **FR-019**: System MUST use rule-based detection (Tier 1) as the primary method, reserving AI (Tier 4) only for edge cases
- **FR-020**: System MUST log tier usage for all detection operations for cost monitoring

### Key Entities

- **TravelPeriod**: Represents a business trip with start_date, end_date, destination, source (Flight/Hotel), user_id, and AI review flag
- **DetectedSubscription**: Tracks recurring charges with vendor_id, user_id, average_amount, occurrence_months, last_seen_date, and status (Active/Missing/Flagged)
- **SplitPattern**: Stores learned expense allocations with user_id (user-scoped), vendor_alias_id, split_config (array of GL/department/percentage allocations), usage_count, and last_used_date
- **KnownSubscriptionVendor**: Seed data table with vendor patterns known to be subscriptions (Claude.AI, OpenAI, Cursor, Foxit, Adobe, Microsoft 365, etc.)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Travel periods are automatically detected for 90%+ of domestic flights and hotel stays without manual intervention
- **SC-002**: Users spend less than 30 seconds reviewing and confirming a detected travel period
- **SC-003**: Subscriptions are detected with 95%+ accuracy after 2 months of transaction history
- **SC-004**: Users are alerted to missing recurring charges by calendar month end if subscription not detected
- **SC-005**: Split pattern suggestions are applied in 80%+ of cases where the pattern exists
- **SC-006**: All travel detection and subscription identification uses Tier 1 (rule-based) for 95%+ of cases, with AI fallback only for complex scenarios
- **SC-007**: Users can complete an expense split allocation in under 60 seconds
- **SC-008**: System handles 50+ travel periods and 20+ subscriptions per user without performance degradation

## Clarifications

### Session 2025-12-16

- Q: When a travel period is created from a flight receipt and later extended by a hotel receipt, how should the destination be determined? → A: Flight arrival city (primary), hotel location as fallback
- Q: How should "current period" be defined for subscription missing alerts? → A: Calendar month end (alert if not seen by last day of month)
- Q: Should split patterns be scoped to individual users or shared across the organization? → A: User-scoped only (each user has their own split patterns)
- Q: How should the system distinguish subscription vs one-time purchases from the same vendor? → A: Amount tolerance ±20% from average - outside range treated as one-time purchase
- Q: When the system suggests a split pattern, can the user modify it before applying? → A: Yes, suggestion pre-fills form, user can adjust before saving

## Assumptions

- Flight and hotel vendors follow predictable naming patterns in bank/card statements
- Subscription charges occur within a consistent date range each month (plus or minus 7 days)
- Users will confirm travel periods and subscriptions, providing feedback for accuracy improvement
- Existing vendor alias system (Sprint 5) provides foundation for vendor recognition
- Existing tiered categorization system (Sprint 6) provides infrastructure for suggestion ranking
- Split patterns are vendor-specific (same vendor typically gets split the same way)
