# Research: Advanced Features

**Feature**: 007-advanced-features
**Date**: 2025-12-16
**Status**: Complete

## Research Summary

This document captures technical decisions and patterns for implementing travel detection, subscription identification, and expense splitting features.

---

## 1. Travel Period Detection

### Decision: Rule-Based Vendor Pattern Matching

**Rationale**: Flight and hotel vendors follow predictable naming patterns in bank statements. Rule-based matching (Tier 1) achieves 90%+ accuracy for domestic travel without AI costs.

**Approach**:
- Maintain airline vendor patterns: "DELTA", "UNITED", "AMERICAN AIR", "SOUTHWEST", "ALASKA AIR", "JETBLUE"
- Maintain hotel patterns: "MARRIOTT", "HILTON", "HYATT", "IHG", "AIRBNB", "VRBO", "HOLIDAY INN"
- Use existing VendorAlias infrastructure with new `VendorCategory` enum (Airline/Hotel/Standard)

**Alternatives Considered**:
| Alternative | Rejected Because |
|-------------|------------------|
| AI classification for all receipts | Violates Principle I (Cost-First); rule-based handles 90%+ cases |
| Keyword-only matching | Too fragile; vendor alias patterns provide better accuracy |
| External travel API integration | Over-engineering for initial scope; can add later if needed |

### Decision: Date Extraction from Receipt Line Items

**Rationale**: Flight receipts contain departure dates in line items. Hotel receipts contain check-in/check-out dates. Existing `ReceiptLineItem` already captures this data via Document Intelligence.

**Approach**:
- Parse `Receipt.LineItems` for date patterns after vendor categorization
- Flight: Use first travel date as period start
- Hotel: Use check-in date as start, calculate end from nights or check-out
- Store destination from city/airport codes when available

**Key Finding**: Document Intelligence already extracts structured line items. No additional OCR needed.

---

## 2. Subscription Detection

### Decision: Pattern Recognition via SQL Window Functions

**Rationale**: Subscription detection is a data analysis problem, not an AI problem. SQL window functions efficiently identify recurring patterns across months.

**Approach**:
```sql
-- Detect subscriptions: same vendor, similar amount, 2+ consecutive months
WITH monthly_charges AS (
    SELECT
        vendor_alias_id,
        DATE_TRUNC('month', transaction_date) AS charge_month,
        AVG(amount) AS avg_amount,
        COUNT(*) AS charge_count
    FROM transactions t
    JOIN vendor_aliases va ON t.description LIKE va.alias_pattern
    WHERE user_id = @userId
    GROUP BY vendor_alias_id, DATE_TRUNC('month', transaction_date)
),
consecutive_months AS (
    SELECT
        vendor_alias_id,
        charge_month,
        avg_amount,
        LAG(charge_month) OVER (PARTITION BY vendor_alias_id ORDER BY charge_month) AS prev_month,
        LAG(avg_amount) OVER (PARTITION BY vendor_alias_id ORDER BY charge_month) AS prev_amount
    FROM monthly_charges
)
SELECT vendor_alias_id, AVG(avg_amount) AS subscription_amount
FROM consecutive_months
WHERE charge_month = prev_month + INTERVAL '1 month'
  AND ABS(avg_amount - prev_amount) / prev_amount < 0.20  -- ±20% tolerance
GROUP BY vendor_alias_id
HAVING COUNT(*) >= 2;  -- 2+ consecutive months
```

**Alternatives Considered**:
| Alternative | Rejected Because |
|-------------|------------------|
| AI pattern detection | Violates Principle I; SQL pattern matching is deterministic and free |
| Fixed-date matching | Too brittle; billing dates vary by ±7 days |
| External subscription tracking API | Adds dependency; internal detection is sufficient |

### Decision: Known Subscription Vendor Seed Data

**Rationale**: Immediate recognition for common subscription vendors improves UX. No need to wait for pattern detection.

**Seed Data**:
```
Claude.AI, OpenAI, Cursor, GitHub, Microsoft 365, Adobe Creative Cloud,
Spotify, Netflix, Amazon Prime, Zoom, Slack, Notion, Figma,
AWS, Azure, Google Cloud, DigitalOcean, Heroku
```

---

## 3. Expense Splitting

### Decision: Extend Existing SplitPattern Entity

**Rationale**: `SplitPattern` entity already exists in codebase (Sprint 5). Add `UserId` for user-scoping per clarification session.

**Current Entity** (from codebase):
```csharp
public class SplitPattern : BaseEntity
{
    public Guid? VendorAliasId { get; set; }
    public string SplitConfig { get; set; } = "{}";  // JSON
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
```

**Required Changes**:
- Add `UserId` property (FK to Users) for user-scoping
- Define `SplitConfig` JSON schema:
```json
{
  "allocations": [
    { "glCode": "64100", "department": "07", "percentage": 60.0 },
    { "glCode": "65100", "department": "12", "percentage": 40.0 }
  ]
}
```

### Decision: Pre-fill UI with Modifiable Suggestions

**Rationale**: Per clarification, users want flexibility to adjust splits for specific cases while benefiting from pattern suggestions.

**UX Flow**:
1. User selects expense to split
2. System checks SplitPattern table for vendor match
3. If found: pre-fill split form with pattern allocations
4. User can modify allocations before saving
5. Modified splits do NOT update the stored pattern (pattern remains stable)

---

## 4. Travel Period Entity Relationships

### Decision: Link Travel Periods to Source Documents

**Rationale**: Travel periods need audit trail showing which receipts created/extended them.

**Entity Design**:
```
TravelPeriod
├── Id (PK)
├── UserId (FK → Users)
├── StartDate
├── EndDate
├── Destination (nullable)
├── Source ("Flight" | "Hotel" | "Manual")
├── SourceReceiptId (FK → Receipts, nullable)
├── RequiresAiReview (bool)
├── CreatedAt
└── UpdatedAt
```

**Relationship Notes**:
- One-to-many: User → TravelPeriods
- Many-to-one: TravelPeriod → Receipt (source document)
- Travel periods don't directly link to transactions; relationship is via date overlap

---

## 5. Integration with Existing Systems

### Decision: Leverage Existing VendorAlias Infrastructure

**Finding**: VendorAlias already supports pattern matching. Add `VendorCategory` to distinguish airlines/hotels.

**Changes Required**:
```csharp
public enum VendorCategory
{
    Standard,   // Normal vendors
    Airline,    // Triggers travel period detection
    Hotel,      // Triggers travel period detection/extension
    Subscription // Known subscription vendor (immediate flagging)
}

// Add to VendorAlias entity:
public VendorCategory Category { get; set; } = VendorCategory.Standard;
```

### Decision: Use Hangfire for Subscription Alerts

**Finding**: Hangfire already configured for background jobs. Add monthly job for subscription missing alerts.

**Job Schedule**: Run on 1st of each month at 4 AM to check previous month's subscriptions.

---

## 6. API Design Patterns

### Decision: Follow Existing Controller Patterns

**Finding**: Existing controllers use consistent patterns:
- `[Authorize]` attribute for auth
- `[FromQuery]` for filtering
- `ActionResult<T>` return types
- Pagination via `skip`/`take` parameters

**New Endpoints Follow Same Patterns**:
```
GET  /api/travel-periods           - List user's travel periods
POST /api/travel-periods           - Manually create travel period
PUT  /api/travel-periods/{id}      - Update travel period
DELETE /api/travel-periods/{id}    - Delete travel period

GET  /api/subscriptions            - List detected subscriptions
GET  /api/subscriptions/alerts     - Get missing subscription alerts

POST /api/expenses/{id}/split      - Apply split to expense
GET  /api/split-patterns           - List user's split patterns
```

---

## Technical Dependencies

| Dependency | Version | Purpose |
|------------|---------|---------|
| Entity Framework Core | 8.x | ORM, migrations |
| Npgsql | 8.x | PostgreSQL provider |
| Hangfire | 1.8.x | Background job scheduling |
| F23.StringSimilarity | 5.x | Vendor name fuzzy matching (existing) |

No new NuGet packages required.

---

## Performance Considerations

| Operation | Target | Approach |
|-----------|--------|----------|
| Travel period detection | <500ms | Rule-based vendor matching, no AI |
| Split pattern lookup | <100ms | Direct SQL query on indexed VendorAliasId |
| Subscription detection | <2s | SQL window functions, batch monthly |
| Missing subscription check | N/A | Background job, not user-facing |

---

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Airline vendor patterns miss international carriers | Start with domestic + major international; user can manually create periods |
| Subscription amount varies too much | ±20% tolerance handles most cases; flag outliers for review |
| Split patterns become stale | Track `LastUsedAt`; surface unused patterns in admin UI |
| Travel period overlap conflicts | Merge adjacent periods with same destination; keep separate if different |
