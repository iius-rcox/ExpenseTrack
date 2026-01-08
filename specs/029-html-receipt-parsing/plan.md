# Implementation Plan: HTML Receipt Parsing

**Branch**: `029-html-receipt-parsing` | **Date**: 2026-01-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/029-html-receipt-parsing/spec.md`

## Summary

Extend the ExpenseFlow receipt processing pipeline to accept HTML files (.html, .htm) as a new input format. HTML receipts from email clients (Amazon, Uber, airline confirmations, etc.) will be processed using AI/LLM-based extraction via Azure OpenAI to extract vendor name, transaction date, total amount, and line items. The system will generate visual thumbnails using HTML-to-image rendering and display sanitized HTML content in the receipt detail view while blocking scripts and external resources for security.

## Technical Context

**Language/Version**: .NET 8 with C# 12
**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core 8, Azure.AI.OpenAI, Microsoft.SemanticKernel, HtmlAgilityPack (HTML parsing/sanitization), PuppeteerSharp or Playwright (HTML-to-image for thumbnails)
**Storage**: PostgreSQL 15+ (Supabase), Azure Blob Storage (ccproctemp2025)
**Testing**: xUnit, Moq, FluentAssertions
**Target Platform**: Azure Kubernetes Service (linux/amd64)
**Project Type**: Web application (backend API + frontend SPA)
**Performance Goals**: Process HTML receipt within 30 seconds, thumbnail generation within 5 seconds
**Constraints**: 5MB max HTML file size, AI extraction via Azure OpenAI gpt-4o-mini
**Scale/Scope**: Extension to existing receipt processing pipeline (~34 receipts currently in system)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Clean Architecture | ✅ PASS | New service in Infrastructure layer, interface in Core |
| II. Test-First Development | ✅ PASS | Unit tests for extraction, integration tests for upload flow |
| III. ERP Integration | N/A | Feature does not interact with Viewpoint Vista |
| IV. API Design | ✅ PASS | Extends existing `/api/receipts` endpoint, uses ProblemDetails |
| V. Observability | ✅ PASS | Serilog structured logging, extraction metrics tracked |
| VI. Security | ✅ PASS | HTML sanitization required, script/external resource blocking |

**Gate Result**: PASS - No violations. Proceed with Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/029-html-receipt-parsing/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
backend/
├── src/
│   ├── ExpenseFlow.Api/
│   │   └── Controllers/
│   │       └── ReceiptsController.cs        # Extend to accept text/html
│   ├── ExpenseFlow.Core/
│   │   ├── Interfaces/
│   │   │   ├── IHtmlReceiptExtractionService.cs  # NEW: HTML extraction interface
│   │   │   └── IHtmlThumbnailService.cs          # NEW: HTML thumbnail interface
│   │   └── Services/
│   │       └── ReceiptService.cs            # Extend allowed content types
│   ├── ExpenseFlow.Infrastructure/
│   │   ├── Services/
│   │   │   ├── HtmlReceiptExtractionService.cs   # NEW: AI-based HTML extraction
│   │   │   ├── HtmlSanitizationService.cs        # NEW: XSS prevention
│   │   │   └── HtmlThumbnailService.cs           # NEW: HTML-to-image rendering
│   │   └── Jobs/
│   │       └── ProcessReceiptJob.cs         # Extend to handle HTML content type
│   └── ExpenseFlow.Shared/
│       └── DTOs/
│           └── HtmlExtractionMetricsDto.cs  # NEW: Extraction logging metrics
└── tests/
    ├── ExpenseFlow.Api.Tests/
    │   └── Controllers/
    │       └── ReceiptsControllerTests.cs   # Add HTML upload tests
    └── ExpenseFlow.Infrastructure.Tests/
        └── Services/
            ├── HtmlReceiptExtractionServiceTests.cs  # NEW
            └── HtmlThumbnailServiceTests.cs          # NEW

frontend/
└── src/
    └── components/
        └── receipts/
            └── receipt-viewer.tsx           # Extend to render sanitized HTML
```

**Structure Decision**: Extends existing web application structure. New services follow established pattern of interface in Core, implementation in Infrastructure.

## Complexity Tracking

No Constitution violations requiring justification.
