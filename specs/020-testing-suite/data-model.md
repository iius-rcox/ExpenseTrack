# Data Model: Comprehensive Testing Suite

**Branch**: `020-testing-suite` | **Date**: 2025-12-31
**Phase**: 1 - Design

## Overview

This document defines the test entities, fixtures, and configuration models required for the comprehensive testing suite. These entities support tracking test runs, managing quarantined tests, and providing programmatic test data factories.

---

## 1. Test Tracking Entities

### 1.1 TestRun

Represents a single execution of the test suite in CI/CD.

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Primary key |
| CommitSha | string | Git commit SHA that triggered the run |
| BranchName | string | Git branch name |
| TriggerType | TestTriggerType | Enum: Commit, PullRequest, Nightly, Manual |
| WorkflowName | string | GitHub Actions workflow name |
| Status | TestRunStatus | Enum: Running, Passed, Failed, Cancelled |
| StartedAt | DateTime | When the run started |
| CompletedAt | DateTime? | When the run completed (null if still running) |
| DurationMs | int? | Total duration in milliseconds |
| TotalTests | int | Total number of tests executed |
| PassedCount | int | Number of passed tests |
| FailedCount | int | Number of failed tests |
| SkippedCount | int | Number of skipped tests |
| CoveragePercentage | decimal? | Code coverage percentage (if collected) |
| ArtifactUrl | string? | URL to test artifacts (logs, reports) |
| GithubRunId | long | GitHub Actions run ID for correlation |

**Relationships**:
- One-to-many with TestResult

### 1.2 TestResult

Individual test outcome within a test run.

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Primary key |
| TestRunId | Guid | Foreign key to TestRun |
| TestName | string | Fully qualified test name |
| TestClassName | string | Test class name |
| TestMethodName | string | Test method name |
| TestCategory | TestCategory | Enum: Unit, Integration, Contract, Property, Scenario, E2E |
| Status | TestResultStatus | Enum: Passed, Failed, Skipped, Quarantined |
| DurationMs | int | Test execution duration |
| ErrorMessage | string? | Error message if failed |
| StackTrace | string? | Stack trace if failed |
| RetryCount | int | Number of retries attempted (per FR-017) |
| OutputLog | string? | Test output/console log |

**Relationships**:
- Many-to-one with TestRun

### 1.3 QuarantinedTest

Test flagged as flaky after inconsistent retry results (per FR-017).

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Primary key |
| TestName | string | Fully qualified test name |
| TestClassName | string | Test class name |
| QuarantinedAt | DateTime | When the test was quarantined |
| QuarantinedBy | string | Username or "system" for auto-quarantine |
| Reason | string | Why the test was quarantined |
| FailurePattern | string? | JSON describing failure pattern |
| LastFailureDate | DateTime | Most recent failure date |
| FailureCount | int | Total failure count in quarantine |
| AssociatedPullRequest | string? | PR number where first identified |
| IsActive | bool | Whether still quarantined |
| ResolvedAt | DateTime? | When removed from quarantine |
| ResolvedBy | string? | Who resolved it |

**Indexes**:
- Unique on TestName (only one quarantine per test)
- Index on IsActive for filtering

### 1.4 TestArtifact

Files generated during testing (per FR-014).

| Property | Type | Description |
|----------|------|-------------|
| Id | Guid | Primary key |
| TestRunId | Guid | Foreign key to TestRun |
| TestResultId | Guid? | Optional FK to specific test result |
| ArtifactType | ArtifactType | Enum: Log, Screenshot, CoverageReport, TraceFile |
| FileName | string | Original file name |
| BlobUrl | string | Azure Blob Storage URL |
| ContentType | string | MIME type |
| FileSizeBytes | long | File size in bytes |
| CreatedAt | DateTime | When artifact was uploaded |
| ExpiresAt | DateTime | When artifact will be deleted (7 days per FR-014) |

**Relationships**:
- Many-to-one with TestRun
- Optional many-to-one with TestResult

---

## 2. Enumerations

### 2.1 TestTriggerType
```csharp
public enum TestTriggerType
{
    Commit = 1,      // Triggered by push to any branch
    PullRequest = 2, // Triggered by PR creation/update
    Nightly = 3,     // Scheduled nightly run
    Manual = 4       // Manually triggered
}
```

### 2.2 TestRunStatus
```csharp
public enum TestRunStatus
{
    Queued = 0,      // Waiting to start
    Running = 1,     // Currently executing
    Passed = 2,      // All tests passed
    Failed = 3,      // One or more tests failed
    Cancelled = 4,   // Run was cancelled (e.g., superseded)
    TimedOut = 5     // Exceeded timeout limit
}
```

### 2.3 TestCategory
```csharp
public enum TestCategory
{
    Unit = 1,        // Isolated unit tests (no dependencies)
    Integration = 2, // Tests with real dependencies (DB, services)
    Contract = 3,    // OpenAPI contract validation tests
    Property = 4,    // FsCheck property-based tests
    Scenario = 5,    // End-to-end scenario tests
    E2E = 6,         // Full UI end-to-end (Playwright)
    Load = 7,        // Performance/load tests (NBomber)
    Chaos = 8        // Chaos engineering tests
}
```

### 2.4 TestResultStatus
```csharp
public enum TestResultStatus
{
    Passed = 1,
    Failed = 2,
    Skipped = 3,
    Quarantined = 4  // Failed but allowed to proceed (FR-017)
}
```

### 2.5 ArtifactType
```csharp
public enum ArtifactType
{
    Log = 1,
    Screenshot = 2,
    CoverageReport = 3,
    TraceFile = 4,
    Video = 5
}
```

---

## 3. Test Fixture Definitions

### 3.1 TestFixture (Configuration Model)

Represents a reusable test data factory (per FR-018).

| Property | Type | Description |
|----------|------|-------------|
| Name | string | Fixture identifier (e.g., "ReceiptWithMatchingTransaction") |
| Description | string | What this fixture sets up |
| EntityTypes | string[] | Entity types created by this fixture |
| DependsOn | string[] | Other fixtures that must run first |
| SeedDataPath | string? | Path to seed data JSON file |
| CleanupStrategy | CleanupStrategy | How to clean up after tests |

### 3.2 CleanupStrategy
```csharp
public enum CleanupStrategy
{
    TransactionRollback = 1,  // Rollback DB transaction (fastest)
    DeleteByTestRunId = 2,    // Delete rows with test prefix
    TruncateTables = 3,       // Truncate affected tables
    ResetDatabase = 4         // Full database reset (slowest)
}
```

---

## 4. Test Data Builders

### 4.1 Builder Interfaces

```csharp
public interface ITestDataBuilder<T>
{
    T Build();
    Task<T> CreateAsync(ExpenseFlowDbContext db);
}
```

### 4.2 Standard Fixtures

| Fixture Name | Entities Created | Purpose |
|--------------|------------------|---------|
| `EmptyDatabase` | None | Clean slate for isolation |
| `SingleUser` | User | Single test user with preferences |
| `UserWithReceipts` | User, Receipt(5) | User with sample receipts |
| `MatchingScenario` | User, Receipt(3), Transaction(5) | Test matching algorithm |
| `CategorizationScenario` | User, Transaction(10), ExpenseEmbedding(10) | Test Tier 1/2/3 |
| `ReportScenario` | User, ExpenseReport, LineItem(10) | Test report generation |
| `SubscriptionScenario` | User, Transaction(24 months) | Test subscription detection |

### 4.3 Fixture Implementation Pattern

```csharp
public class MatchingScenarioFixture : ITestFixture
{
    public string Name => "MatchingScenario";
    public string[] EntityTypes => ["User", "Receipt", "Transaction"];
    public string[] DependsOn => [];
    public CleanupStrategy CleanupStrategy => CleanupStrategy.TransactionRollback;

    public Guid TestUserId { get; private set; }
    public List<Receipt> Receipts { get; private set; } = [];
    public List<Transaction> Transactions { get; private set; } = [];

    public async Task SeedAsync(ExpenseFlowDbContext db)
    {
        TestUserId = Guid.NewGuid();

        // Create user
        var user = new UserBuilder()
            .WithId(TestUserId)
            .WithEmail($"test-{TestUserId}@expenseflow.test")
            .Build();
        db.Users.Add(user);

        // Create receipts with known data
        Receipts.Add(await new ReceiptBuilder()
            .ForUser(TestUserId)
            .WithVendor("AMAZON.COM")
            .WithTotal(99.99m)
            .WithDate(DateTime.Today.AddDays(-5))
            .CreateAsync(db));

        // Create transactions with matching and non-matching data
        Transactions.Add(await new TransactionBuilder()
            .ForUser(TestUserId)
            .WithDescription("Amazon Marketplace")
            .WithAmount(99.99m)
            .WithDate(DateTime.Today.AddDays(-5))
            .CreateAsync(db));

        await db.SaveChangesAsync();
    }

    public async Task CleanupAsync(ExpenseFlowDbContext db)
    {
        // Delete in dependency order
        await db.Transactions.Where(t => t.UserId == TestUserId).ExecuteDeleteAsync();
        await db.Receipts.Where(r => r.UserId == TestUserId).ExecuteDeleteAsync();
        await db.Users.Where(u => u.Id == TestUserId).ExecuteDeleteAsync();
    }
}
```

---

## 5. Configuration Models

### 5.1 TestConfiguration

Runtime configuration for test execution.

```csharp
public class TestConfiguration
{
    public string DatabaseConnectionString { get; set; }
    public string WireMockBaseUrl { get; set; }
    public bool EnableChaosEngineering { get; set; }
    public double ChaosInjectionRate { get; set; }
    public int FlakeyTestMaxRetries { get; set; } = 2;  // FR-017
    public decimal CoverageThreshold { get; set; } = 0.80m;  // FR-015
    public TimeSpan TestTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
```

### 5.2 ChaosConfiguration

Configuration for chaos engineering tests.

```csharp
public class ChaosConfiguration
{
    public bool Enabled { get; set; }
    public double FaultInjectionRate { get; set; } = 0.05;  // 5%
    public TimeSpan MaxLatencyInjection { get; set; } = TimeSpan.FromSeconds(5);
    public string[] TargetServices { get; set; } =
        ["DocumentIntelligence", "OpenAI", "VistaERP"];
}
```

---

## 6. Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────┐
│    TestRun      │       │  TestResult     │
├─────────────────┤       ├─────────────────┤
│ Id (PK)         │──────<│ Id (PK)         │
│ CommitSha       │       │ TestRunId (FK)  │
│ BranchName      │       │ TestName        │
│ TriggerType     │       │ Status          │
│ Status          │       │ DurationMs      │
│ StartedAt       │       │ ErrorMessage    │
│ ...             │       │ RetryCount      │
└─────────────────┘       └─────────────────┘
         │                         │
         │                         │
         ▼                         ▼
┌─────────────────┐       ┌─────────────────┐
│  TestArtifact   │       │ QuarantinedTest │
├─────────────────┤       ├─────────────────┤
│ Id (PK)         │       │ Id (PK)         │
│ TestRunId (FK)  │       │ TestName (UK)   │
│ TestResultId    │       │ QuarantinedAt   │
│ ArtifactType    │       │ Reason          │
│ BlobUrl         │       │ IsActive        │
│ ExpiresAt       │       │ ...             │
└─────────────────┘       └─────────────────┘
```

---

## 7. Database Migrations

### 7.1 New Tables

```sql
-- TestRuns table
CREATE TABLE "TestRuns" (
    "Id" uuid PRIMARY KEY,
    "CommitSha" varchar(40) NOT NULL,
    "BranchName" varchar(255) NOT NULL,
    "TriggerType" integer NOT NULL,
    "WorkflowName" varchar(100) NOT NULL,
    "Status" integer NOT NULL,
    "StartedAt" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone,
    "DurationMs" integer,
    "TotalTests" integer NOT NULL DEFAULT 0,
    "PassedCount" integer NOT NULL DEFAULT 0,
    "FailedCount" integer NOT NULL DEFAULT 0,
    "SkippedCount" integer NOT NULL DEFAULT 0,
    "CoveragePercentage" decimal(5,2),
    "ArtifactUrl" text,
    "GithubRunId" bigint NOT NULL
);

CREATE INDEX "IX_TestRuns_CommitSha" ON "TestRuns" ("CommitSha");
CREATE INDEX "IX_TestRuns_StartedAt" ON "TestRuns" ("StartedAt" DESC);

-- TestResults table
CREATE TABLE "TestResults" (
    "Id" uuid PRIMARY KEY,
    "TestRunId" uuid NOT NULL REFERENCES "TestRuns"("Id") ON DELETE CASCADE,
    "TestName" varchar(500) NOT NULL,
    "TestClassName" varchar(255) NOT NULL,
    "TestMethodName" varchar(255) NOT NULL,
    "TestCategory" integer NOT NULL,
    "Status" integer NOT NULL,
    "DurationMs" integer NOT NULL,
    "ErrorMessage" text,
    "StackTrace" text,
    "RetryCount" integer NOT NULL DEFAULT 0,
    "OutputLog" text
);

CREATE INDEX "IX_TestResults_TestRunId" ON "TestResults" ("TestRunId");
CREATE INDEX "IX_TestResults_Status" ON "TestResults" ("Status");

-- QuarantinedTests table
CREATE TABLE "QuarantinedTests" (
    "Id" uuid PRIMARY KEY,
    "TestName" varchar(500) NOT NULL UNIQUE,
    "TestClassName" varchar(255) NOT NULL,
    "QuarantinedAt" timestamp with time zone NOT NULL,
    "QuarantinedBy" varchar(100) NOT NULL,
    "Reason" text NOT NULL,
    "FailurePattern" jsonb,
    "LastFailureDate" timestamp with time zone NOT NULL,
    "FailureCount" integer NOT NULL DEFAULT 1,
    "AssociatedPullRequest" varchar(50),
    "IsActive" boolean NOT NULL DEFAULT true,
    "ResolvedAt" timestamp with time zone,
    "ResolvedBy" varchar(100)
);

CREATE INDEX "IX_QuarantinedTests_IsActive" ON "QuarantinedTests" ("IsActive") WHERE "IsActive" = true;

-- TestArtifacts table
CREATE TABLE "TestArtifacts" (
    "Id" uuid PRIMARY KEY,
    "TestRunId" uuid NOT NULL REFERENCES "TestRuns"("Id") ON DELETE CASCADE,
    "TestResultId" uuid REFERENCES "TestResults"("Id") ON DELETE SET NULL,
    "ArtifactType" integer NOT NULL,
    "FileName" varchar(255) NOT NULL,
    "BlobUrl" text NOT NULL,
    "ContentType" varchar(100) NOT NULL,
    "FileSizeBytes" bigint NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL
);

CREATE INDEX "IX_TestArtifacts_TestRunId" ON "TestArtifacts" ("TestRunId");
CREATE INDEX "IX_TestArtifacts_ExpiresAt" ON "TestArtifacts" ("ExpiresAt");
```

---

## 8. Notes

1. **Test tracking entities are optional** - These tables support test visibility (User Story 4) but are not required for core testing functionality
2. **Fixtures are in-memory configurations** - Not stored in database, defined in code
3. **7-day retention** - Per FR-014, artifacts older than 7 days should be deleted by a scheduled cleanup job
4. **Quarantine is automatic** - Per FR-017, tests that fail inconsistently after 2 retries are auto-quarantined
