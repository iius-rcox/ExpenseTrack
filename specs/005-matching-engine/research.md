# Research: Matching Engine

**Feature**: 005-matching-engine
**Date**: 2025-12-15

## Research Summary

This feature has minimal unknowns since it's a Tier 1 (no AI) implementation using established patterns. Research focused on fuzzy string matching library selection and optimistic locking strategy.

---

## 1. Fuzzy String Matching Library

**Decision**: F23.StringSimilarity (NuGet: `F23.StringSimilarity`)

**Rationale**:
- Pure .NET implementation with no native dependencies
- Implements multiple algorithms: Levenshtein, Jaro-Winkler, Normalized Levenshtein
- Well-maintained with 500k+ downloads
- Returns normalized similarity score (0.0-1.0) matching our 70% threshold requirement
- Significantly faster than rolling our own implementation

**Alternatives Considered**:

| Library | Pros | Cons | Decision |
|---------|------|------|----------|
| F23.StringSimilarity | Fast, multiple algorithms, normalized output | - | ✅ Selected |
| FuzzySharp | More algorithms | Slower, overkill for our needs | ❌ Rejected |
| SimMetrics.Net | Academic-grade | Complex API, heavyweight | ❌ Rejected |
| Custom implementation | Full control | Development time, maintenance burden | ❌ Rejected |

**Usage Pattern**:
```csharp
var levenshtein = new NormalizedLevenshtein();
double similarity = levenshtein.Similarity("DELTA AIR", "Delta Airlines");
// Returns ~0.72 (72% similar)
```

---

## 2. Optimistic Locking Strategy

**Decision**: EF Core RowVersion/ConcurrencyToken

**Rationale**:
- Built into Entity Framework Core - no additional dependencies
- Standard pattern for web applications with concurrent users
- Automatic conflict detection with DbUpdateConcurrencyException
- Works seamlessly with PostgreSQL via xmin system column

**Implementation Pattern**:
```csharp
// Entity
public class ReceiptTransactionMatch : BaseEntity
{
    [Timestamp]
    public uint RowVersion { get; set; }
}

// Configuration
builder.Property(e => e.RowVersion)
    .IsRowVersion()
    .HasColumnName("xmin")
    .HasColumnType("xid");

// Controller conflict handling
catch (DbUpdateConcurrencyException)
{
    return Conflict(new ProblemDetails
    {
        Title = "Match conflict",
        Detail = "This match was modified by another user. Please refresh and try again."
    });
}
```

**Alternatives Considered**:

| Approach | Pros | Cons | Decision |
|----------|------|------|----------|
| EF Core RowVersion | Native, simple, automatic | Requires entity reload on conflict | ✅ Selected |
| Pessimistic locking | Prevents conflicts | Blocks users, timeout complexity | ❌ Rejected |
| Last-write-wins | Simplest | Data loss risk | ❌ Rejected |
| Application-level locking | Custom control | Complex, distributed state | ❌ Rejected |

---

## 3. Confidence Scoring Algorithm

**Decision**: Weighted component scoring (40/35/25)

**Rationale**:
- Amount match (40 points max) - Most reliable indicator
- Date match (35 points max) - Strong correlation but posting delays exist
- Vendor match (25 points max) - Helpful but vendor names vary significantly

**Scoring Breakdown**:

| Component | Condition | Points |
|-----------|-----------|--------|
| **Amount** | Exact match (±$0.10) | 40 |
| | Near match (±$1.00) | 20 |
| | Out of tolerance | 0 |
| **Date** | Same day | 35 |
| | ±1 day | 30 |
| | ±2-3 days | 25 |
| | ±4-7 days | 10 |
| | >7 days | 0 |
| **Vendor** | Exact alias match | 25 |
| | Fuzzy match (>70%) | 15 |
| | No match | 0 |

**Threshold**: Minimum 70 points to propose a match

**Edge Cases**:
- Multiple matches within 5 points: Flag as "ambiguous" for user review
- Refunds/credits: Compare absolute values, maintain sign awareness

---

## 4. Vendor Alias Pattern Extraction

**Decision**: First N words extraction with common suffix removal

**Rationale**:
- Transaction descriptions often include reference numbers: "DELTA AIR 0062363598531"
- Pattern should capture vendor identity, not unique transaction IDs
- Simple regex approach sufficient for Tier 1

**Implementation Pattern**:
```csharp
public string ExtractVendorPattern(string description)
{
    // Remove trailing numbers/codes (likely reference numbers)
    var cleaned = Regex.Replace(description, @"\s*[\d#]+[\dA-Z]*$", "");

    // Take first 3 words max (vendor name)
    var words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    return string.Join(" ", words.Take(3)).ToUpperInvariant();
}

// Examples:
// "DELTA AIR 0062363598531" → "DELTA AIR"
// "MARRIOTT HOTELS NYC 1234" → "MARRIOTT HOTELS NYC"
// "AMAZON.COM*AB1CD2EF3" → "AMAZON.COM*AB1CD2EF3" → needs special handling
```

**Known Patterns to Handle**:
- `AMAZON.COM*xxxx` - Keep as-is (Amazon uses consistent prefix)
- `SQ *MERCHANT` - Square payments, keep SQ prefix
- `PAYPAL *MERCHANT` - PayPal, keep PAYPAL prefix

---

## 5. Background Job Scheduling

**Decision**: Weekly Hangfire recurring job

**Rationale**:
- Alias confidence decay is a maintenance task, not time-critical
- Weekly execution sufficient for 6-month decay window
- Follows existing JobBase pattern in ExpenseFlow.Infrastructure

**Implementation**:
```csharp
// Registration in Program.cs
RecurringJob.AddOrUpdate<AliasConfidenceDecayJob>(
    "alias-confidence-decay",
    job => job.ExecuteAsync(CancellationToken.None),
    Cron.Weekly(DayOfWeek.Sunday, 2, 0)); // 2 AM Sunday

// Job logic
public async Task ExecuteAsync(CancellationToken ct)
{
    var staleThreshold = DateTime.UtcNow.AddMonths(-6);
    var staleAliases = await _dbContext.VendorAliases
        .Where(a => a.LastMatchedAt < staleThreshold && a.Confidence > 0.5m)
        .ToListAsync(ct);

    foreach (var alias in staleAliases)
    {
        alias.Confidence *= 0.9m; // Reduce by 10%
    }

    await _dbContext.SaveChangesAsync(ct);
    _logger.LogInformation("Decayed confidence for {Count} stale aliases", staleAliases.Count);
}
```

---

## Dependencies to Add

```xml
<!-- ExpenseFlow.Infrastructure.csproj -->
<PackageReference Include="F23.StringSimilarity" Version="5.1.0" />
```

No other new dependencies required - all other functionality uses existing packages.

---

## Open Questions Resolved

All technical unknowns have been resolved through this research. No blocking issues identified.
