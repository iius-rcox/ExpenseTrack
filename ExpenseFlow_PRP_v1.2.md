# ExpenseFlow
## Product Requirements & Planning Document
### AI-Powered Expense Report Management System

**Version 1.2 — Cost Optimized**  
**December 2025**

---

**Infrastructure:** AKS • NGINX Ingress • Supabase (self-hosted) • Hangfire  
**AI Strategy:** Cache-First • Embeddings-First • GPT-4o-mini Default

**Estimated New Monthly Cost: ~$25**  
*(Blob Storage $5 + AI APIs ~$20)*

*Existing Resources: AKS, Document Intelligence, Key Vault, ACR*

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [Cost Optimization Strategy](#2-cost-optimization-strategy)
3. [Goals & Non-Goals](#3-goals--non-goals)
4. [User Stories & Acceptance Criteria](#4-user-stories--acceptance-criteria)
5. [Technical Architecture](#5-technical-architecture)
6. [Data Models](#6-data-models)
7. [AI/ML Strategy](#7-aiml-strategy)
8. [Caching Architecture](#8-caching-architecture)
9. [API Specifications](#9-api-specifications)
10. [Azure Resources](#10-azure-resources)
11. [Development Phases](#11-development-phases)
12. [Success Metrics](#12-success-metrics)
- [Appendix A: GL Code Reference](#appendix-a-gl-code-reference)
- [Appendix B: Department Code Reference](#appendix-b-department-code-reference)

---

## 1. Executive Summary

### 1.1 Problem Statement

Processing monthly expense reports is a time-consuming manual task involving matching credit card transactions with receipts, categorizing expenses with correct GL codes and department allocations, and generating formatted reports for accounts payable.

### 1.2 Proposed Solution

ExpenseFlow is a cost-optimized, AI-powered web application that automates expense report processing. The system uses a **cache-first, embeddings-first** approach to minimize AI API costs while maximizing accuracy through learned patterns.

### 1.3 Key Capabilities

- Automated receipt/invoice extraction via Azure Document Intelligence
- AI-driven transaction matching with ±$0.10 amount and ±3 day date tolerance
- Statement source fingerprinting for zero-configuration imports
- Vendor aliasing with confidence decay for improved matching
- Smart description normalization for AP-readable expense descriptions
- Pattern-based GL code suggestions with embedding similarity matching
- Auto-split suggestions based on historical allocation patterns
- Travel period detection from flight and hotel receipts
- Corporate subscription detection after 2 consecutive months
- Auto-generated draft expense reports for review-and-submit workflow
- Month-over-month expense comparison dashboard
- Excel report generation matching exact AP template requirements
- Consolidated receipt PDF with placeholder pages for missing receipts
- Progressive Web App (PWA) for mobile receipt capture

### 1.4 Target Users

10-20 corporate users generating approximately 1 expense report per month each. Single-tenant deployment with one administrator and standard users authenticated via Microsoft Entra ID.

### 1.5 Critical Business Rule

**All expenses require a receipt or invoice prior to submission.** This includes recurring subscriptions. Expenses without receipts require a justification and will have a placeholder page in the consolidated receipt PDF.

---

## 2. Cost Optimization Strategy

### 2.1 Infrastructure Decisions

The following decisions minimize monthly costs while maintaining reliability for a 10-20 user application:

| Component | Decision | Alternative Avoided | Monthly Savings |
|-----------|----------|---------------------|-----------------|
| Ingress | NGINX + cert-manager | App Gateway ($150) | $150 |
| Database | Supabase self-hosted in AKS | Azure PostgreSQL ($50) | $50 |
| Job Queue | Hangfire (PostgreSQL) | Service Bus ($10) | $10 |
| Container Registry | ACR (existing) | N/A | $0 (have) |
| AI Strategy | Cache + Embeddings + Mini | GPT-4o everywhere ($75) | ~$55 |

### 2.2 AI Cost Reduction Hierarchy

**MANDATORY:** For every AI-powered operation, the system MUST follow this decision tree in order:

| # | Check | Action | Cost per Call |
|---|-------|--------|---------------|
| 1 | Exact Cache Hit | Hash input → lookup in cache table → return cached result | $0 |
| 2 | Embedding Similarity | Generate embedding → find top-5 similar (>0.92) → use if confident | ~$0.00002 |
| 3 | GPT-4o-mini | Simple inference tasks (90% of cases) | ~$0.0003 |
| 4 | GPT-4o / Claude | Complex reasoning only (travel detection, ambiguous cases) | ~$0.01 |

### 2.3 Model Selection by Task

Each AI task is assigned a specific model tier based on complexity:

| Task | Primary Method | Fallback Model | Tier |
|------|----------------|----------------|------|
| Column Mapping (statement) | Fingerprint cache | GPT-4o-mini | 1 → 3 |
| Description Normalization | Cache lookup | GPT-4o-mini | 1 → 3 |
| Vendor Matching | Alias table + fuzzy | None (deterministic) | 1 only |
| GL Code Suggestion | Embedding similarity | GPT-4o-mini | 2 → 3 |
| Department Suggestion | Embedding similarity | GPT-4o-mini | 2 → 3 |
| Split Pattern Suggestion | Pattern table lookup | None | 1 only |
| Travel Period Detection | Rule-based first | Claude Sonnet | 1 → 4 |
| Subscription Detection | Pattern matching | None (deterministic) | 1 only |
| Business Expense Flagging | Rule-based ($200+) | GPT-4o-mini (edge) | 1 → 3 |

### 2.4 Cost Projection

Based on 20 users × 30 expenses/month = 600 expense lines/month:

| Scenario | Month 1 | Month 6+ | Notes |
|----------|---------|----------|-------|
| Cache hit rate | 20% | 70%+ | Improves with use |
| Embedding-only resolution | 30% | 50%+ | After learning |
| GPT-4o-mini calls needed | ~400 | ~120 | Decreases |
| GPT-4o/Claude calls needed | ~50 | ~20 | Travel/edge only |
| Estimated AI API cost | ~$30 | ~$10 | Self-optimizing |

---

## 3. Goals & Non-Goals

### 3.1 Goals

1. **Receipt Accountability:** Every expense requires a receipt/invoice or documented justification.
2. **Accurate Categorization:** Expenses coded to correct GL accounts and departments.
3. **Intelligent Matching:** Auto-match receipts to transactions using vendor aliasing.
4. **AP-Ready Descriptions:** Human-readable expense descriptions for accounts payable.
5. **Format Compliance:** Excel reports match exact AP template requirements.
6. **Processing Speed:** Batch processing of 50+ receipts within 5 minutes.
7. **Self-Improving System:** Every user correction reduces future AI API calls.
8. **Cost Efficiency:** AI costs decrease over time as cache/embeddings grow.
9. **Draft Auto-Generation:** Shift workflow from 'create' to 'verify'.
10. **High Availability:** 99.9% uptime target.

### 3.2 Non-Goals

1. **Approval Workflows:** No manager approval process. Reports submitted directly to AP.
2. **Multi-Tenancy:** Single-tenant deployment only.
3. **Complex Roles:** Only Admin and User roles.
4. **Regulatory Compliance:** No SOX, GDPR, or FedRAMP requirements.
5. **Native Mobile Apps:** PWA only; no iOS/Android apps.
6. **ERP Integration:** Excel output for manual AP submission.
7. **Expensive AI Models:** GPT-4o/Claude used only when cheaper options fail.

---

## 4. User Stories & Acceptance Criteria

### 4.1 Receipt Management

#### US-001: Upload Receipts

*As a* user, *I want to* upload receipts individually or in bulk *so that* I can collect documents for my expense report.

**Acceptance Criteria:**
- Supports PDF, JPG, PNG, HEIC formats
- Drag-and-drop upload for desktop; camera capture via PWA on mobile
- Azure Document Intelligence extracts vendor, date, amount, line items
- Processing queued via Hangfire for async handling
- Receipt stored with status 'Unmatched' until linked to transaction

#### US-002: Receipt Accountability

*As a* user, *I want to* see all unmatched receipts *so that* I ensure every receipt is on a report or deleted.

**Acceptance Criteria:**
- Dashboard shows count of unmatched receipts
- User can delete unneeded receipts with confirmation
- Warning shown when generating report with unresolved receipts

### 4.2 Statement Import

#### US-003: Import Credit Card Statement

*As a* user, *I want to* upload my credit card statement *so that* the system can identify transactions to match with receipts.

**Acceptance Criteria:**
- Supports CSV and Excel formats
- System checks fingerprint cache first (Tier 1)
- If unknown source: GPT-4o-mini infers column mappings (Tier 3)
- User confirms mappings; system saves fingerprint for future
- Known sources auto-import with no confirmation needed

#### US-004: Statement Source Fingerprinting

*As a* user, *I want the system to* recognize my statement source automatically *so that* I don't confirm mappings for known sources.

**Acceptance Criteria:**
- Fingerprint based on: header hash, column names, date format, amount sign
- Known sources processed without AI call (cost: $0)
- User can reset fingerprint if source format changes
- Fingerprints stored per-user to handle personalized exports

### 4.3 Matching & Categorization

#### US-005: Automatic Receipt Matching

*As a* user, *I want to* auto-match receipts to transactions *so that* I don't manually associate each one.

**Acceptance Criteria:**
- Amount tolerance: ±$0.10; Date tolerance: ±3 days
- Vendor matching via alias table (no AI call needed)
- Confidence score displayed for each match
- User confirmations create new vendor aliases

#### US-006: Vendor Aliasing

*As a* user, *I want the system to* recognize vendor name variations *so that* matching improves without AI calls.

**Acceptance Criteria:**
- Groups variations (e.g., 'DELTA AIR 006236' ↔ Delta receipt)
- Learns new aliases from user-confirmed matches
- Confidence decay: aliases unused for 6 months flagged for review
- All alias lookups are deterministic (Tier 1, cost: $0)

#### US-007: Smart Description Normalization

*As a* user, *I want* cryptic descriptions converted to readable text *so that* AP workers understand each expense.

**Acceptance Criteria:**
- Cache checked first for exact raw description match (Tier 1)
- If cache miss: GPT-4o-mini normalizes (Tier 3)
- Result cached permanently for future lookups
- User edits update cache and improve future results
- Examples: 'OPENAI *CHATGPT SUBSCR' → 'OpenAI ChatGPT Subscription'

#### US-008: GL Code Suggestion

*As a* user, *I want* GL codes suggested based on patterns *so that* I can quickly code expenses correctly.

**Acceptance Criteria:**
- Step 1: Check if vendor has cached GL assignment (Tier 1)
- Step 2: Find top-5 similar expenses via embedding (Tier 2)
- Step 3: If similarity >0.92, use that GL code
- Step 4: Only if no match: GPT-4o-mini suggests based on GL descriptions (Tier 3)
- User selection creates new verified embedding
- Common codes: 63300 (Software), 66300 (Travel), 66800 (Internet)

#### US-009: Expense Splitting with Auto-Suggestions

*As a* user, *I want to* split expenses with suggested allocations *so that* costs are allocated correctly with minimal effort.

**Acceptance Criteria:**
- System checks SplitPatterns table for vendor (Tier 1, no AI)
- If pattern exists: one-click apply suggested split
- Split amounts must sum to original transaction total
- User corrections update pattern for future use

### 4.4 Travel & Subscriptions

#### US-010: Travel Period Detection

*As a* user, *I want* travel periods detected automatically *so that* related expenses are grouped.

**Acceptance Criteria:**
- Rule-based detection first: flight receipts, hotel check-in/out (Tier 1)
- Ambiguous cases only: Claude Sonnet for reasoning (Tier 4)
- International travel uses ±1 day buffer
- Travel periods displayed visually on expense timeline

#### US-011: Subscription Detection

*As a* user, *I want* recurring subscriptions identified *so that* I can easily include them monthly.

**Acceptance Criteria:**
- Detection via pattern matching (Tier 1, no AI): same vendor, similar amount, 2+ consecutive months
- Pre-configured list: Claude.AI, OpenAI, Superhuman, Fireflies.AI, Foxit, Adobe, Cursor, GFiber, AT&T
- One-click include for known subscriptions
- **Receipts still required for all subscriptions**

### 4.5 Report Generation

#### US-012: Auto-Generate Draft Report

*As a* user, *I want* draft reports auto-generated *so that* I review and submit rather than build from scratch.

**Acceptance Criteria:**
- Draft includes all matched receipt-transaction pairs
- GL codes and departments pre-populated from suggestions
- Descriptions normalized to AP-readable format
- Workflow shifts from 'create' to 'verify'

#### US-013: Generate Excel Report

*As a* user, *I want* formatted Excel output matching AP template *so that* I can submit to accounts payable.

**Acceptance Criteria:**
- Exact template match: Date | GL Acct | Dept | Description | Units | Rate | Total
- Preserves formulas (e.g., =IF(ISBLANK(I14),'',I14*J14))
- Header populated with employee info

#### US-014: Receipt PDF with Placeholders

*As a* user, *I want* receipts consolidated with placeholders for missing ones *so that* AP has complete documentation.

**Acceptance Criteria:**
- Receipts ordered by expense line-item sequence
- Missing receipt = placeholder page with: name, date, amount, description, justification
- Justification options: 'Receipt not provided', 'Lost receipt', 'Digital subscription', 'Other'
- Placeholder clearly labeled 'MISSING RECEIPT'

### 4.6 Analytics

#### US-015: Month-over-Month Comparison

*As a* user, *I want to* compare this month vs last month *so that* I catch anomalies and ensure completeness.

**Acceptance Criteria:**
- Dashboard widget: 'This Month vs. Last Month'
- Highlights: new vendors, missing recurring charges, significant amount changes (>20% or >$50)
- Clickable items to drill into details

---

## 5. Technical Architecture

### 5.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│  CLIENTS                                                        │
│  ┌───────────┐  ┌───────────┐  ┌───────────┐                    │
│  │   Web     │  │    PWA    │  │  Desktop  │                    │
│  └─────┬─────┘  └─────┬─────┘  └─────┬─────┘                    │
│        └──────────────┼──────────────┘                          │
│                       ▼                                         │
│  ┌─────────────────────────────────────────┐                    │
│  │  NGINX Ingress + cert-manager (FREE)    │ ← Let's Encrypt    │
│  └───────────────────┬─────────────────────┘                    │
│                       ▼                                         │
│  ┌─────────────────────────────────────────────────────────┐    │
│  │              AZURE KUBERNETES SERVICE                   │    │
│  │  ┌───────────┐  ┌───────────┐  ┌───────────────────┐    │    │
│  │  │ Frontend  │  │    API    │  │  Hangfire Worker  │    │    │
│  │  │   React   │  │  .NET 8   │  │   (Background)    │    │    │
│  │  └───────────┘  └─────┬─────┘  └─────────┬─────────┘    │    │
│  │                       │                  │               │    │
│  │  ┌───────────────────┴──────────────────┴───────────┐   │    │
│  │  │  Supabase PostgreSQL (self-hosted) + pgvector    │   │    │
│  │  │  • Cache tables  • Embeddings  • Hangfire jobs   │   │    │
│  │  └───────────────────────────────────────────────────┘   │    │
│  └─────────────────────────────────────────────────────────┘    │
│                                                                  │
│  EXTERNAL SERVICES (Existing)         AI (Cost-Tiered)          │
│  ┌─────────────┐  ┌───────────────┐  ┌─────────────────────┐    │
│  │ Azure Doc   │  │  SQL Server   │  │ T1: Cache ($0)      │    │
│  │ Intelligence│  │  (Read-Only)  │  │ T2: Embeddings      │    │
│  │ (existing)  │  │               │  │ T3: GPT-4o-mini     │    │
│  └─────────────┘  └───────────────┘  │ T4: GPT-4o/Claude   │    │
│                                       └─────────────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

### 5.2 Component Details

#### Ingress Layer (NGINX + cert-manager)

- NGINX Ingress Controller deployed via Helm
- cert-manager for automatic Let's Encrypt SSL certificates
- No Azure Application Gateway needed (saves $150/month)
- TLS termination at ingress level

#### Frontend (React + TypeScript)

- React 18+ with TypeScript
- PWA-enabled with service worker for offline receipt capture
- Tailwind CSS for responsive design
- MSAL.js for Entra ID authentication

#### API Backend (.NET 8)

- ASP.NET Core 8 Web API
- Entity Framework Core with Npgsql for PostgreSQL
- Semantic Kernel for AI orchestration (tiered model selection)
- Hangfire for background job processing
- Polly for resilience patterns

#### Database (Supabase Self-Hosted)

- PostgreSQL 15+ with pgvector extension
- Supabase stack deployed in AKS cluster
- Persistent Volume (20GB Premium SSD) for data
- Hangfire tables for job queue (replaces Service Bus)
- Weekly sync from external SQL Server for GL/Dept/Project tables

---

## 6. Data Models

### 6.1 Cache Tables (Cost Optimization)

**These tables enable Tier 1 lookups ($0 cost):**

#### DescriptionCache

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| raw_description_hash | VARCHAR(64) | SHA-256 hash of raw description |
| raw_description | VARCHAR(500) | Original transaction description |
| normalized_description | VARCHAR(500) | Human-readable version |
| hit_count | INTEGER | Number of cache hits |
| created_at | TIMESTAMP | When first cached |

#### VendorAliases

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| canonical_name | VARCHAR(255) | Normalized vendor (e.g., 'delta_airlines') |
| alias_pattern | VARCHAR(500) | Raw pattern (e.g., 'DELTA AIR*') |
| display_name | VARCHAR(255) | Human-readable (e.g., 'Delta Airlines') |
| default_gl_code | VARCHAR(10) | Learned GL code for this vendor |
| match_count | INTEGER | Times matched |
| last_matched_at | TIMESTAMP | For confidence decay |
| confidence | DECIMAL(3,2) | Decays if unused 6 months |

#### StatementFingerprints

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| user_id | UUID FK | Owner |
| source_name | VARCHAR(100) | E.g., 'Chase Business Card' |
| header_hash | VARCHAR(64) | Hash of column headers |
| column_mapping | JSONB | Column → field type mapping |
| date_format | VARCHAR(50) | E.g., 'MM/DD/YYYY' |
| amount_sign | VARCHAR(20) | 'negative_charges' or 'positive_charges' |

#### SplitPatterns

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| vendor_alias_id | UUID FK | Vendor this pattern applies to |
| split_config | JSONB | [{gl_code, department, percentage}] |
| usage_count | INTEGER | Times used |
| last_used_at | TIMESTAMP | Last usage |

### 6.2 Embedding Tables (Tier 2)

#### ExpenseEmbeddings

| Column | Type | Description |
|--------|------|-------------|
| id | UUID | Primary key |
| expense_line_id | UUID FK | Source expense line |
| vendor_normalized | VARCHAR(255) | Normalized vendor name |
| description_text | VARCHAR(500) | Text used for embedding |
| gl_code | VARCHAR(10) | Assigned GL code |
| department | VARCHAR(20) | Assigned department |
| embedding | VECTOR(1536) | text-embedding-3-small vector |
| verified | BOOLEAN | User verified this |

### 6.3 Core Entity Tables

Standard tables for Users, Receipts, Transactions, ExpenseLines, TravelPeriods, ExpenseReports. See Appendix for full schemas.

---

## 7. AI/ML Strategy

### 7.1 Cost-Tiered Architecture

All AI operations MUST follow the cost hierarchy. The system is designed to maximize Tier 1/2 usage:

| Tier | Method | When Used | Cost/Call |
|------|--------|-----------|-----------|
| 1 | Cache Lookup | Exact match in cache tables | $0 |
| 2 | Embedding Similarity | Similar expense (>0.92) in vector DB | $0.00002 |
| 3 | GPT-4o-mini | Simple inference (90% of new items) | $0.0003 |
| 4 | GPT-4o / Claude | Complex reasoning only | $0.01 |

### 7.2 Learning Loops

Every user action improves future performance and reduces costs:

- **Description confirmation:** Adds to DescriptionCache (future lookups = Tier 1)
- **Vendor match confirmation:** Creates VendorAlias entry (future matching = Tier 1)
- **GL code selection:** Creates verified ExpenseEmbedding (future lookups = Tier 2)
- **Split allocation:** Updates SplitPatterns (future splits = Tier 1)
- **Statement import:** Saves StatementFingerprint (future imports = Tier 1)

### 7.3 Model Configuration

| Model | Provider | Use Cases | Tier |
|-------|----------|-----------|------|
| text-embedding-3-small | OpenAI | All embeddings | 2 |
| gpt-4o-mini | OpenAI | Normalization, column mapping, GL suggestion | 3 |
| gpt-4o | OpenAI | Ambiguous categorization | 4 |
| claude-3.5-sonnet | Anthropic | Complex travel detection, edge cases | 4 |

### 7.4 Description Normalization Examples

| Raw Description | Normalized (AP-Readable) |
|-----------------|--------------------------|
| OPENAI *CHATGPT SUBSCR | OpenAI ChatGPT Subscription |
| DNH*GODADDY#3835741538 | GoDaddy Domain/Hosting Service |
| DELTA AIR 0062363598531 | Delta Airlines Flight |
| CURSOR AI POWERED IDE | Cursor AI Code Editor Subscription |
| RDUAA PUBLIC PARKING | RDU Airport Parking |
| IAH ITRP EL TIEMPO 1122 | IAH Airport Restaurant - El Tiempo |

---

## 8. Caching Architecture

### 8.1 Cache-First Decision Flow

This pseudocode is **MANDATORY** for all AI-powered operations:

```javascript
async function getGLCodeSuggestion(expense):
  // TIER 1: Check vendor cache (cost: $0)
  vendorAlias = await VendorAliases.findByPattern(expense.rawDescription)
  if (vendorAlias?.default_gl_code):
    log('TIER_1_HIT', expense.id)
    return vendorAlias.default_gl_code

  // TIER 2: Check embedding similarity (cost: ~$0.00002)
  embedding = await generateEmbedding(expense.normalizedDescription)
  similar = await ExpenseEmbeddings.findSimilar(embedding, threshold=0.92, limit=5)
  if (similar.length > 0 && similar[0].verified):
    log('TIER_2_HIT', expense.id, similar[0].similarity)
    return similar[0].gl_code

  // TIER 3: GPT-4o-mini inference (cost: ~$0.0003)
  glCodes = await GLAccounts.getAll()
  suggestion = await gpt4omini.suggest(expense.normalizedDescription, glCodes)
  log('TIER_3_CALL', expense.id)
  return suggestion
```

### 8.2 Cache Warming Strategy

Pre-populate caches from historical data to maximize day-one efficiency:

1. **Import historical expense reports:** Creates verified embeddings
2. **Extract unique vendor patterns:** Populates VendorAliases table
3. **Normalize all descriptions:** Fills DescriptionCache
4. **Extract split patterns:** Creates SplitPatterns entries
5. **Process known statement formats:** Saves StatementFingerprints

### 8.3 Cache Metrics & Monitoring

Track these metrics to ensure cost optimization:

| Metric | Target (Month 6+) | Alert Threshold |
|--------|-------------------|-----------------|
| Tier 1 hit rate (cache) | >70% | <50% |
| Tier 2 hit rate (embeddings) | >50% of cache misses | <30% |
| Tier 4 usage (expensive models) | <5% of total calls | >15% |
| Monthly AI API spend | <$20 | >$40 |
| Cache entries (descriptions) | >500 | <100 after 3 months |
| Vendor aliases | >200 | <50 after 3 months |

---

## 9. API Specifications

### 9.1 Authentication

All endpoints require Microsoft Entra ID JWT token: `Authorization: Bearer {token}`

### 9.2 Core Endpoints

#### Receipts

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/receipts | Upload receipt(s) - queued via Hangfire |
| GET | /api/receipts | List receipts with filtering |
| GET | /api/receipts/unmatched | Get all unmatched receipts |
| DELETE | /api/receipts/{id} | Delete unmatched receipt |

#### Statements

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/statements/analyze | Upload + check fingerprint (Tier 1 first) |
| POST | /api/statements/import | Import with confirmed mapping |
| GET | /api/statements/fingerprints | List saved fingerprints |
| DELETE | /api/statements/fingerprints/{id} | Reset fingerprint |

#### Matching & Categorization

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/matching/auto | Auto-match (uses VendorAliases, Tier 1) |
| POST | /api/matching/confirm | Confirm match → creates alias |
| GET | /api/categorization/suggest/{txnId} | Get GL/dept suggestion (tiered) |
| GET | /api/categorization/split-suggest | Get split pattern (Tier 1 only) |
| POST | /api/descriptions/normalize | Normalize description (cache-first) |

#### Reports

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | /api/reports/draft | Auto-generate draft expense report |
| GET | /api/reports/{id} | Get report with lines |
| PUT | /api/reports/{id}/lines | Update lines → updates caches |
| GET | /api/reports/{id}/excel | Download formatted Excel |
| GET | /api/reports/{id}/receipts.pdf | Download receipt PDF |

#### Analytics

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | /api/analytics/comparison | Month-over-month comparison |
| GET | /api/analytics/cache-stats | Cache hit rates and AI costs |

---

## 10. Azure Resources

### 10.1 Existing Resources (No Additional Cost)

| Resource | SKU | Status |
|----------|-----|--------|
| AKS Cluster | 2x Standard_D2s_v3 | ✓ Already provisioned |
| Azure Document Intelligence | S0 | ✓ Already provisioned |
| Azure Key Vault | Standard | ✓ Already provisioned |
| Azure Container Registry | Basic | ✓ Already provisioned |

### 10.2 New Resources Required

| Resource | Configuration | Monthly Cost | Notes |
|----------|---------------|--------------|-------|
| Azure Blob Storage | Standard LRS, 100GB | ~$5 | Receipts, docs |
| Persistent Volume | 20GB Premium SSD | ~$3 | PostgreSQL data |
| AI Provider APIs | OpenAI + Anthropic | ~$15-25 | Tiered usage |

### 10.3 In-Cluster Deployments (No Additional Cost)

| Component | Deployment Method |
|-----------|-------------------|
| NGINX Ingress | Helm: ingress-nginx/ingress-nginx |
| cert-manager | Helm: jetstack/cert-manager + Let's Encrypt ClusterIssuer |
| Supabase PostgreSQL | Helm: supabase/supabase with pgvector extension |
| Hangfire | In-process with API, uses PostgreSQL for storage |

### 10.4 Total Monthly Cost Summary

| Category | Monthly Cost |
|----------|--------------|
| Existing infrastructure (AKS, ACR, Doc Intel, Key Vault) | $0 (sunk) |
| Blob Storage | ~$5 |
| Persistent Volume | ~$3 |
| AI APIs (Month 1) | ~$25-30 |
| AI APIs (Month 6+, after learning) | ~$10-15 |
| **TOTAL (Month 1)** | **~$35** |
| **TOTAL (Month 6+)** | **~$20** |

---

## 11. Development Phases

### Phase 1: Foundation (Weeks 1-4)

**Goal:** Infrastructure + core data model + caching layer

- Deploy NGINX Ingress + cert-manager to existing AKS
- Deploy Supabase self-hosted with pgvector extension
- Create all cache tables (DescriptionCache, VendorAliases, etc.)
- Implement Entra ID authentication
- Build SQL Server sync for GL/Dept/Project
- Set up Hangfire for background jobs

### Phase 2: Document Processing (Weeks 5-8)

**Goal:** Receipt upload + extraction + statement fingerprinting

- Implement receipt upload with Blob Storage
- Integrate Azure Document Intelligence
- Build statement fingerprinting system
- Implement column mapping (fingerprint-first, then GPT-4o-mini)
- Add PWA support for mobile capture

### Phase 3: Matching & Intelligence (Weeks 9-14)

**Goal:** Core AI with cost-tiered architecture

- Build vendor aliasing with confidence decay
- Implement description normalization (cache-first)
- Create embedding pipeline with pgvector
- Build GL code suggestion (Tier 1→2→3 hierarchy)
- Implement split patterns and auto-suggestions
- Add travel period detection (rules-first, Claude for edge)
- Build subscription detection (pattern matching only)

### Phase 4: Report Generation (Weeks 15-18)

**Goal:** Output generation + analytics

- Build draft report auto-generation
- Implement Excel template matching
- Create receipt PDF with missing receipt placeholders
- Build month-over-month comparison dashboard
- Add cache statistics dashboard

### Phase 5: Polish & Launch (Weeks 19-22)

**Goal:** Production readiness + cache warming

- User acceptance testing
- Import historical expense reports for cache warming
- Performance optimization (<5 min batch processing)
- Security audit
- Production deployment

---

## 12. Success Metrics

### 12.1 Functional Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Receipt extraction accuracy | ≥95% | % correct vendor/amount/date |
| Auto-match success rate | ≥85% | % matched without intervention |
| GL code suggestion accuracy | ≥80% | % correct on first suggestion |
| Description normalization quality | ≥90% | % requiring no user edit |
| Statement auto-recognition | ≥95% | % known sources fingerprinted |
| Processing time (50 receipts) | <5 min | End-to-end batch |
| System uptime | 99.9% | Azure Monitor |

### 12.2 Cost Optimization Metrics

| Metric | Target (Month 6+) | Alert If |
|--------|-------------------|----------|
| Tier 1 cache hit rate | >70% | <50% |
| Tier 2 embedding resolution | >50% of misses | <30% |
| Tier 4 (expensive model) usage | <5% | >15% |
| Monthly AI API spend | <$20 | >$40 |
| Cache entries (descriptions) | >500 | <100 at month 3 |
| Vendor aliases | >200 | <50 at month 3 |
| Verified embeddings | >1000 | <200 at month 6 |

---

## Appendix A: GL Code Reference

| GL Code | Description / Usage |
|---------|---------------------|
| 63300 | Software/Technology - AI subscriptions, SaaS tools, Foxit, Cursor, etc. |
| 66300 | Travel - Flights, hotels, rental cars, meals during travel, parking |
| 66800 | Internet Services - GFiber, home office internet |
| 66100 | Wireless/Telecom - AT&T, mobile phone bills |
| 15146 | Equipment/Assets - Dell computers, large equipment purchases |

---

## Appendix B: Department Code Reference

| Dept | Description | Notes |
|------|-------------|-------|
| 07 | General OH | Default department for corporate expenses |
| 08 | FST OH | Facilities Solutions |
| 11 | Houston - Scaffold Yard | Houston operations |
| 43 | Baton Rouge OH | Baton Rouge office overhead |
| 44 | EPC | Engineer / Procure / Construct |
| 92624 | Project Code | Example job/project number |

---

*End of Document*  
*ExpenseFlow PRP v1.2 — Cost Optimized*
