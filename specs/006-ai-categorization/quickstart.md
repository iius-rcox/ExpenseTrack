# Quickstart: AI Categorization (Tiered)

**Feature**: 006-ai-categorization
**Date**: 2025-12-16

## Prerequisites

- .NET 8 SDK installed
- Node.js 18+ and pnpm installed
- Docker Desktop running (for local PostgreSQL with pgvector)
- Azure OpenAI access (iius-embedding endpoint)
- Existing ExpenseFlow backend and frontend running

## Backend Setup

### 1. Add Required NuGet Packages

```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet add package Microsoft.SemanticKernel --version 1.25.0
dotnet add package Microsoft.SemanticKernel.Connectors.AzureOpenAI --version 1.25.0
```

### 2. Configure Azure OpenAI

Add to `appsettings.Development.json`:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://iius-embedding.openai.azure.com/",
    "EmbeddingDeployment": "text-embedding-3-small",
    "ChatDeployment": "gpt-4o-mini",
    "ApiKey": "{from-keyvault-or-env}"
  },
  "Categorization": {
    "EmbeddingSimilarityThreshold": 0.92,
    "VendorAliasConfirmThreshold": 3,
    "EmbeddingRetentionMonths": 6
  }
}
```

### 3. Run Database Migrations

```bash
cd backend
dotnet ef migrations add AddCategorizationEntities --project src/ExpenseFlow.Infrastructure --startup-project src/ExpenseFlow.Api

dotnet ef database update --project src/ExpenseFlow.Infrastructure --startup-project src/ExpenseFlow.Api
```

### 4. Register Services

Add to `Program.cs` or service registration:

```csharp
// Semantic Kernel
builder.Services.AddAzureOpenAITextEmbeddingGeneration(
    deploymentName: config["AzureOpenAI:EmbeddingDeployment"],
    endpoint: config["AzureOpenAI:Endpoint"],
    apiKey: config["AzureOpenAI:ApiKey"]
);

// Categorization services
builder.Services.AddScoped<IDescriptionNormalizationService, DescriptionNormalizationService>();
builder.Services.AddScoped<ICategorizationService, CategorizationService>();
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ITierUsageService, TierUsageService>();

// Polly resilience
builder.Services.AddResiliencePipeline("ai-calls", builder => {
    builder.AddRetry(new RetryStrategyOptions {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential
    });
    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(60)
    });
});
```

### 5. Add Hangfire Job for Embedding Cleanup

```csharp
// In Hangfire configuration
RecurringJob.AddOrUpdate<IEmbeddingCleanupJob>(
    "purge-stale-embeddings",
    job => job.PurgeStaleEmbeddingsAsync(),
    Cron.Monthly
);
```

## Frontend Setup

### 1. Install Dependencies

```bash
cd frontend
pnpm add @tanstack/react-query
```

### 2. Configure React Query

Add to `src/main.tsx`:

```tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 1000 * 60 * 5, // 5 minutes
      retry: 2,
    },
  },
});

// Wrap app with QueryClientProvider
<QueryClientProvider client={queryClient}>
  <App />
</QueryClientProvider>
```

### 3. Create API Service

Create `src/services/categorizationService.ts`:

```typescript
import { apiClient } from './apiClient';

export interface CategorizationSuggestion {
  glCode: string;
  glName: string;
  confidence: number;
  tier: 1 | 2 | 3;
  source: string;
  explanation: string;
}

export interface TransactionCategorization {
  transactionId: string;
  normalizedDescription: string;
  vendor: string;
  gl: {
    topSuggestion: CategorizationSuggestion | null;
    alternatives: CategorizationSuggestion[];
  };
  department: {
    topSuggestion: CategorizationSuggestion | null;
    alternatives: CategorizationSuggestion[];
  };
}

export const categorizationService = {
  getCategorization: (transactionId: string) =>
    apiClient.get<TransactionCategorization>(
      `/categorization/transactions/${transactionId}`
    ),

  confirmCategorization: (
    transactionId: string,
    glCode: string,
    departmentCode: string
  ) =>
    apiClient.post(`/categorization/transactions/${transactionId}/confirm`, {
      glCode,
      departmentCode,
      acceptedSuggestion: true,
    }),

  skipSuggestion: (transactionId: string) =>
    apiClient.post(`/categorization/transactions/${transactionId}/skip`, {
      reason: 'user_choice',
    }),
};
```

## Testing the Feature

### 1. Unit Test Setup

Create test file `tests/ExpenseFlow.Infrastructure.Tests/Services/CategorizationServiceTests.cs`:

```csharp
public class CategorizationServiceTests
{
    [Fact]
    public async Task SuggestGLCode_WithVendorAlias_ReturnsTier1()
    {
        // Arrange
        var vendorAliasRepo = new Mock<IVendorAliasRepository>();
        vendorAliasRepo.Setup(r => r.FindByPatternAsync("DELTA AIR", It.IsAny<Guid>()))
            .ReturnsAsync(new VendorAlias { DefaultGLCode = "66300" });

        var service = new CategorizationService(vendorAliasRepo.Object, ...);

        // Act
        var result = await service.SuggestGLCodeAsync("DELTA AIR 12345", userId);

        // Assert
        result.Tier.Should().Be(1);
        result.GLCode.Should().Be("66300");
    }

    [Fact]
    public async Task SuggestGLCode_WithSimilarEmbedding_ReturnsTier2()
    {
        // ... test embedding similarity fallback
    }

    [Fact]
    public async Task SuggestGLCode_NoMatches_ReturnsTier3()
    {
        // ... test AI inference fallback
    }
}
```

### 2. Integration Test

```bash
# Start the API
cd backend
dotnet run --project src/ExpenseFlow.Api

# Test normalization endpoint
curl -X POST http://localhost:5000/api/v1/descriptions/normalize \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"rawDescription": "DELTA AIR 0062363598531"}'

# Expected response (first call - Tier 3):
# {"normalizedDescription": "Delta Airlines - Flight", "tier": 3, "cacheHit": false}

# Second call with same description (Tier 1 cache hit):
# {"normalizedDescription": "Delta Airlines - Flight", "tier": 1, "cacheHit": true}
```

### 3. Verify Tier Logging

```sql
-- Check tier usage after testing
SELECT operation_type, tier_used, COUNT(*)
FROM tier_usage_logs
WHERE created_at > NOW() - INTERVAL '1 hour'
GROUP BY operation_type, tier_used
ORDER BY operation_type, tier_used;
```

## Common Issues

### pgvector Extension Not Found

```sql
-- Run in PostgreSQL as superuser
CREATE EXTENSION IF NOT EXISTS vector;
```

### Azure OpenAI 429 Rate Limit

- Check Polly circuit breaker is configured
- Verify retry policy in `appsettings.json`
- Review Azure OpenAI quota in portal

### Embedding Similarity Always Returns Empty

- Verify similarity threshold (default 0.92 may be too high)
- Check that embeddings exist: `SELECT COUNT(*) FROM expense_embeddings WHERE verified = true`
- Ensure vector index is created: `\d expense_embeddings` in psql

## Next Steps

1. Run `/speckit.tasks` to generate implementation tasks
2. Implement backend services following the contracts
3. Build frontend components
4. Run integration tests
5. Deploy to dev environment
