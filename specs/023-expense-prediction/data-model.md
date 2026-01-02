# Data Model: Expense Prediction

**Feature**: 023-expense-prediction
**Date**: 2026-01-02

## Entity Relationship Diagram

```text
┌──────────────────┐       ┌──────────────────────┐       ┌─────────────────────┐
│   ExpenseReport  │       │    ExpensePattern    │       │ TransactionPrediction│
│   (existing)     │       │      (NEW)           │       │       (NEW)         │
├──────────────────┤       ├──────────────────────┤       ├─────────────────────┤
│ Id               │       │ Id                   │       │ Id                  │
│ UserId ──────────┼──────▶│ UserId               │◀──────│ PatternId           │
│ Status           │       │ NormalizedVendor     │       │ TransactionId ──────┼──▶ Transaction
│ ...              │       │ DisplayName          │       │ UserId              │
└────────┬─────────┘       │ Category             │       │ ConfidenceScore     │
         │                 │ AverageAmount        │       │ ConfidenceLevel     │
         │                 │ MinAmount            │       │ Status              │
         ▼                 │ MaxAmount            │       │ CreatedAt           │
┌──────────────────┐       │ OccurrenceCount      │       │ ResolvedAt          │
│   ExpenseLine    │       │ LastSeenAt           │       └─────────────────────┘
│   (existing)     │       │ DefaultGLCode        │
├──────────────────┤       │ DefaultDepartment    │       ┌─────────────────────┐
│ Id               │       │ ConfirmCount         │       │  PredictionFeedback │
│ VendorName       │       │ RejectCount          │       │       (NEW)         │
│ GLCode           │       │ IsSuppressed         │       ├─────────────────────┤
│ DepartmentCode   │       │ CreatedAt            │       │ Id                  │
│ Amount           │       │ UpdatedAt            │       │ PredictionId ───────┼──▶ TransactionPrediction
│ ...              │       └──────────────────────┘       │ UserId              │
└──────────────────┘                                       │ FeedbackType        │
                                                          │ CreatedAt           │
                                                          └─────────────────────┘
```

---

## Entity Definitions

### ExpensePattern (NEW)

Represents a learned expense pattern for a user, derived from approved expense reports.

```csharp
namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Learned expense pattern extracted from user's approved expense reports.
/// Used to predict which future transactions are likely business expenses.
/// </summary>
public class ExpensePattern : BaseEntity
{
    /// <summary>FK to Users - pattern owner</summary>
    public Guid UserId { get; set; }

    /// <summary>Normalized vendor name (via VendorAlias lookup)</summary>
    public string NormalizedVendor { get; set; } = string.Empty;

    /// <summary>Human-readable vendor name for UI display</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Most common category for this vendor's expenses</summary>
    public string? Category { get; set; }

    /// <summary>Running weighted average of expense amounts</summary>
    public decimal AverageAmount { get; set; }

    /// <summary>Minimum amount seen for this vendor</summary>
    public decimal MinAmount { get; set; }

    /// <summary>Maximum amount seen for this vendor</summary>
    public decimal MaxAmount { get; set; }

    /// <summary>Number of times this vendor appeared in expense reports</summary>
    public int OccurrenceCount { get; set; }

    /// <summary>Timestamp of most recent occurrence</summary>
    public DateTimeOffset LastSeenAt { get; set; }

    /// <summary>Most commonly used GL code for this vendor</summary>
    public string? DefaultGLCode { get; set; }

    /// <summary>Most commonly used department for this vendor</summary>
    public string? DefaultDepartment { get; set; }

    /// <summary>Number of times user confirmed predictions for this pattern</summary>
    public int ConfirmCount { get; set; }

    /// <summary>Number of times user rejected predictions for this pattern</summary>
    public int RejectCount { get; set; }

    /// <summary>True if user explicitly excluded this vendor from predictions</summary>
    public bool IsSuppressed { get; set; }

    /// <summary>Pattern creation timestamp</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Last update timestamp</summary>
    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<TransactionPrediction> Predictions { get; set; } = new List<TransactionPrediction>();
}
```

### TransactionPrediction (NEW)

Represents a prediction that a specific transaction is a business expense.

```csharp
namespace ExpenseFlow.Core.Entities;

/// <summary>
/// Prediction that a transaction is likely a business expense.
/// Links a transaction to the pattern that generated the prediction.
/// </summary>
public class TransactionPrediction : BaseEntity
{
    /// <summary>FK to ExpensePatterns - the pattern that matched</summary>
    public Guid PatternId { get; set; }

    /// <summary>FK to Transactions - the predicted transaction</summary>
    public Guid TransactionId { get; set; }

    /// <summary>FK to Users - for efficient user-scoped queries</summary>
    public Guid UserId { get; set; }

    /// <summary>Calculated confidence score (0.00 - 1.00)</summary>
    public decimal ConfidenceScore { get; set; }

    /// <summary>Confidence level: High, Medium, or Low</summary>
    public PredictionConfidence ConfidenceLevel { get; set; }

    /// <summary>Prediction status: Pending, Confirmed, Rejected, Ignored</summary>
    public PredictionStatus Status { get; set; } = PredictionStatus.Pending;

    /// <summary>Prediction creation timestamp</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When user acted on prediction (confirm/reject)</summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    // Navigation properties
    public ExpensePattern Pattern { get; set; } = null!;
    public Transaction Transaction { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

### PredictionFeedback (NEW)

Captures explicit user feedback on predictions.

```csharp
namespace ExpenseFlow.Core.Entities;

/// <summary>
/// User feedback on a specific prediction.
/// Used for observability metrics and pattern improvement.
/// </summary>
public class PredictionFeedback : BaseEntity
{
    /// <summary>FK to TransactionPredictions</summary>
    public Guid PredictionId { get; set; }

    /// <summary>FK to Users</summary>
    public Guid UserId { get; set; }

    /// <summary>Feedback type: Confirmed or Rejected</summary>
    public FeedbackType FeedbackType { get; set; }

    /// <summary>Feedback timestamp</summary>
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation properties
    public TransactionPrediction Prediction { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

---

## Enums

```csharp
namespace ExpenseFlow.Shared.Enums;

/// <summary>
/// Confidence level for expense predictions.
/// </summary>
public enum PredictionConfidence
{
    Low = 0,      // < 0.50 (suppressed from display)
    Medium = 1,   // 0.50 - 0.74
    High = 2      // >= 0.75
}

/// <summary>
/// Status of a transaction prediction.
/// </summary>
public enum PredictionStatus
{
    Pending = 0,   // Awaiting user action
    Confirmed = 1, // User confirmed as expense
    Rejected = 2,  // User rejected as not expense
    Ignored = 3    // User took no action (implicit ignore)
}

/// <summary>
/// Type of user feedback on a prediction.
/// </summary>
public enum FeedbackType
{
    Confirmed = 1,
    Rejected = 2
}
```

---

## EF Core Configurations

### ExpensePatternConfiguration

```csharp
namespace ExpenseFlow.Infrastructure.Data.Configurations;

public class ExpensePatternConfiguration : IEntityTypeConfiguration<ExpensePattern>
{
    public void Configure(EntityTypeBuilder<ExpensePattern> builder)
    {
        builder.ToTable("expense_patterns");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.NormalizedVendor)
            .HasColumnName("normalized_vendor")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasMaxLength(100);

        builder.Property(e => e.AverageAmount)
            .HasColumnName("average_amount")
            .HasPrecision(18, 2);

        builder.Property(e => e.MinAmount)
            .HasColumnName("min_amount")
            .HasPrecision(18, 2);

        builder.Property(e => e.MaxAmount)
            .HasColumnName("max_amount")
            .HasPrecision(18, 2);

        builder.Property(e => e.OccurrenceCount)
            .HasColumnName("occurrence_count");

        builder.Property(e => e.LastSeenAt)
            .HasColumnName("last_seen_at");

        builder.Property(e => e.DefaultGLCode)
            .HasColumnName("default_gl_code")
            .HasMaxLength(50);

        builder.Property(e => e.DefaultDepartment)
            .HasColumnName("default_department")
            .HasMaxLength(50);

        builder.Property(e => e.ConfirmCount)
            .HasColumnName("confirm_count");

        builder.Property(e => e.RejectCount)
            .HasColumnName("reject_count");

        builder.Property(e => e.IsSuppressed)
            .HasColumnName("is_suppressed");

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Unique constraint: one pattern per vendor per user
        builder.HasIndex(e => new { e.UserId, e.NormalizedVendor })
            .IsUnique();

        // Index for efficient user queries
        builder.HasIndex(e => e.UserId);

        // Relationship
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### TransactionPredictionConfiguration

```csharp
namespace ExpenseFlow.Infrastructure.Data.Configurations;

public class TransactionPredictionConfiguration : IEntityTypeConfiguration<TransactionPrediction>
{
    public void Configure(EntityTypeBuilder<TransactionPrediction> builder)
    {
        builder.ToTable("transaction_predictions");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.PatternId)
            .HasColumnName("pattern_id")
            .IsRequired();

        builder.Property(e => e.TransactionId)
            .HasColumnName("transaction_id")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.ConfidenceScore)
            .HasColumnName("confidence_score")
            .HasPrecision(5, 4);

        builder.Property(e => e.ConfidenceLevel)
            .HasColumnName("confidence_level")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(e => e.ResolvedAt)
            .HasColumnName("resolved_at");

        // Unique: one prediction per transaction
        builder.HasIndex(e => e.TransactionId)
            .IsUnique();

        // Index for user queries
        builder.HasIndex(e => e.UserId);

        // Index for pending predictions
        builder.HasIndex(e => new { e.UserId, e.Status });

        // Relationships
        builder.HasOne(e => e.Pattern)
            .WithMany(p => p.Predictions)
            .HasForeignKey(e => e.PatternId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Transaction)
            .WithMany()
            .HasForeignKey(e => e.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### PredictionFeedbackConfiguration

```csharp
namespace ExpenseFlow.Infrastructure.Data.Configurations;

public class PredictionFeedbackConfiguration : IEntityTypeConfiguration<PredictionFeedback>
{
    public void Configure(EntityTypeBuilder<PredictionFeedback> builder)
    {
        builder.ToTable("prediction_feedback");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.PredictionId)
            .HasColumnName("prediction_id")
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.FeedbackType)
            .HasColumnName("feedback_type")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at");

        // Index for analytics
        builder.HasIndex(e => new { e.UserId, e.CreatedAt });

        // Relationships
        builder.HasOne(e => e.Prediction)
            .WithMany()
            .HasForeignKey(e => e.PredictionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

---

## Migration SQL (PostgreSQL)

```sql
-- Create expense_patterns table
CREATE TABLE expense_patterns (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    normalized_vendor VARCHAR(255) NOT NULL,
    display_name VARCHAR(255) NOT NULL,
    category VARCHAR(100),
    average_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    min_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    max_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    occurrence_count INT NOT NULL DEFAULT 0,
    last_seen_at TIMESTAMPTZ NOT NULL,
    default_gl_code VARCHAR(50),
    default_department VARCHAR(50),
    confirm_count INT NOT NULL DEFAULT 0,
    reject_count INT NOT NULL DEFAULT 0,
    is_suppressed BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, normalized_vendor)
);

CREATE INDEX idx_expense_patterns_user_id ON expense_patterns(user_id);

-- Create transaction_predictions table
CREATE TABLE transaction_predictions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    pattern_id UUID NOT NULL REFERENCES expense_patterns(id) ON DELETE CASCADE,
    transaction_id UUID NOT NULL REFERENCES transactions(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    confidence_score DECIMAL(5,4) NOT NULL,
    confidence_level VARCHAR(20) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at TIMESTAMPTZ,
    UNIQUE (transaction_id)
);

CREATE INDEX idx_transaction_predictions_user_id ON transaction_predictions(user_id);
CREATE INDEX idx_transaction_predictions_user_status ON transaction_predictions(user_id, status);

-- Create prediction_feedback table
CREATE TABLE prediction_feedback (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    prediction_id UUID NOT NULL REFERENCES transaction_predictions(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    feedback_type VARCHAR(20) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_prediction_feedback_user_created ON prediction_feedback(user_id, created_at);
```

---

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| ExpensePattern | NormalizedVendor | Required, max 255 chars |
| ExpensePattern | AverageAmount | >= 0 |
| ExpensePattern | OccurrenceCount | >= 1 |
| TransactionPrediction | ConfidenceScore | 0.00 - 1.00 |
| TransactionPrediction | ConfidenceLevel | Must match score range |
| PredictionFeedback | FeedbackType | Confirmed or Rejected |
