# Research: AI Categorization (Tiered)

**Feature**: 006-ai-categorization
**Date**: 2025-12-16
**Status**: Complete

## Research Tasks

### 1. Embedding Generation Strategy

**Decision**: Use Azure OpenAI text-embedding-3-small via Semantic Kernel

**Rationale**:
- Constitution mandates Azure OpenAI (iius-embedding endpoint) for Tier 2
- text-embedding-3-small provides 1536-dimension vectors at $0.00002/1K tokens
- Semantic Kernel provides unified abstraction for both embeddings and chat completions
- Already deployed and accessible from AKS cluster

**Alternatives Considered**:
- OpenAI API directly: Rejected - constitution specifies Azure OpenAI for cost control
- Local embedding models (sentence-transformers): Rejected - adds infrastructure complexity, not cost-justified for 10-20 users
- Azure AI Search embeddings: Rejected - adds another service dependency

**Implementation Notes**:
- Use `Microsoft.SemanticKernel` NuGet package
- Configure `AzureOpenAITextEmbeddingGenerationService` with iius-embedding endpoint
- Embedding dimension: 1536 (text-embedding-3-small default)

### 2. Vector Similarity Search with pgvector

**Decision**: Use pgvector with IVFFlat index and cosine similarity

**Rationale**:
- pgvector already enabled in Supabase PostgreSQL (Sprint 2)
- IVFFlat index provides good balance of speed and accuracy for expected data volume
- Cosine similarity is standard for text embeddings
- EF Core + Npgsql.EntityFrameworkCore.PostgreSQL supports pgvector operations

**Alternatives Considered**:
- HNSW index: Rejected - faster queries but slower inserts, overkill for <100K embeddings
- Azure AI Search: Rejected - adds $250+/month cost, violates constitution Principle IV
- Pinecone/Weaviate: Rejected - external dependency, cost, and complexity

**Implementation Notes**:
- Similarity threshold: 0.92 (configurable via appsettings)
- Index: `CREATE INDEX ... USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100)`
- Query: `ORDER BY embedding <=> @queryVector LIMIT 5`
- Use `Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite` for vector operations

### 3. Description Normalization via GPT-4o-mini

**Decision**: Use GPT-4o-mini with structured output for description normalization

**Rationale**:
- Constitution Tier 3 specifies GPT-4o-mini for simple inference
- Cost: ~$0.0003 per normalization (150 input + 50 output tokens typical)
- Structured output ensures consistent format for caching

**Alternatives Considered**:
- GPT-4o: Rejected - 10x cost, not justified for simple normalization
- Claude 3.5 Haiku: Rejected - constitution specifies GPT-4o-mini for Tier 3
- Regex/rule-based: Rejected - too fragile for diverse bank descriptions

**Implementation Notes**:
- System prompt: "Normalize this bank transaction description to a human-readable format. Extract vendor name and transaction type. Output JSON: {vendor, description}"
- Max tokens: 100
- Temperature: 0 (deterministic output for caching consistency)

### 4. GL Code Suggestion via AI

**Decision**: Use GPT-4o-mini with GL account list context

**Rationale**:
- Same model as normalization for consistency
- Provide full GL account list (typically 50-100 codes) in context
- Let model select best match based on normalized description

**Alternatives Considered**:
- Embedding similarity on GL descriptions: Rejected - GL codes don't have rich descriptions
- Classification model: Rejected - requires training data we don't have
- Rule-based mapping: Rejected - too rigid for diverse expense types

**Implementation Notes**:
- Include GL code, name, and category in prompt context
- System prompt: "Given this expense description, select the most appropriate GL code from the list. Output JSON: {glCode, confidence, reasoning}"
- Cache AI responses with normalized description as key

### 5. Tiered Service Architecture

**Decision**: Implement as chain-of-responsibility pattern with early exit

**Rationale**:
- Clean separation of concerns for each tier
- Easy to add/remove tiers without changing caller code
- Supports tier logging at each decision point

**Alternatives Considered**:
- Single service with if/else: Rejected - becomes complex, hard to test tiers independently
- Event-driven pipeline: Rejected - overkill for synchronous request/response
- Strategy pattern: Rejected - doesn't naturally support fallback chain

**Implementation Notes**:
```csharp
public interface ITierHandler
{
    int TierLevel { get; }
    Task<CategorizationResult?> TryHandle(CategorizationRequest request);
}

// Tier1Handler -> Tier2Handler -> Tier3Handler (chain)
```

### 6. Retry and Resilience Strategy

**Decision**: Use Polly for exponential backoff with circuit breaker

**Rationale**:
- Constitution specifies Polly for resilience patterns
- AI services may rate limit or have transient failures
- Circuit breaker prevents cascading failures

**Alternatives Considered**:
- Simple retry loop: Rejected - doesn't handle circuit breaking
- Azure SDK built-in retry: Rejected - less configurable, doesn't work across services

**Implementation Notes**:
- Retry policy: 3 attempts, exponential backoff (1s, 2s, 4s)
- Circuit breaker: Open after 5 failures in 30 seconds, half-open after 60 seconds
- Fallback: Return "suggestion unavailable" status, allow manual categorization

### 7. Embedding Retention and Cleanup

**Decision**: Hangfire scheduled job for monthly purge of stale unverified embeddings

**Rationale**:
- Clarification session determined 6-month retention for unverified embeddings
- Hangfire already used for background jobs per constitution
- Monthly cleanup balances storage efficiency with operational overhead

**Alternatives Considered**:
- PostgreSQL TTL/partitioning: Rejected - adds complexity, overkill for expected volume
- Real-time cleanup on insert: Rejected - adds latency to user operations
- Manual cleanup: Rejected - error-prone, not sustainable

**Implementation Notes**:
- Hangfire recurring job: `@monthly` schedule
- Query: `DELETE FROM expense_embeddings WHERE verified = false AND created_at < NOW() - INTERVAL '6 months'`
- Log deleted count for monitoring

### 8. Frontend Component Architecture

**Decision**: React components with React Query for server state

**Rationale**:
- Existing frontend uses React + TypeScript
- React Query provides caching, background refresh, and optimistic updates
- Aligns with statement import page patterns

**Alternatives Considered**:
- Redux: Rejected - overkill for server state, React Query simpler
- SWR: Rejected - less feature-rich than React Query
- Plain fetch: Rejected - no caching, more boilerplate

**Implementation Notes**:
- Use `@tanstack/react-query` for API calls
- Mutations for confirm/reject actions
- Invalidate suggestions query on user selection

## Dependencies Verified

| Dependency | Version | Status |
|------------|---------|--------|
| Microsoft.SemanticKernel | 1.x | Add to Infrastructure project |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.x | Already installed |
| pgvector extension | 0.5+ | Enabled in Supabase |
| Polly | 8.x | Already installed |
| @tanstack/react-query | 5.x | Add to frontend |

## Cost Projections

| Operation | Tier | Cost/Call | Est. Calls/Month | Monthly Cost |
|-----------|------|-----------|------------------|--------------|
| Embedding generation | 2 | $0.00002 | 500 | $0.01 |
| Description normalization | 3 | $0.0003 | 200 | $0.06 |
| GL suggestion | 3 | $0.0005 | 200 | $0.10 |
| **Total new AI costs** | | | | **~$0.17** |

With existing receipt processing and statement fingerprinting, total AI spend remains well under $40/month target.
