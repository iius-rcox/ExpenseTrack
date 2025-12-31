# Data Model: Analytics Dashboard API Endpoints

**Feature**: 019-analytics-dashboard
**Date**: 2025-12-31

## Overview

This feature uses **existing entities** (Transaction, VendorAlias, DetectedSubscription) and introduces **new DTOs** for API responses. No database schema changes required.

## Existing Entities (Read-Only)

### Transaction
**Location**: `ExpenseFlow.Core.Entities.Transaction`

| Field | Type | Usage in Analytics |
|-------|------|-------------------|
| Id | Guid | Record identifier |
| UserId | Guid | Filter by authenticated user |
| TransactionDate | DateOnly | Aggregation key for trends |
| Description | string | Vendor name extraction, category derivation |
| Amount | decimal | Sum for totals (positive=expense, negative=refund) |

### VendorAlias
**Location**: `ExpenseFlow.Core.Entities.VendorAlias`

| Field | Type | Usage in Analytics |
|-------|------|-------------------|
| CanonicalName | string | Normalized vendor name for grouping |
| DisplayName | string | Human-readable name for UI |
| Category | VendorCategory | Optional secondary classification |

### DetectedSubscription
**Location**: `ExpenseFlow.Core.Entities.DetectedSubscription`

Already exposed via existing SubscriptionDetectionService - analytics endpoints proxy to this.

## New DTOs (ExpenseFlow.Shared.DTOs)

### SpendingTrendItemDto

```csharp
/// <summary>
/// Single data point in a spending trend time series.
/// </summary>
public record SpendingTrendItemDto
{
    /// <summary>Period identifier (ISO date for day, "2025-W05" for week, "2025-02" for month)</summary>
    public required string Date { get; init; }

    /// <summary>Total amount for this period (sum of all transactions)</summary>
    public decimal Amount { get; init; }

    /// <summary>Number of transactions in this period</summary>
    public int TransactionCount { get; init; }
}
```

### SpendingByCategoryItemDto

```csharp
/// <summary>
/// Category spending aggregate.
/// </summary>
public record SpendingByCategoryItemDto
{
    /// <summary>Category name (derived from transaction patterns)</summary>
    public required string Category { get; init; }

    /// <summary>Total amount spent in this category</summary>
    public decimal Amount { get; init; }

    /// <summary>Number of transactions in this category</summary>
    public int TransactionCount { get; init; }

    /// <summary>Percentage of total spending (0.00-100.00)</summary>
    public decimal PercentageOfTotal { get; init; }
}
```

### SpendingByVendorItemDto

```csharp
/// <summary>
/// Vendor spending aggregate.
/// </summary>
public record SpendingByVendorItemDto
{
    /// <summary>Vendor name (from transaction description)</summary>
    public required string VendorName { get; init; }

    /// <summary>Total amount spent with this vendor</summary>
    public decimal Amount { get; init; }

    /// <summary>Number of transactions with this vendor</summary>
    public int TransactionCount { get; init; }

    /// <summary>Percentage of total spending (0.00-100.00)</summary>
    public decimal PercentageOfTotal { get; init; }
}
```

### MerchantAnalyticsResponseDto

```csharp
/// <summary>
/// Comprehensive merchant analytics response.
/// </summary>
public record MerchantAnalyticsResponseDto
{
    /// <summary>Top merchants by spending amount</summary>
    public required List<TopMerchantDto> TopMerchants { get; init; }

    /// <summary>Merchants appearing in current period but not comparison period</summary>
    public required List<TopMerchantDto> NewMerchants { get; init; }

    /// <summary>Merchants with significant spending changes (>50%)</summary>
    public required List<TopMerchantDto> SignificantChanges { get; init; }

    /// <summary>Total unique merchant count in the period</summary>
    public int TotalMerchantCount { get; init; }

    /// <summary>Analysis date range</summary>
    public required AnalyticsDateRangeDto DateRange { get; init; }
}
```

### TopMerchantDto

```csharp
/// <summary>
/// Detailed merchant analytics.
/// </summary>
public record TopMerchantDto
{
    /// <summary>Merchant/vendor name</summary>
    public required string MerchantName { get; init; }

    /// <summary>Display-friendly name (if different)</summary>
    public string? DisplayName { get; init; }

    /// <summary>Total amount spent</summary>
    public decimal TotalAmount { get; init; }

    /// <summary>Transaction count</summary>
    public int TransactionCount { get; init; }

    /// <summary>Average transaction amount</summary>
    public decimal AverageAmount { get; init; }

    /// <summary>Percentage of total spending</summary>
    public decimal PercentageOfTotal { get; init; }

    /// <summary>Previous period amount (null if comparison not requested or new merchant)</summary>
    public decimal? PreviousAmount { get; init; }

    /// <summary>Percentage change from previous period</summary>
    public decimal? ChangePercent { get; init; }

    /// <summary>Trend direction: "increasing", "decreasing", or "stable"</summary>
    public string? Trend { get; init; }
}
```

### AnalyticsDateRangeDto

```csharp
/// <summary>
/// Date range for analytics queries.
/// </summary>
public record AnalyticsDateRangeDto
{
    /// <summary>Start date (ISO format YYYY-MM-DD)</summary>
    public required string StartDate { get; init; }

    /// <summary>End date (ISO format YYYY-MM-DD)</summary>
    public required string EndDate { get; init; }
}
```

### AnalyticsSubscriptionResponseDto

```csharp
/// <summary>
/// Subscription detection results for analytics dashboard.
/// Mirrors existing SubscriptionListResponseDto structure.
/// </summary>
public record AnalyticsSubscriptionResponseDto
{
    /// <summary>Detected subscriptions</summary>
    public required List<SubscriptionDetailDto> Subscriptions { get; init; }

    /// <summary>Summary statistics</summary>
    public required SubscriptionSummaryDto Summary { get; init; }

    /// <summary>Newly detected subscriptions</summary>
    public required List<SubscriptionDetailDto> NewSubscriptions { get; init; }

    /// <summary>Subscriptions that may have ended</summary>
    public required List<SubscriptionDetailDto> PossiblyEnded { get; init; }

    /// <summary>When analysis was performed</summary>
    public DateTime AnalyzedAt { get; init; }
}
```

## Derived Categories

Pattern-based category derivation from transaction descriptions:

| Category | Matching Patterns |
|----------|------------------|
| Transportation | UBER, LYFT, TAXI, PARKING, GAS, SHELL, CHEVRON, EXXON, FUEL |
| Food & Dining | RESTAURANT, CAFE, COFFEE, STARBUCKS, MCDONALD, DOORDASH, PIZZA |
| Travel & Lodging | HOTEL, MARRIOTT, HILTON, AIRBNB, AIRLINE, SOUTHWEST, DELTA |
| Shopping & Retail | AMAZON, WALMART, TARGET, COSTCO, BEST BUY, APPLE |
| Entertainment | NETFLIX, SPOTIFY, HULU, DISNEY, YOUTUBE, MOVIE, THEATER |
| Office & Business | OFFICE, STAPLES, FEDEX, UPS, ZOOM, MICROSOFT, ADOBE |
| Healthcare | PHARMACY, CVS, WALGREENS, MEDICAL, DOCTOR, HOSPITAL |
| Utilities & Bills | ELECTRIC, WATER, INTERNET, PHONE, VERIZON, AT&T, T-MOBILE |
| Other | (default when no pattern matches) |

## Validation Rules

| Rule | Implementation |
|------|----------------|
| Date format | ISO 8601 (YYYY-MM-DD) - parsed with DateOnly.TryParse |
| Date range | startDate <= endDate, max 5 years (1,826 days) |
| Granularity | Enum: day, week, month |
| topCount | Integer 1-100, default 10 |
| minConfidence | Enum: high, medium, low |

## Relationships

```
User (1) ──────< Transaction (*)
                     │
                     ▼
              [Description text]
                     │
                     ├──> DeriveCategory() ──> Category name
                     └──> ExtractVendor()  ──> Vendor name
```

No foreign key relationships added. Analytics aggregates are computed at query time from existing transaction data.
