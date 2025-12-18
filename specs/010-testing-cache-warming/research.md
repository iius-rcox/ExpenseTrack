# Research: Testing & Cache Warming

**Feature Branch**: `010-testing-cache-warming`
**Date**: 2025-12-17
**Status**: Complete

## Research Topics

### 1. Historical Data Import Format

**Decision**: Support Excel (.xlsx) format as primary import format using ClosedXML

**Rationale**:
- Historical expense reports from the existing system are exported as Excel files
- ClosedXML is already a project dependency (used for Excel export in Sprint 9)
- Excel format preserves data types and handles special characters better than CSV
- Supports multiple sheets (e.g., summary + line items)

**Alternatives Considered**:
- CSV: Rejected due to encoding issues, no standard for embedded commas, and multiple files needed
- JSON: Rejected as not a typical export format from legacy expense systems
- Direct database connection: Rejected due to security and access complexity

**Import Schema Expected**:
```
Column: Expense Date | Description | Vendor | Amount | GL Code | Department | Notes
Type:   Date        | Text        | Text   | Number | Text    | Text       | Text
```

### 2. Load Testing Framework Selection

**Decision**: Use NBomber for .NET-native load testing

**Rationale**:
- NBomber is a .NET library that integrates naturally with the existing test infrastructure
- Supports scenario-based testing matching UAT test cases
- Can be run as part of CI/CD pipeline
- Generates detailed reports with percentile breakdowns
- No external tooling required (unlike k6 which requires separate installation)

**Alternatives Considered**:
- k6: Powerful but requires JavaScript/TypeScript, separate installation, and different toolchain
- Apache JMeter: Heavy-weight, GUI-focused, poor .NET integration
- Artillery: Node.js based, requires separate installation
- BenchmarkDotNet: Better for micro-benchmarks, not HTTP load testing

**NBomber Implementation Pattern**:
```csharp
var scenario = Scenario.Create("upload_receipt", async context =>
{
    var response = await httpClient.PostAsync("/api/receipts", content);
    return response.IsSuccessStatusCode ? Response.Ok() : Response.Fail();
})
.WithLoadSimulations(
    Simulation.Inject(rate: 20, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(5))
);
```

### 3. Staging Environment Isolation Strategy

**Decision**: Use Kubernetes namespace isolation with separate Supabase instance

**Rationale**:
- `expenseflow-staging` namespace already exists
- Namespace provides network isolation via NetworkPolicy
- Separate Supabase deployment ensures complete data isolation
- Same container images as production (just different config)
- Can use same Azure resources (ACR, Key Vault) with different secrets

**Alternatives Considered**:
- Schema-level isolation: Rejected - risk of accidental cross-contamination
- Feature flags: Rejected - doesn't isolate data, only code paths
- Separate AKS cluster: Overkill for 10-20 user application

**Staging Architecture**:
```
expenseflow-staging namespace:
├── api-deployment (same image as dev/prod)
├── supabase-staging (separate PostgreSQL instance)
├── configmap-staging (staging-specific config)
└── secret-staging (staging credentials from Key Vault)
```

### 4. Cache Warming Batch Processing Strategy

**Decision**: Use Hangfire background job with chunked processing

**Rationale**:
- Hangfire is already configured in the project for background jobs
- Chunked processing (100 records at a time) prevents memory issues
- Progress tracking through ImportJob entity allows UI updates
- Retries handled automatically by Hangfire
- Can be scheduled or triggered on-demand

**Alternatives Considered**:
- Synchronous processing: Rejected - would timeout for large imports
- Azure Functions: Rejected - adds infrastructure complexity for one-time operation
- Parallel.ForEach: Rejected - would overwhelm embedding API rate limits

**Processing Flow**:
```
1. Upload Excel file → Blob Storage
2. Create ImportJob record (status: Pending)
3. Queue Hangfire job with ImportJob ID
4. Background job:
   a. Read Excel from Blob Storage
   b. For each chunk of 100 records:
      - Parse and validate
      - Deduplicate against existing cache
      - Generate embeddings (batched API calls)
      - Insert/update DescriptionCache, VendorAliases, ExpenseEmbeddings
      - Update ImportJob progress
   c. Mark ImportJob as Complete
5. Return statistics to user
```

### 5. Embedding Generation Cost Control

**Decision**: Batch embedding requests (up to 100 texts per API call) with rate limiting

**Rationale**:
- Azure OpenAI text-embedding-3-small supports batch requests
- Reduces API calls by 100x compared to single-text requests
- Rate limiting prevents 429 errors
- Total cost estimate: ~500 embeddings × $0.00002 = $0.01 (well under $10 budget)

**Alternatives Considered**:
- Single-text requests: Rejected - 500+ API calls vs 5-6 batched calls
- Pre-computed embeddings from another source: Not available
- Skip embeddings for duplicates: Already implemented via deduplication

**Batch API Pattern**:
```csharp
// Batch up to 100 descriptions per API call
var embeddings = await _embeddingService.GenerateEmbeddingsAsync(
    descriptions.Take(100).ToList()
);
```

### 6. UAT Test Case Management

**Decision**: Use markdown files in repository for test case documentation

**Rationale**:
- Version controlled alongside code
- No additional tooling required
- Easy for testers to read and update
- Can be converted to other formats if needed later
- Defects tracked via GitHub Issues with links to test cases

**Alternatives Considered**:
- TestRail/Zephyr: Overkill for 7 test cases, additional cost
- Spreadsheet: Version control issues, harder to link to code
- JIRA Test Management: Not using JIRA, would require new tooling

**Test Case Template**:
```markdown
# TC-001: Receipt Upload Flow
## Preconditions
- User is authenticated
- No existing receipts for test period

## Steps
1. Navigate to Upload page
2. Select PDF receipt file
3. Click Upload
4. Wait for processing

## Expected Results
- Receipt appears in list with status "Processing"
- Status changes to "Ready" within 30 seconds
- Extracted vendor, date, amount displayed correctly

## Actual Results
[Filled during testing]

## Status: [Pass/Fail]
## Defects: [Link to GitHub Issue if any]
```

### 7. Query Performance Monitoring

**Decision**: Use PostgreSQL pg_stat_statements extension with EF Core logging

**Rationale**:
- pg_stat_statements is built into PostgreSQL, no additional cost
- EF Core can log query execution times with MinimumLevel.Information
- Combined approach catches both ORM-generated and raw queries
- Can export to Azure Monitor for dashboard

**Alternatives Considered**:
- Application Insights: Already available via Container Insights, but adds SDK overhead
- MiniProfiler: Good for dev, but EF Core logging sufficient for this use case
- Custom middleware: Reinventing what EF Core already provides

**Query Logging Configuration**:
```csharp
// In Program.cs for staging/perf testing
optionsBuilder.LogTo(
    Console.WriteLine,
    new[] { DbLoggerCategory.Database.Command.Name },
    LogLevel.Information
);
```

## Summary

All research topics resolved. No NEEDS CLARIFICATION items remain.

| Topic | Decision | Key Benefit |
|-------|----------|-------------|
| Import Format | Excel via ClosedXML | Reuses existing dependency, handles encoding |
| Load Testing | NBomber | .NET native, no external tools |
| Staging Isolation | K8s namespace + separate Supabase | Complete data isolation |
| Batch Processing | Hangfire + chunks | Memory safe, progress tracking |
| Embedding Costs | Batched API calls | 100x fewer API calls |
| UAT Management | Markdown in repo | Version controlled, no new tools |
| Query Monitoring | pg_stat_statements + EF logging | Built-in, no cost |
