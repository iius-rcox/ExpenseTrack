# Implementation Plan: Statement Import & Fingerprinting

**Branch**: `004-statement-fingerprinting` | **Date**: 2025-12-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/004-statement-fingerprinting/spec.md`

## Summary

Enable users to import credit card statements (CSV/Excel) with automatic column detection using a tiered approach: cached fingerprints (Tier 1) for known formats, AI inference via GPT-4o-mini (Tier 3) for unknown formats. User-confirmed mappings are saved for future automatic detection, aligning with the self-improving system principle.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Npgsql, CsvHelper, ClosedXML (Excel), Semantic Kernel, Azure.AI.OpenAI
**Storage**: PostgreSQL 15+ with pgvector (Supabase self-hosted), Azure Blob Storage (ccproctemp2025)
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Azure Kubernetes Service (dev-aks), Kubernetes 1.33.3
**Project Type**: Web application (backend API + React frontend)
**Performance Goals**: Process 1,000 transactions in <10 seconds, fingerprint lookup <100ms
**Constraints**: <30 second import time for known formats, AI inference only when fingerprint miss
**Scale/Scope**: 10-20 users, ~200 transactions per statement, monthly imports

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Cost-First AI Architecture | **PASS** | Tier 1 (fingerprint cache) checked before Tier 3 (GPT-4o-mini). Tier usage logged per FR-013. See Tier 2 Exemption below. |
| II. Self-Improving System | **PASS** | User confirmations create StatementFingerprint entries (future Tier 1). FR-008 explicitly requires this. |
| III. Receipt Accountability | **N/A** | This feature imports transactions; receipt linking is Sprint 5 scope. |
| IV. Infrastructure Optimization | **PASS** | Uses existing AKS, Supabase, Azure OpenAI. No new managed services. |
| V. Cache-First Design | **PASS** | FR-003 requires fingerprint check before AI. Edge case handles AI failure gracefully. |

### Tier 2 Exemption Justification

**Why Tier 2 (Embedding Similarity) is Not Applicable:**

The constitution mandates checking cheaper tiers before expensive ones: Tier 1 → Tier 2 → Tier 3 → Tier 4. This feature skips Tier 2 for the following technical reasons:

1. **Discrete vs. Semantic Matching**: Header hashes are discrete identifiers (SHA-256 of normalized column names). They either match exactly or they don't. Embedding similarity is designed for semantic/fuzzy matching where "similar" has meaning (e.g., "receipt for coffee" vs "Starbucks invoice").

2. **No Semantic Relationship**: Column headers like `"Transaction Date"` and `"TxnDate"` are semantically similar to humans but produce completely different hashes. Embedding similarity would require maintaining a vector database of all possible header variations, which:
   - Adds infrastructure cost (pgvector storage, embedding API calls)
   - Still requires a similarity threshold decision
   - Provides no improvement over the self-improving fingerprint cache

3. **Self-Improvement Already Handles Variations**: When a user imports an unknown format (Tier 3), the confirmed mapping becomes a new Tier 1 fingerprint. Future imports of that exact format hit Tier 1. This is more cost-effective than maintaining embedding vectors for header variations.

4. **Cost Analysis**:
   - Tier 2 embedding call: ~$0.0001 per header set (Ada-002)
   - Tier 3 inference call: ~$0.001 per mapping inference (GPT-4o-mini)
   - For a new format, Tier 2 would still miss (no semantic match) and fall through to Tier 3
   - Net result: Tier 2 adds cost without reducing Tier 3 calls for genuinely new formats

**Conclusion**: For discrete header matching, the Tier 1 → Tier 3 path is more cost-effective than Tier 1 → Tier 2 → Tier 3. The self-improving system principle ensures Tier 3 is only called once per unique header pattern.

**Gate Result**: All applicable principles satisfied. Proceed to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/004-statement-fingerprinting/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── statements-api.yaml
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── StatementsController.cs    # New: Statement import endpoints
│   ├── ExpenseFlow.Core/
│   │   ├── Entities/
│   │   │   ├── StatementFingerprint.cs    # Existing: Extend for system fingerprints
│   │   │   ├── Transaction.cs             # New: Imported transactions
│   │   │   └── StatementImport.cs         # New: Import audit records
│   │   └── Interfaces/
│   │       ├── IStatementFingerprintService.cs   # Existing: Extend interface
│   │       ├── IStatementParsingService.cs       # New: CSV/Excel parsing
│   │       ├── IColumnMappingInferenceService.cs # New: AI column inference
│   │       └── ITransactionRepository.cs         # New: Transaction data access
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Data/
│   │   │   └── Configurations/
│   │   │       ├── TransactionConfiguration.cs      # New
│   │   │       └── StatementImportConfiguration.cs  # New
│   │   ├── Services/
│   │   │   ├── StatementFingerprintService.cs  # Existing: Extend for system fingerprints
│   │   │   ├── StatementParsingService.cs      # New: CSV/Excel parsing
│   │   │   └── ColumnMappingInferenceService.cs # New: AI inference via Semantic Kernel
│   │   └── Repositories/
│   │       └── TransactionRepository.cs         # New
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           ├── StatementAnalyzeRequest.cs       # New
│           ├── StatementAnalyzeResponse.cs      # New
│           ├── StatementImportRequest.cs        # New
│           ├── StatementImportResponse.cs       # New
│           └── ColumnMappingDto.cs              # New
└── tests/
    ├── ExpenseFlow.Core.Tests/
    │   └── Services/
    │       └── StatementParsingServiceTests.cs  # New
    └── ExpenseFlow.Infrastructure.Tests/
        └── Services/
            ├── ColumnMappingInferenceServiceTests.cs # New
            └── TransactionRepositoryTests.cs         # New

frontend/
├── src/
│   ├── components/
│   │   └── statements/
│   │       ├── StatementUpload.tsx           # New: Upload component
│   │       ├── ColumnMappingEditor.tsx       # New: Mapping confirmation UI
│   │       └── ImportSummary.tsx             # New: Results display
│   ├── pages/
│   │   └── StatementImportPage.tsx           # New: Main import page
│   └── services/
│       └── statementService.ts               # New: API client
└── tests/
    └── components/
        └── statements/
            └── ColumnMappingEditor.test.tsx  # New
```

**Structure Decision**: Extends existing web application structure with new controller, entities, services, and frontend components. Follows established Clean Architecture pattern with Core/Infrastructure/Api layers.

## Complexity Tracking

> No constitution violations requiring justification.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| None | - | - |
