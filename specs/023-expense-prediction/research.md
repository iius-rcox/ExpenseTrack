# Research: Expense Prediction from Historical Reports

**Feature**: 023-expense-prediction
**Date**: 2026-01-02

## Overview

This document captures technical research and design decisions for implementing expense prediction based on historical expense reports.

---

## 1. Pattern Extraction Algorithm

### Decision: Vendor-Centric Pattern Aggregation

Extract patterns from approved expense reports by aggregating expense lines grouped by normalized vendor name.

### Rationale

- **Vendor is the strongest signal**: Users consistently expense the same vendors (Starbucks, Uber, airlines)
- **Existing infrastructure**: VendorAlias system already normalizes vendor names
- **Simplicity**: Direct vendor matching is fast and interpretable

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| Description embedding similarity | Too slow for real-time prediction, overkill for this use case |
| Category-only matching | Too coarse - "Dining" includes both business lunches and personal dinners |
| Amount-based clustering | Too fragile - same vendor with different amounts is still same vendor |

### Algorithm

```text
For each approved expense report (status = Submitted):
  For each expense line:
    1. Normalize vendor name via VendorAlias lookup
    2. Upsert ExpensePattern for (UserId, NormalizedVendor):
       - Increment OccurrenceCount
       - Update running average amount: avg = (avg * (n-1) + amount) / n
       - Track min/max amount seen
       - Update LastSeenAt timestamp
       - Record most common GLCode and DepartmentCode
       - Apply exponential decay weight to previous data
```

---

## 2. Exponential Decay Formula

### Decision: Half-Life Decay with 6-Month Period

Recent expense patterns weighted more heavily using exponential decay with a 6-month half-life.

### Rationale

- **Clarification answer**: User specified 2x weighting for recent reports
- **6-month half-life**: After 6 months, older patterns contribute 50% of original weight
- **Seasonal capture**: 12-month effective window captures annual conferences, subscriptions

### Formula

```text
weight(pattern) = 2^(-monthsAgo / 6)

Examples:
- Current month: weight = 1.0
- 6 months ago: weight = 0.5
- 12 months ago: weight = 0.25
- 24 months ago: weight = 0.0625
```

### Implementation

```csharp
public decimal CalculateDecayWeight(DateTimeOffset patternDate)
{
    var monthsAgo = (DateTimeOffset.UtcNow - patternDate).TotalDays / 30.0;
    var halfLifeMonths = 6.0;
    return (decimal)Math.Pow(2, -monthsAgo / halfLifeMonths);
}
```

---

## 3. Confidence Score Calculation

### Decision: Multi-Signal Weighted Confidence

Confidence score (0.0 - 1.0) calculated from multiple signals with weighted combination.

### Rationale

- Single signal (e.g., occurrence count) is brittle
- Multiple signals provide robustness and explainability
- Weights tuned for practical expense workflow

### Signals and Weights

| Signal | Weight | Description |
|--------|--------|-------------|
| Occurrence frequency | 0.40 | More occurrences = higher confidence |
| Recency | 0.25 | Recent patterns weighted via decay |
| Amount consistency | 0.20 | Low variance = higher confidence |
| User feedback | 0.15 | Prior confirmations boost, rejections reduce |

### Confidence Formula

```text
confidence =
  0.40 * min(occurrenceCount / 5, 1.0) +
  0.25 * decayWeight +
  0.20 * (1 - normalizedAmountVariance) +
  0.15 * feedbackScore

Where:
- occurrenceCount: Number of times vendor appeared in expense reports
- decayWeight: Exponential decay based on recency
- normalizedAmountVariance: stddev(amounts) / avg(amounts), capped at 1.0
- feedbackScore: (confirms - rejects) / total_feedback, mapped to 0-1
```

### Thresholds (per clarification)

| Level | Score Range | Display |
|-------|-------------|---------|
| High | >= 0.75 | ✓ Shown with "High Confidence" |
| Medium | 0.50 - 0.74 | ✓ Shown with "Medium Confidence" |
| Low | < 0.50 | ✗ Suppressed (not displayed) |

---

## 4. Batch Prediction Performance

### Decision: Eager Pattern Loading with In-Memory Matching

Load all user patterns into memory once, then match against all transactions in a single pass.

### Rationale

- **Target**: <5 seconds for 1000 transactions (SC-005)
- **Pattern count**: ~100 patterns per user typical
- **Memory footprint**: ~10KB per user (acceptable)

### Algorithm

```text
1. Load all ExpensePatterns for user into Dictionary<normalizedVendor, pattern>
2. For each transaction:
   a. Normalize vendor via VendorAlias lookup
   b. O(1) dictionary lookup for matching pattern
   c. Calculate confidence score
   d. If confidence >= 0.50: create TransactionPrediction
3. Bulk insert predictions
```

### Performance Analysis

| Operation | Cost | Notes |
|-----------|------|-------|
| Load patterns | O(P) | P = ~100 patterns, single DB query |
| Vendor normalization | O(T) | T = transactions, uses cached VendorAlias |
| Pattern matching | O(T) | Dictionary lookup O(1) per transaction |
| Confidence calculation | O(T) | Simple arithmetic |
| Bulk insert | O(T) | Single batch insert |

**Total**: O(T + P) ≈ O(T) for typical P << T

**Expected**: 1000 transactions in ~500ms (well under 5s target)

---

## 5. Pattern Recalculation Trigger

### Decision: Event-Driven on Report Approval

Recalculate patterns when an expense report status changes to "Submitted".

### Rationale

- Approved reports represent ground truth (per spec assumptions)
- Event-driven is more efficient than scheduled batch
- Immediate feedback for user's next import

### Implementation

```csharp
// In ExpenseReportService.SubmitReportAsync()
await _expensePredictionService.UpdatePatternsFromReportAsync(report);
```

### Handling Draft Report Pre-Population (P2)

When generating a new draft:
1. Run predictions on all unmatched transactions in the period
2. Auto-select transactions with High confidence
3. Mark Medium confidence as "suggested" (user reviews)
4. Ignore Low confidence (< 0.50)

---

## 6. Feedback Learning Mechanism

### Decision: Incremental Bayesian Update

User feedback (confirm/reject) adjusts pattern confidence using Bayesian-inspired updates.

### Rationale

- Explicit feedback is higher quality than implicit
- Bayesian approach prevents overcorrection from single feedback
- Preserves base pattern strength while incorporating new evidence

### Update Rules

```text
On CONFIRM:
  confirmCount += 1
  feedbackScore = confirmCount / (confirmCount + rejectCount)

On REJECT:
  rejectCount += 1
  feedbackScore = confirmCount / (confirmCount + rejectCount)

  If rejectCount > 3 && feedbackScore < 0.3:
    Mark pattern as "suppressed" (user explicitly excludes this vendor)
```

### UI Interaction (per clarification)

Each predicted transaction shows:
- "Likely Expense" badge with confidence level
- ✓ Confirm button (thumb up)
- ✗ Reject button (thumb down)

---

## 7. Cold Start Handling

### Decision: Graceful Degradation with Feature Flag

When user has no approved expense reports, the prediction feature is silently disabled.

### Rationale

- No patterns = no predictions (mathematically correct)
- Avoid confusing empty states
- Feature becomes available automatically after first report

### Implementation

```csharp
public bool IsPredictionAvailable(Guid userId)
{
    return _patternRepository.GetPatternCountAsync(userId) > 0;
}
```

Frontend checks this before rendering prediction UI.

---

## 8. Privacy Considerations

### Decision: User-Scoped Data with No Cross-User Leakage

All pattern and prediction data is strictly scoped to the owning user.

### Implementation

- All queries filter by `UserId`
- Repository methods require `userId` parameter
- No aggregate queries that could leak patterns
- Patterns not exportable (internal use only)

---

## Summary of Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Pattern extraction | Vendor-centric aggregation | Strongest signal, existing infrastructure |
| Decay formula | 6-month half-life exponential | Captures seasonality, 2x recent weighting |
| Confidence calculation | Multi-signal weighted | Robust, explainable |
| Display threshold | Medium+ (>= 0.50) | Reduces noise per clarification |
| Performance strategy | Eager load + in-memory | O(T) complexity, <5s target |
| Recalculation trigger | Report approval event | Immediate, efficient |
| Feedback mechanism | Incremental Bayesian | Prevents overcorrection |
| Cold start | Graceful degradation | Feature auto-enables with data |
