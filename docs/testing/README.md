# ExpenseFlow Testing Documentation

This directory contains comprehensive testing documentation for the ExpenseFlow expense tracking application.

**Last Updated**: 2025-12-31
**Version**: 1.16.0

---

## Documentation Index

| Document | Description | Size |
|----------|-------------|------|
| [API-ENDPOINTS.md](./API-ENDPOINTS.md) | Complete API endpoint reference with request/response examples | 18 controllers, 60+ endpoints |
| [FRONTEND-FEATURES.md](./FRONTEND-FEATURES.md) | Frontend routes, components, and user interactions | 12 routes, design system |
| [BACKGROUND-JOBS.md](./BACKGROUND-JOBS.md) | Hangfire jobs, schedules, and retry policies | 6 jobs |
| [DATABASE-ENTITIES.md](./DATABASE-ENTITIES.md) | Entity models, relationships, and enums | 15+ entities |
| [EXTERNAL-INTEGRATIONS.md](./EXTERNAL-INTEGRATIONS.md) | Azure services and external system integrations | 5 services |
| [uat_testing.md](./uat_testing.md) | UAT test cases and execution procedures | 7 test cases |

---

## Quick Reference

### API Controllers

| Controller | Endpoints | Description |
|------------|-----------|-------------|
| HealthController | 1 | Service health checks |
| ReceiptsController | 8 | Receipt CRUD, upload, processing |
| StatementsController | 5 | Statement import with column mapping |
| TransactionsController | 4 | Transaction listing and management |
| MatchingController | 8 | Receipt-transaction matching |
| CategorizationController | 3 | AI-powered expense categorization |
| SubscriptionsController | 6 | Recurring payment detection |
| TravelPeriodsController | 5 | Travel period management |
| ReportsController | 7 | Expense report generation |
| AnalyticsController | 7 | Spending analytics and trends |
| DashboardController | 3 | Dashboard metrics |
| UsersController | 4 | User profile and preferences |
| CacheController | 1 | Cache statistics (admin) |
| ExpenseSplittingController | 5 | Expense allocation |
| ReferenceController | 4 | GL codes, departments, projects |
| CacheWarmingController | 5 | Historical data import |
| DescriptionController | 2 | Description normalization |
| TestCleanupController | 1 | Test data cleanup (staging only) |

### Background Jobs

| Job | Schedule | Purpose |
|-----|----------|---------|
| ProcessReceiptJob | On upload | OCR extraction via Document Intelligence |
| SubscriptionAlertJob | Monthly (1st) | Missing subscription detection |
| CacheWarmingJob | On demand | Historical data import processing |
| ReferenceDataSyncJob | Daily (2 AM) | Vista ERP data synchronization |
| EmbeddingCleanupJob | Monthly | Stale embedding purge |
| AliasConfidenceDecayJob | Weekly | Confidence score decay |

### External Services

| Service | Purpose |
|---------|---------|
| Azure Blob Storage | Receipt images, thumbnails, imports |
| Azure Document Intelligence | Receipt OCR extraction |
| Azure OpenAI | Embeddings and AI categorization |
| Viewpoint Vista ERP | Reference data (GL, departments, projects) |
| Microsoft Entra ID | User authentication |

### Frontend Routes

| Route | Features |
|-------|----------|
| /dashboard | Metrics, activity feed, action queue |
| /receipts | Upload, list, status filters |
| /transactions | List, search, filters |
| /statements | Import wizard with column mapping |
| /matching | Auto-match, proposals, manual linking |
| /reports | Draft, edit, export (Excel/PDF) |
| /analytics | Trends, categories, merchants, subscriptions |
| /settings | Theme, preferences |

---

## Testing Approach

### Unit Testing
- Mock external services (Blob, AI, Vista)
- Use InMemory database for entity tests
- Focus on business logic isolation

### Integration Testing
- Test API endpoints with test database
- Verify authentication flows
- Test Hangfire job execution

### E2E Testing
- See [uat_testing.md](./uat_testing.md) for test cases
- Use test data in `test-data/` directory
- Staging environment: `expenseflow-staging.iius.app`

### Load Testing
- See [../performance/](../performance/) for load test reports
- NBomber scenarios for batch receipt processing
- k6 scripts for concurrent user testing

---

## Environment Configuration

### Development
- Local PostgreSQL or Supabase
- Azure Storage Emulator (Azurite)
- Mock AI services

### Staging
- Namespace: `expenseflow-staging`
- URL: `https://expenseflow-staging.iius.app`
- Real Azure services with test accounts

### Production
- Namespace: `expenseflow-prod`
- Full Azure service integration
- Production Entra ID tenant

---

## Related Documentation

- [User Guide](../user-guide/README.md) - End-user documentation
- [UAT Test Cases](../uat/test-cases/) - Detailed test procedures
- [Performance Reports](../performance/) - Load test results
- [Feature Specifications](../../specs/) - Technical specifications

---

## Version History

| Date | Version | Changes |
|------|---------|---------|
| 2025-12-31 | 1.16.0 | Complete testing documentation suite |
| 2025-12-21 | 1.11.0 | Initial UAT testing documentation |
