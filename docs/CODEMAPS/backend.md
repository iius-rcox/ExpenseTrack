# ExpenseTrack Backend Architecture

**Last Updated**: 2026-01-25
**Token Estimate**: ~1200 tokens

---

## Project Structure

```
backend/
├── src/
│   ├── ExpenseFlow.Api/           # REST API layer
│   │   ├── Controllers/           # 13 controllers, 25 endpoints
│   │   └── Program.cs             # App configuration
│   ├── ExpenseFlow.Core/          # Domain layer
│   │   ├── Entities/              # 33 domain entities
│   │   ├── Interfaces/            # Service contracts
│   │   └── Services/              # Domain services
│   ├── ExpenseFlow.Infrastructure/# Infrastructure layer
│   │   ├── Data/                  # EF Core DbContext
│   │   ├── Services/              # 42 infrastructure services
│   │   ├── Jobs/                  # Hangfire background jobs
│   │   └── Repositories/          # Data access
│   └── ExpenseFlow.Shared/        # Cross-cutting concerns
│       ├── DTOs/                  # API contracts
│       └── Enums/                 # Domain enumerations
└── tests/
    ├── ExpenseFlow.Core.Tests/
    ├── ExpenseFlow.Api.Tests/
    ├── ExpenseFlow.Infrastructure.Tests/
    └── ExpenseFlow.Scenarios.Tests/
```

## API Controllers

| Controller | Endpoints | Purpose |
|------------|-----------|---------|
| `ReceiptsController` | 8 | Upload, extraction, thumbnails |
| `MatchingController` | 6 | Receipt-transaction matching |
| `TransactionsController` | 5 | Transaction CRUD, filtering |
| `ReportsController` | 7 | Report generation, export |
| `AnalyticsController` | 4 | Analytics & insights |
| `DashboardController` | 3 | Dashboard widgets |
| `CategorizationController` | 3 | AI categorization |
| `PredictionsController` | 4 | Expense predictions |
| `MissingReceiptsController` | 3 | Missing receipt detection |
| `TransactionGroupsController` | 4 | Transaction grouping |
| `ReportJobsController` | 3 | Async job status |
| `ExtractionCorrectionsController` | 4 | ML training data |
| `ReferenceDataController` | 3 | GL/Dept/Project lookups |

## Key Services

### Receipt Processing Pipeline
```
ReceiptService
├── DocumentIntelligenceService    # Azure AI OCR
├── HtmlReceiptExtractionService   # HTML email receipts
├── ThumbnailGenerationService     # Preview generation
└── HeicConversionService          # Apple format support
```

### Matching & Categorization
```
MatchingService
├── FuzzyMatchingService           # Levenshtein vendor matching
├── CategorizationService          # AI tier routing
├── VendorAliasService             # Vendor normalization
└── DescriptionNormalizationService
```

### Report Generation
```
ReportGenerationService
├── PdfReportService               # PDF with receipts
├── ExcelExportService             # Excel with formulas
├── CsvExportService               # CSV export
└── AnalyticsExportService         # Analytics export
```

### Background Jobs (Hangfire)
```
Jobs/
├── SubscriptionAlertJob           # Monthly subscription alerts
├── CacheWarmingJob                # Historical data import
├── VistaSyncJob                   # Reference data sync
└── ReportGenerationJob            # Async report processing
```

## Matching Algorithm

```
Score = Amount (40) + Date (35) + Vendor (25) = 100 pts max

Amount Score:
  ├── Exact match: 40 pts
  ├── Within 1%: 35 pts
  └── Within 5%: 25 pts

Date Score:
  ├── Same day: 35 pts
  ├── ±1 day: 30 pts
  └── ±3 days: 20 pts

Vendor Score:
  ├── Exact match: 25 pts
  ├── Fuzzy >90%: 20 pts
  └── Fuzzy >80%: 15 pts
```

## Database Context

```csharp
ExpenseFlowDbContext : DbContext
├── Receipts
├── Transactions
├── ReceiptTransactionMatches
├── ExpenseReports
├── ExpenseLines
├── Departments          // Vista sync
├── Projects             // Vista sync
├── GLAccounts           // Vista sync
├── ExpensePatterns
├── ExpenseEmbeddings    // pgvector
├── TravelPeriods
├── DetectedSubscriptions
├── SplitPatterns
├── TransactionGroups
├── ExtractionCorrections
├── PredictionFeedback
├── VendorAliases
├── DescriptionCache
├── StatementFingerprints
├── ReportGenerationJobs
├── ImportJobs
└── Users / UserPreferences
```

## Dependency Injection Setup

```csharp
// Program.cs registration order
services.AddInfrastructure(config);  // EF Core, Blob, Hangfire
services.AddCoreServices();           // Domain services
services.AddAIServices(config);       // Semantic Kernel, OpenAI
services.AddIdentity(config);         // Entra ID auth
```
