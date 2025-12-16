# Data Model: Advanced Features

**Feature**: 007-advanced-features
**Date**: 2025-12-16
**Status**: Complete

## Entity Overview

This sprint introduces 3 new entities and modifies 2 existing entities:

| Entity | Type | Purpose |
|--------|------|---------|
| TravelPeriod | NEW | Tracks detected business trips |
| DetectedSubscription | NEW | Tracks recurring subscription charges |
| KnownSubscriptionVendor | NEW | Seed data for immediate subscription recognition |
| SplitPattern | MODIFY | Add UserId for user-scoping |
| VendorAlias | MODIFY | Add Category for vendor classification |

---

## New Entities

### TravelPeriod

Represents a detected or manually created business travel period.

```
TravelPeriod
├── Id: Guid (PK)
├── UserId: Guid (FK → Users, required)
├── StartDate: DateOnly (required)
├── EndDate: DateOnly (required)
├── Destination: string? (nullable, max 100 chars)
├── Source: TravelPeriodSource (enum: Flight, Hotel, Manual)
├── SourceReceiptId: Guid? (FK → Receipts, nullable)
├── RequiresAiReview: bool (default: false)
├── CreatedAt: DateTime (auto)
└── UpdatedAt: DateTime? (nullable)
```

**Relationships**:
- Many-to-One: TravelPeriod → User
- Many-to-One: TravelPeriod → Receipt (source document)

**Indexes**:
- `IX_TravelPeriods_UserId` (non-clustered)
- `IX_TravelPeriods_UserId_StartDate_EndDate` (composite, for overlap queries)

**Validation Rules**:
- EndDate >= StartDate
- Destination max length 100 characters
- SourceReceiptId required when Source != Manual

**State Transitions**:
```
[Created] ──(hotel receipt)──> [Extended]
[Created] ──(AI flag)──> [RequiresReview]
[RequiresReview] ──(user confirm)──> [Confirmed]
```

---

### DetectedSubscription

Tracks recurring charges identified through pattern analysis or seed data matching.

```
DetectedSubscription
├── Id: Guid (PK)
├── UserId: Guid (FK → Users, required)
├── VendorAliasId: Guid? (FK → VendorAliases, nullable)
├── VendorName: string (required, max 200 chars)
├── AverageAmount: decimal (precision 18,2)
├── OccurrenceMonths: string (JSON array of "YYYY-MM")
├── LastSeenDate: DateOnly (required)
├── ExpectedNextDate: DateOnly? (nullable, calculated)
├── Status: SubscriptionStatus (enum: Active, Missing, Flagged)
├── DetectionSource: DetectionSource (enum: PatternMatch, SeedData)
├── CreatedAt: DateTime (auto)
└── UpdatedAt: DateTime? (nullable)
```

**Relationships**:
- Many-to-One: DetectedSubscription → User
- Many-to-One: DetectedSubscription → VendorAlias (optional)

**Indexes**:
- `IX_DetectedSubscriptions_UserId` (non-clustered)
- `IX_DetectedSubscriptions_UserId_Status` (composite, for alert queries)
- `IX_DetectedSubscriptions_VendorAliasId` (non-clustered)

**Validation Rules**:
- AverageAmount > 0
- OccurrenceMonths must be valid JSON array
- VendorName max length 200 characters

**State Transitions**:
```
[Detected] ──(charge appears)──> [Active]
[Active] ──(month end, no charge)──> [Missing]
[Active] ──(amount varies >20%)──> [Flagged]
[Missing] ──(charge appears)──> [Active]
[Flagged] ──(user confirms)──> [Active]
```

---

### KnownSubscriptionVendor

Seed data table for immediate subscription recognition without pattern detection.

```
KnownSubscriptionVendor
├── Id: Guid (PK)
├── VendorPattern: string (required, max 100 chars)
├── DisplayName: string (required, max 100 chars)
├── Category: string? (nullable, e.g., "Software", "Cloud", "Media")
├── TypicalAmount: decimal? (nullable, for reference)
├── IsActive: bool (default: true)
└── CreatedAt: DateTime (auto)
```

**Indexes**:
- `IX_KnownSubscriptionVendors_VendorPattern` (unique)
- `IX_KnownSubscriptionVendors_IsActive` (filtered)

**Seed Data**:
| VendorPattern | DisplayName | Category |
|---------------|-------------|----------|
| OPENAI | OpenAI | Software |
| CLAUDE | Claude.AI | Software |
| CURSOR | Cursor | Software |
| GITHUB | GitHub | Software |
| MICROSOFT 365 | Microsoft 365 | Software |
| ADOBE | Adobe Creative Cloud | Software |
| SPOTIFY | Spotify | Media |
| NETFLIX | Netflix | Media |
| AMAZON PRIME | Amazon Prime | Media |
| ZOOM | Zoom | Software |
| SLACK | Slack | Software |
| NOTION | Notion | Software |
| FIGMA | Figma | Software |
| AWS | Amazon Web Services | Cloud |
| AZURE | Microsoft Azure | Cloud |
| GOOGLE CLOUD | Google Cloud | Cloud |
| DIGITALOCEAN | DigitalOcean | Cloud |
| HEROKU | Heroku | Cloud |

---

## Modified Entities

### SplitPattern (Existing)

Add `UserId` for user-scoping per clarification.

```diff
SplitPattern
├── Id: Guid (PK)
+├── UserId: Guid (FK → Users, required)
├── VendorAliasId: Guid? (FK → VendorAliases, nullable)
├── SplitConfig: string (JSON, required)
├── UsageCount: int (default: 0)
├── LastUsedAt: DateTime? (nullable)
└── CreatedAt: DateTime (auto)
```

**New Index**:
- `IX_SplitPatterns_UserId_VendorAliasId` (composite, for pattern lookup)

**SplitConfig JSON Schema**:
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "allocations": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "glCode": { "type": "string", "maxLength": 10 },
          "department": { "type": "string", "maxLength": 10 },
          "project": { "type": "string", "maxLength": 20, "nullable": true },
          "percentage": { "type": "number", "minimum": 0, "maximum": 100 },
          "fixedAmount": { "type": "number", "minimum": 0, "nullable": true }
        },
        "required": ["glCode", "percentage"]
      },
      "minItems": 2
    }
  },
  "required": ["allocations"]
}
```

**Validation Rules**:
- Sum of percentages must equal exactly 100
- At least 2 allocations required (otherwise not a split)
- Either percentage OR fixedAmount, not both per allocation

---

### VendorAlias (Existing)

Add `Category` for vendor classification (airline/hotel/subscription detection).

```diff
VendorAlias
├── Id: Guid (PK)
├── CanonicalName: string
├── AliasPattern: string
├── DisplayName: string
├── DefaultGLCode: string?
├── DefaultDepartment: string?
├── MatchCount: int
├── LastMatchedAt: DateTime?
├── Confidence: decimal
+├── Category: VendorCategory (enum, default: Standard)
└── CreatedAt: DateTime (auto)
```

**New Enum**:
```csharp
public enum VendorCategory
{
    Standard = 0,      // Normal vendor
    Airline = 1,       // Triggers travel period detection
    Hotel = 2,         // Triggers travel period extension
    Subscription = 3   // Known recurring charge
}
```

**New Index**:
- `IX_VendorAliases_Category` (filtered on Airline/Hotel for travel detection)

**Migration Seed Data** (add Category to existing airline/hotel vendors):
- DELTA, UNITED, AMERICAN, SOUTHWEST, ALASKA, JETBLUE → Airline
- MARRIOTT, HILTON, HYATT, IHG, AIRBNB, VRBO, HOLIDAY INN → Hotel

---

## Enums

### TravelPeriodSource
```csharp
public enum TravelPeriodSource
{
    Flight = 0,   // Created from flight receipt
    Hotel = 1,    // Created from hotel receipt
    Manual = 2    // User-created manually
}
```

### SubscriptionStatus
```csharp
public enum SubscriptionStatus
{
    Active = 0,   // Subscription detected and current
    Missing = 1,  // Expected charge not seen by month end
    Flagged = 2   // Unusual amount variation (>20%)
}
```

### DetectionSource
```csharp
public enum DetectionSource
{
    PatternMatch = 0,  // Detected via 2+ consecutive months
    SeedData = 1       // Matched KnownSubscriptionVendor
}
```

### VendorCategory
```csharp
public enum VendorCategory
{
    Standard = 0,
    Airline = 1,
    Hotel = 2,
    Subscription = 3
}
```

---

## Entity Relationship Diagram

```
┌─────────────┐       ┌──────────────────────┐
│    User     │───1:N─│    TravelPeriod      │
└─────────────┘       └──────────────────────┘
       │                        │
       │                        │ N:1
       │                        ▼
       │              ┌──────────────────────┐
       │              │      Receipt         │
       │              └──────────────────────┘
       │
       ├───1:N────────┌──────────────────────┐
       │              │ DetectedSubscription │──N:1──┐
       │              └──────────────────────┘       │
       │                                             │
       ├───1:N────────┌──────────────────────┐       │
       │              │    SplitPattern      │──N:1──┤
       │              └──────────────────────┘       │
       │                                             │
       │              ┌──────────────────────┐       │
       └──────────────│    VendorAlias       │◄──────┘
                      └──────────────────────┘
                               │
                               │ (lookup)
                               ▼
                      ┌──────────────────────┐
                      │KnownSubscriptionVendor│
                      └──────────────────────┘
```

---

## Migration Strategy

### Migration 1: Add VendorCategory
1. Add `Category` column to `vendor_aliases` with default `Standard`
2. Update seed data for airline/hotel vendors
3. Add index on Category

### Migration 2: Add TravelPeriod
1. Create `travel_periods` table
2. Create indexes
3. Add FK constraints

### Migration 3: Add DetectedSubscription
1. Create `detected_subscriptions` table
2. Create indexes
3. Add FK constraints

### Migration 4: Add KnownSubscriptionVendor
1. Create `known_subscription_vendors` table
2. Insert seed data
3. Create indexes

### Migration 5: Modify SplitPattern
1. Add `UserId` column (nullable initially)
2. Backfill UserId from existing data (if any)
3. Make UserId required
4. Add composite index
