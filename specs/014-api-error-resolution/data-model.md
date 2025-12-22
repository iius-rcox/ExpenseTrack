# Data Model: API Error Resolution

**Feature**: 014-api-error-resolution
**Date**: 2025-12-22

## Overview

This feature adds two new DTOs and their corresponding response models. No database schema changes are required - all data comes from existing entities.

---

## New DTOs

### PendingActionDto

Represents an actionable item requiring user attention (match review, categorization approval).

```csharp
namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Represents a pending action requiring user review.
/// </summary>
public record PendingActionDto
{
    /// <summary>
    /// Unique identifier for the action item.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Type of action: "match_review" or "categorization".
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Human-readable action title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed description of what needs review.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// When the action was created.
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Optional metadata for the action (e.g., confidence score for matches).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
```

**Source Entities**:
- `ReceiptTransactionMatches` (Status = Proposed) → Type = "match_review"
- Future: `Categorizations` (pending approval) → Type = "categorization"

---

### CategoryBreakdownDto

Aggregated spending breakdown by category for a given period.

```csharp
namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Category spending breakdown for a period.
/// </summary>
public record CategoryBreakdownDto
{
    /// <summary>
    /// Period in YYYY-MM format.
    /// </summary>
    public required string Period { get; init; }

    /// <summary>
    /// Total spending amount for the period.
    /// </summary>
    public required decimal TotalSpending { get; init; }

    /// <summary>
    /// Number of transactions in the period.
    /// </summary>
    public required int TransactionCount { get; init; }

    /// <summary>
    /// Breakdown by category.
    /// </summary>
    public required List<CategorySpendingDto> Categories { get; init; }
}
```

---

### CategorySpendingDto

Individual category spending within a breakdown.

```csharp
namespace ExpenseFlow.Shared.DTOs;

/// <summary>
/// Spending summary for a single category.
/// </summary>
public record CategorySpendingDto
{
    /// <summary>
    /// Category name (e.g., "Food & Dining", "Transportation").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Total amount spent in this category.
    /// </summary>
    public required decimal Amount { get; init; }

    /// <summary>
    /// Percentage of total spending.
    /// </summary>
    public required decimal Percentage { get; init; }

    /// <summary>
    /// Number of transactions in this category.
    /// </summary>
    public required int TransactionCount { get; init; }
}
```

**Source Entity**: `Transaction` (grouped by `Category` field)

---

## Entity Relationships

```text
┌─────────────────────────┐       ┌──────────────────────────┐
│ ReceiptTransactionMatch │       │       Transaction        │
├─────────────────────────┤       ├──────────────────────────┤
│ Id                      │       │ Id                       │
│ UserId                  │       │ UserId                   │
│ ReceiptId               │       │ Category                 │
│ TransactionId           │       │ Amount                   │
│ Status (Proposed)       │──────▶│ TransactionDate          │
│ ConfidenceScore         │       │ Description              │
│ CreatedAt               │       └──────────────────────────┘
└─────────────────────────┘                  │
         │                                   │
         │ maps to                           │ aggregates to
         ▼                                   ▼
┌─────────────────────────┐       ┌──────────────────────────┐
│    PendingActionDto     │       │   CategoryBreakdownDto   │
├─────────────────────────┤       ├──────────────────────────┤
│ Id                      │       │ Period                   │
│ Type = "match_review"   │       │ TotalSpending            │
│ Title                   │       │ TransactionCount         │
│ Description             │       │ Categories[]             │
│ CreatedAt               │       │   └─ CategorySpendingDto │
│ Metadata                │       └──────────────────────────┘
└─────────────────────────┘
```

---

## Validation Rules

### PendingActionDto

| Field | Type | Validation |
|-------|------|------------|
| Id | string | Required, non-empty |
| Type | string | Required, one of: "match_review", "categorization" |
| Title | string | Required, max 200 chars |
| Description | string | Required, max 500 chars |
| CreatedAt | DateTime | Required, valid UTC datetime |
| Metadata | Dictionary | Optional |

### CategoryBreakdownDto

| Field | Type | Validation |
|-------|------|------------|
| Period | string | Required, format YYYY-MM |
| TotalSpending | decimal | Required, >= 0 |
| TransactionCount | int | Required, >= 0 |
| Categories | List | Required, may be empty |

### CategorySpendingDto

| Field | Type | Validation |
|-------|------|------------|
| Category | string | Required, non-empty |
| Amount | decimal | Required, >= 0 |
| Percentage | decimal | Required, 0-100 |
| TransactionCount | int | Required, >= 0 |

---

## File Location

These DTOs should be added to:
```
backend/src/ExpenseFlow.Shared/DTOs/PendingActionDto.cs
backend/src/ExpenseFlow.Shared/DTOs/CategoryBreakdownDto.cs
```

Following the existing pattern where `DashboardMetricsDto`, `RecentActivityItemDto`, and `MonthlyComparisonDto` are defined.
