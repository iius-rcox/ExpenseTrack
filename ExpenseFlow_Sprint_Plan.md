# ExpenseFlow Sprint Plan
## 11 Sprints • 22 Weeks • Cost-Optimized Architecture

---

## Sprint Overview

| Sprint | Weeks | Phase | Focus | Key Deliverable |
|--------|-------|-------|-------|-----------------|
| 1 | 1-2 | Foundation | Infrastructure | AKS configured with NGINX + Supabase running |
| 2 | 3-4 | Foundation | Core Backend | Auth working, cache tables created, Hangfire ready |
| 3 | 5-6 | Document Processing | Receipt Pipeline | Receipts upload → Doc Intelligence → stored |
| 4 | 7-8 | Document Processing | Statement Import | Fingerprinting + column mapping working |
| 5 | 9-10 | Intelligence | Matching Engine | Vendor aliasing + receipt-transaction matching |
| 6 | 11-12 | Intelligence | AI Categorization | Tiered GL suggestion (cache→embed→mini) |
| 7 | 13-14 | Intelligence | Advanced Features | Travel detection, subscriptions, splits |
| 8 | 15-16 | Reports | Draft Generation | Auto-generated draft reports |
| 9 | 17-18 | Reports | Output & Analytics | Excel export, PDF receipts, MoM dashboard |
| 10 | 19-20 | Launch Prep | Testing & Warming | UAT, cache warming, performance tuning |
| 11 | 21-22 | Launch | Production | Security audit, deployment, go-live |

---

## Sprint 1: Infrastructure Setup
**Weeks 1-2 | Phase: Foundation**

### Goal
Get the cost-optimized infrastructure running in existing AKS cluster.

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 1.1 | Deploy NGINX Ingress Controller | `kubectl get pods -n ingress-nginx` shows running pods | 3 |
| 1.2 | Deploy cert-manager | `kubectl get clusterissuer` shows letsencrypt-prod ready | 2 |
| 1.3 | Configure Let's Encrypt ClusterIssuer | Certificate issued for dev domain | 2 |
| 1.4 | Deploy Supabase self-hosted | PostgreSQL accessible, Studio UI working | 5 |
| 1.5 | Enable pgvector extension | `CREATE EXTENSION vector` succeeds | 1 |
| 1.6 | Create Persistent Volume (20GB) | PVC bound and mounted to Supabase | 2 |
| 1.7 | Configure Azure Blob Storage | Storage account created, connection string in Key Vault | 2 |
| 1.8 | Set up dev/staging namespaces | `expenseflow-dev` and `expenseflow-staging` namespaces exist | 1 |

### Definition of Done
- [ ] NGINX Ingress serves HTTPS traffic with valid Let's Encrypt cert
- [ ] Supabase PostgreSQL accepts connections from within cluster
- [ ] pgvector extension enabled and tested with sample embedding insert
- [ ] Blob Storage accessible via managed identity or connection string
- [ ] All secrets stored in Azure Key Vault

### Technical Notes
```bash
# NGINX Ingress
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm install ingress-nginx ingress-nginx/ingress-nginx -n ingress-nginx --create-namespace

# cert-manager
helm repo add jetstack https://charts.jetstack.io
helm install cert-manager jetstack/cert-manager -n cert-manager --create-namespace --set installCRDs=true

# Supabase (self-hosted)
helm repo add supabase https://supabase.github.io/supabase-kubernetes
helm install supabase supabase/supabase -n expenseflow-dev -f supabase-values.yaml
```

### Risks
- Supabase Helm chart complexity → Mitigation: Use minimal config, disable unused services
- Let's Encrypt rate limits → Mitigation: Use staging issuer for dev

---

## Sprint 2: Core Backend & Auth
**Weeks 3-4 | Phase: Foundation**

### Goal
.NET 8 API running with Entra ID auth, cache tables created, Hangfire configured.

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 2.1 | Scaffold .NET 8 Web API project | Solution builds, Dockerfile works | 3 |
| 2.2 | Configure Entra ID authentication | `/api/health` returns 401 without token, 200 with valid token | 5 |
| 2.3 | Create Users table + EF Core mapping | User created on first login via JWT claims | 3 |
| 2.4 | Create DescriptionCache table | CRUD operations work, hash index on raw_description_hash | 2 |
| 2.5 | Create VendorAliases table | Pattern matching query works with LIKE/ILIKE | 2 |
| 2.6 | Create StatementFingerprints table | JSONB column_mapping stores/retrieves correctly | 2 |
| 2.7 | Create SplitPatterns table | JSONB split_config stores array of allocations | 2 |
| 2.8 | Create ExpenseEmbeddings table | VECTOR(1536) column works, similarity search returns results | 3 |
| 2.9 | Configure Hangfire with PostgreSQL | Dashboard accessible at /hangfire, test job executes | 3 |
| 2.10 | Set up SQL Server sync job | GLAccounts, Departments, Projects tables populated | 3 |
| 2.11 | Deploy API to AKS | API accessible via Ingress URL | 2 |

### Definition of Done
- [ ] API authenticates via Entra ID JWT tokens
- [ ] All 5 cache tables created with proper indexes
- [ ] ExpenseEmbeddings supports vector similarity search
- [ ] Hangfire dashboard accessible (admin only)
- [ ] GL/Dept/Project data synced from SQL Server
- [ ] API deployed and accessible via HTTPS

### Database Schema (Sprint 2)
```sql
-- Cache tables
CREATE TABLE description_cache (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  raw_description_hash VARCHAR(64) NOT NULL,
  raw_description VARCHAR(500) NOT NULL,
  normalized_description VARCHAR(500) NOT NULL,
  hit_count INTEGER DEFAULT 0,
  created_at TIMESTAMP DEFAULT NOW(),
  CONSTRAINT idx_desc_hash UNIQUE (raw_description_hash)
);

CREATE TABLE vendor_aliases (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  canonical_name VARCHAR(255) NOT NULL,
  alias_pattern VARCHAR(500) NOT NULL,
  display_name VARCHAR(255) NOT NULL,
  default_gl_code VARCHAR(10),
  default_department VARCHAR(20),
  match_count INTEGER DEFAULT 0,
  last_matched_at TIMESTAMP,
  confidence DECIMAL(3,2) DEFAULT 1.00,
  created_at TIMESTAMP DEFAULT NOW()
);
CREATE INDEX idx_alias_pattern ON vendor_aliases(alias_pattern);

CREATE TABLE statement_fingerprints (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES users(id),
  source_name VARCHAR(100) NOT NULL,
  header_hash VARCHAR(64) NOT NULL,
  column_mapping JSONB NOT NULL,
  date_format VARCHAR(50),
  amount_sign VARCHAR(20) DEFAULT 'negative_charges',
  created_at TIMESTAMP DEFAULT NOW(),
  CONSTRAINT idx_fingerprint UNIQUE (user_id, header_hash)
);

CREATE TABLE split_patterns (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  vendor_alias_id UUID REFERENCES vendor_aliases(id),
  split_config JSONB NOT NULL,
  usage_count INTEGER DEFAULT 0,
  last_used_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE expense_embeddings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  expense_line_id UUID,
  vendor_normalized VARCHAR(255),
  description_text VARCHAR(500) NOT NULL,
  gl_code VARCHAR(10),
  department VARCHAR(20),
  embedding VECTOR(1536) NOT NULL,
  verified BOOLEAN DEFAULT FALSE,
  created_at TIMESTAMP DEFAULT NOW()
);
CREATE INDEX idx_embedding_vector ON expense_embeddings 
  USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
```

### Risks
- Entra ID config complexity → Mitigation: Use Microsoft.Identity.Web library
- pgvector index performance → Mitigation: Start with ivfflat, tune lists parameter

---

## Sprint 3: Receipt Upload Pipeline
**Weeks 5-6 | Phase: Document Processing**

### Goal
Users can upload receipts, Document Intelligence extracts data, receipts stored with extracted metadata.

### User Stories Addressed
- US-001: Upload Receipts
- US-002: Receipt Accountability (partial)

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 3.1 | Create Receipts table | Stores blob URL, status, extracted data | 2 |
| 3.2 | Implement receipt upload endpoint | POST /api/receipts accepts PDF/JPG/PNG/HEIC | 3 |
| 3.3 | Store receipts in Blob Storage | Files stored in `receipts/{userId}/{year}/{month}/` | 2 |
| 3.4 | Create Hangfire job: ProcessReceiptJob | Job queued on upload, processes async | 3 |
| 3.5 | Integrate Azure Document Intelligence | Extracted: vendor, date, amount, line items | 5 |
| 3.6 | Handle multi-page receipts | All pages processed, single receipt record | 2 |
| 3.7 | Store extraction results | Receipt updated with vendor_extracted, date_extracted, etc. | 2 |
| 3.8 | Build basic React receipt upload UI | Drag-drop upload, progress indicator | 3 |
| 3.9 | Show receipt list with status | List view: thumbnail, status (Processing/Ready/Error) | 2 |
| 3.10 | GET /api/receipts/unmatched | Returns receipts with status='Unmatched' | 1 |

### Definition of Done
- [ ] User uploads receipt → stored in Blob → queued for processing
- [ ] Document Intelligence extracts vendor, date, amount within 30 seconds
- [ ] Receipt list shows extraction results
- [ ] Unmatched receipts clearly indicated
- [ ] Error handling for failed extractions

### API Contracts
```typescript
// POST /api/receipts
// Request: multipart/form-data with file(s)
// Response:
{
  "receipts": [
    {
      "id": "uuid",
      "fileName": "receipt.pdf",
      "status": "Processing",
      "uploadedAt": "2025-01-15T10:30:00Z"
    }
  ]
}

// GET /api/receipts
// Response:
{
  "receipts": [
    {
      "id": "uuid",
      "fileName": "receipt.pdf",
      "status": "Ready", // Processing | Ready | Error | Matched
      "vendorExtracted": "Delta Airlines",
      "dateExtracted": "2025-01-10",
      "amountExtracted": 425.00,
      "thumbnailUrl": "https://...",
      "uploadedAt": "2025-01-15T10:30:00Z"
    }
  ],
  "unmatchedCount": 5
}
```

### Risks
- Document Intelligence accuracy varies → Mitigation: Store raw JSON, allow manual correction
- Large file uploads timeout → Mitigation: Chunked upload for files >10MB

---

## Sprint 4: Statement Import & Fingerprinting
**Weeks 7-8 | Phase: Document Processing**

### Goal
Users can import credit card statements with automatic column detection for known sources.

### User Stories Addressed
- US-003: Import Credit Card Statement
- US-004: Statement Source Fingerprinting

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 4.1 | Create Transactions table | Stores raw + parsed transaction data | 2 |
| 4.2 | Build CSV/Excel parser | Handles Chase, Amex, generic CSV formats | 3 |
| 4.3 | Implement fingerprint detection | Hash first row, check StatementFingerprints | 3 |
| 4.4 | Build column mapping UI | User confirms/corrects detected columns | 3 |
| 4.5 | Integrate GPT-4o-mini for unknown formats | Infers column mapping when fingerprint unknown | 5 |
| 4.6 | Save new fingerprints | Confirmed mappings saved for future | 2 |
| 4.7 | Import transactions | Parsed transactions stored in Transactions table | 3 |
| 4.8 | Handle amount sign convention | Negative = charge (Chase) vs Positive = charge (Amex) | 2 |
| 4.9 | POST /api/statements/analyze | Returns detected columns + confidence | 2 |
| 4.10 | POST /api/statements/import | Imports with confirmed mapping | 2 |
| 4.11 | Track Tier usage metrics | Log whether Tier 1 (fingerprint) or Tier 3 (GPT) used | 1 |

### Definition of Done
- [ ] Known statement sources import without confirmation (Tier 1)
- [ ] Unknown sources trigger GPT-4o-mini inference (Tier 3)
- [ ] User can confirm/correct column mappings
- [ ] Confirmed mappings saved as fingerprints
- [ ] Transactions imported with correct dates, amounts, descriptions

### Fingerprint Flow
```
Upload CSV
    ↓
Hash header row → Check StatementFingerprints
    ↓
┌─────────────────────────────────────┐
│ Fingerprint found?                  │
│   YES → Apply saved mapping (Tier 1)│
│   NO  → GPT-4o-mini infer (Tier 3)  │
└─────────────────────────────────────┘
    ↓
Show mapping to user for confirmation
    ↓
Save fingerprint (if new) → Import transactions
```

### Pre-configured Fingerprints (seed data)
```json
[
  {
    "source_name": "Chase Business Card",
    "column_mapping": {
      "Transaction Date": "date",
      "Post Date": "post_date", 
      "Description": "description",
      "Category": "category",
      "Type": "type",
      "Amount": "amount",
      "Memo": "memo"
    },
    "date_format": "MM/DD/YYYY",
    "amount_sign": "negative_charges"
  },
  {
    "source_name": "American Express",
    "column_mapping": {
      "Date": "date",
      "Description": "description",
      "Amount": "amount"
    },
    "date_format": "MM/DD/YYYY",
    "amount_sign": "positive_charges"
  }
]
```

### Risks
- GPT-4o-mini column inference errors → Mitigation: Always show confirmation UI
- CSV encoding issues → Mitigation: Detect encoding, support UTF-8/Latin-1

---

## Sprint 5: Matching Engine
**Weeks 9-10 | Phase: Intelligence**

### Goal
Automatically match receipts to transactions using vendor aliases, with learning from confirmations.

### User Stories Addressed
- US-005: Automatic Receipt Matching
- US-006: Vendor Aliasing

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 5.1 | Build vendor alias matching | Pattern match transaction desc to VendorAliases | 3 |
| 5.2 | Implement fuzzy matching fallback | Levenshtein distance for near-matches | 3 |
| 5.3 | Create matching algorithm | Amount ±$0.10, Date ±3 days, Vendor match | 5 |
| 5.4 | Calculate match confidence score | 0-100% based on amount/date/vendor alignment | 2 |
| 5.5 | POST /api/matching/auto | Runs matching for all unmatched receipts | 3 |
| 5.6 | Build matching review UI | Shows proposed matches with confidence | 3 |
| 5.7 | POST /api/matching/confirm | User confirms match → creates/updates alias | 3 |
| 5.8 | Implement alias learning | New patterns added to VendorAliases on confirm | 2 |
| 5.9 | Track alias usage | Increment match_count, update last_matched_at | 1 |
| 5.10 | Build confidence decay job | Flag aliases unused for 6 months | 2 |

### Definition of Done
- [ ] Auto-match finds correct transaction for 85%+ of receipts
- [ ] Confidence scores reflect match quality
- [ ] User confirmations create new vendor aliases
- [ ] Alias match_count and last_matched_at updated
- [ ] All matching uses Tier 1 (no AI calls)

### Matching Algorithm
```csharp
public class MatchResult {
    public Guid TransactionId { get; set; }
    public Guid ReceiptId { get; set; }
    public decimal Confidence { get; set; }
    public string MatchReason { get; set; }
}

public MatchResult? FindMatch(Receipt receipt, List<Transaction> transactions) {
    // Step 1: Find vendor alias
    var alias = _vendorAliases.FindByPattern(receipt.VendorExtracted);
    
    foreach (var txn in transactions.Where(t => !t.IsMatched)) {
        decimal confidence = 0;
        
        // Amount match (±$0.10)
        var amountDiff = Math.Abs(receipt.AmountExtracted - txn.Amount);
        if (amountDiff <= 0.10m) confidence += 40;
        else if (amountDiff <= 1.00m) confidence += 20;
        
        // Date match (±3 days)
        var daysDiff = Math.Abs((receipt.DateExtracted - txn.Date).TotalDays);
        if (daysDiff <= 1) confidence += 35;
        else if (daysDiff <= 3) confidence += 25;
        else if (daysDiff <= 7) confidence += 10;
        
        // Vendor match
        if (alias != null && txn.Description.Contains(alias.AliasPattern))
            confidence += 25;
        else if (FuzzyMatch(receipt.VendorExtracted, txn.Description) > 0.7)
            confidence += 15;
        
        if (confidence >= 70) {
            return new MatchResult {
                TransactionId = txn.Id,
                ReceiptId = receipt.Id,
                Confidence = confidence,
                MatchReason = $"Amount: {amountDiff:C}, Days: {daysDiff}, Vendor: {alias?.DisplayName}"
            };
        }
    }
    return null;
}
```

### Risks
- Duplicate transactions from statement re-import → Mitigation: Dedupe on date+amount+description hash
- False positive matches → Mitigation: Require >70% confidence, always allow user override

---

## Sprint 6: AI Categorization (Tiered)
**Weeks 11-12 | Phase: Intelligence**

### Goal
Suggest GL codes and departments using the cost-tiered approach: cache → embeddings → GPT-4o-mini.

### User Stories Addressed
- US-007: Smart Description Normalization
- US-008: GL Code Suggestion

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 6.1 | Build description normalization service | Checks DescriptionCache first (Tier 1) | 3 |
| 6.2 | Integrate GPT-4o-mini for normalization | Called only on cache miss (Tier 3) | 3 |
| 6.3 | Cache normalized descriptions | Result stored in DescriptionCache after API call | 2 |
| 6.4 | Build embedding generation service | Generate embedding for expense description | 3 |
| 6.5 | Implement similarity search | Find top-5 similar in ExpenseEmbeddings (Tier 2) | 3 |
| 6.6 | Build GL suggestion service | Tier 1→2→3 hierarchy enforced | 5 |
| 6.7 | Build department suggestion service | Same tiered approach | 3 |
| 6.8 | GET /api/categorization/suggest/{txnId} | Returns GL + dept suggestions with tier used | 2 |
| 6.9 | POST /api/descriptions/normalize | Normalizes raw description (cache-first) | 2 |
| 6.10 | Store verified embeddings | User selection creates verified=true embedding | 2 |
| 6.11 | Build categorization UI | Shows suggestions, allows selection, indicates tier | 3 |
| 6.12 | Track tier usage metrics | Log Tier 1/2/3 usage per operation | 1 |

### Definition of Done
- [ ] Description normalization checks cache first (0 API calls for known descriptions)
- [ ] GL suggestion uses embedding similarity when available (Tier 2)
- [ ] GPT-4o-mini only called when cache + embeddings fail
- [ ] User selections create verified embeddings for future use
- [ ] Tier usage logged for cost monitoring

### Tiered Service Implementation
```csharp
public class CategorizationService {
    public async Task<GLSuggestion> SuggestGLCode(string normalizedDescription, string vendor) {
        // TIER 1: Check vendor alias cache
        var alias = await _vendorAliases.FindByCanonicalName(vendor);
        if (alias?.DefaultGLCode != null) {
            _metrics.LogTierHit(1, "gl_suggestion");
            return new GLSuggestion {
                GLCode = alias.DefaultGLCode,
                Confidence = 0.95m,
                Tier = 1,
                Source = "VendorAlias"
            };
        }
        
        // TIER 2: Check embedding similarity
        var embedding = await _openAI.GenerateEmbedding(normalizedDescription);
        var similar = await _embeddings.FindSimilar(embedding, threshold: 0.92m, limit: 5);
        
        if (similar.Any() && similar.First().Verified) {
            _metrics.LogTierHit(2, "gl_suggestion");
            return new GLSuggestion {
                GLCode = similar.First().GLCode,
                Confidence = similar.First().Similarity,
                Tier = 2,
                Source = "EmbeddingSimilarity"
            };
        }
        
        // TIER 3: GPT-4o-mini inference
        var glCodes = await _glAccounts.GetAll();
        var suggestion = await _gpt4oMini.SuggestGLCode(normalizedDescription, glCodes);
        _metrics.LogTierHit(3, "gl_suggestion");
        
        return new GLSuggestion {
            GLCode = suggestion.GLCode,
            Confidence = suggestion.Confidence,
            Tier = 3,
            Source = "GPT4oMini"
        };
    }
}
```

### Risks
- Embedding similarity threshold too high/low → Mitigation: Start at 0.92, tune based on accuracy
- GPT-4o-mini rate limits → Mitigation: Implement retry with exponential backoff

---

## Sprint 7: Advanced Features
**Weeks 13-14 | Phase: Intelligence**

### Goal
Travel detection, subscription identification, and expense splitting—all optimized for cost.

### User Stories Addressed
- US-009: Expense Splitting with Auto-Suggestions
- US-010: Travel Period Detection
- US-011: Subscription Detection

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 7.1 | Create TravelPeriods table | Stores start_date, end_date, destination, source | 2 |
| 7.2 | Build rule-based travel detection | Flight/hotel receipts create travel periods | 3 |
| 7.3 | Integrate Claude for edge cases | Complex itineraries only (Tier 4) | 3 |
| 7.4 | Link expenses to travel periods | Expenses within period auto-tagged | 2 |
| 7.5 | Build subscription pattern matcher | Same vendor + similar amount + 2+ months = subscription | 3 |
| 7.6 | Seed known subscriptions | Claude.AI, OpenAI, Cursor, Foxit, etc. | 1 |
| 7.7 | GET /api/subscriptions/detected | Returns detected subscriptions for month | 2 |
| 7.8 | Build expense split UI | User can split expense across GL/dept | 3 |
| 7.9 | Implement split pattern learning | Splits saved to SplitPatterns table | 2 |
| 7.10 | GET /api/categorization/split-suggest | Returns suggested split from patterns (Tier 1) | 2 |
| 7.11 | Show travel timeline in UI | Visual representation of travel periods | 2 |

### Definition of Done
- [ ] Flight + hotel receipts automatically create travel period
- [ ] Expenses during travel period flagged for GL 66300
- [ ] Subscriptions detected after 2 consecutive months
- [ ] Split patterns learned and suggested (no AI needed)
- [ ] Claude only called for complex travel scenarios

### Travel Detection Rules (Tier 1)
```csharp
public TravelPeriod? DetectTravelPeriod(Receipt receipt) {
    // Rule-based detection (Tier 1)
    var vendor = receipt.VendorExtracted.ToLower();
    
    // Flight detection
    if (vendor.Contains("delta") || vendor.Contains("united") || 
        vendor.Contains("american air") || vendor.Contains("southwest")) {
        return new TravelPeriod {
            StartDate = receipt.DateExtracted,
            EndDate = receipt.DateExtracted, // Will be extended by hotel
            Source = "Flight",
            RequiresAIReview = false
        };
    }
    
    // Hotel detection
    if (vendor.Contains("marriott") || vendor.Contains("hilton") || 
        vendor.Contains("hyatt") || vendor.Contains("airbnb")) {
        // Extract check-in/out from line items if available
        var nights = receipt.LineItems?.FirstOrDefault(l => l.Description.Contains("night"));
        return new TravelPeriod {
            StartDate = receipt.DateExtracted,
            EndDate = receipt.DateExtracted.AddDays(nights?.Quantity ?? 1),
            Source = "Hotel",
            RequiresAIReview = false
        };
    }
    
    return null;
}
```

### Subscription Detection (Tier 1 Only)
```sql
-- Find potential subscriptions (same vendor, similar amount, 2+ months)
SELECT 
    vendor_normalized,
    COUNT(DISTINCT DATE_TRUNC('month', transaction_date)) as month_count,
    AVG(amount) as avg_amount,
    STDDEV(amount) as amount_variance
FROM transactions
WHERE transaction_date > NOW() - INTERVAL '6 months'
GROUP BY vendor_normalized
HAVING 
    COUNT(DISTINCT DATE_TRUNC('month', transaction_date)) >= 2
    AND STDDEV(amount) < 5.00  -- Amount variance < $5
ORDER BY month_count DESC;
```

### Risks
- Travel period detection misses complex itineraries → Mitigation: Flag for Claude review
- False subscription detection → Mitigation: Require user confirmation before auto-include

---

## Sprint 8: Draft Report Generation
**Weeks 15-16 | Phase: Reports**

### Goal
Auto-generate draft expense reports from matched receipts and transactions.

### User Stories Addressed
- US-012: Auto-Generate Draft Report
- US-002: Receipt Accountability (complete)

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 8.1 | Create ExpenseReports table | Stores report metadata, status, period | 2 |
| 8.2 | Create ExpenseLines table | Stores individual line items | 2 |
| 8.3 | POST /api/reports/draft | Creates draft from matched receipts | 5 |
| 8.4 | Pre-populate GL codes | Uses cached/embedded suggestions | 3 |
| 8.5 | Pre-populate departments | Same tiered approach | 2 |
| 8.6 | Apply description normalization | All descriptions normalized before draft | 2 |
| 8.7 | Identify missing receipts | Flag transactions without matched receipts | 2 |
| 8.8 | Build draft review UI | Full report view with edit capabilities | 5 |
| 8.9 | Implement line item editing | User can modify GL, dept, description | 2 |
| 8.10 | Update caches on user edits | Edits update VendorAliases, create embeddings | 2 |
| 8.11 | PUT /api/reports/{id}/lines | Saves edited lines | 2 |

### Definition of Done
- [ ] Draft report generated with all matched receipts
- [ ] GL codes and departments pre-populated
- [ ] Descriptions normalized to AP-readable format
- [ ] Missing receipts clearly identified
- [ ] User edits update caches for future improvement

### Draft Generation Flow
```
POST /api/reports/draft
    ↓
Gather all matched receipt-transaction pairs for period
    ↓
For each pair:
    ├─ Normalize description (cache-first)
    ├─ Suggest GL code (tier 1→2→3)
    ├─ Suggest department (tier 1→2→3)
    └─ Check for split pattern
    ↓
Identify transactions without receipts → flag as "missing"
    ↓
Create ExpenseReport + ExpenseLines
    ↓
Return draft for review
```

### API Response
```typescript
// POST /api/reports/draft
// Request: { period: "2025-01" }
// Response:
{
  "report": {
    "id": "uuid",
    "period": "2025-01",
    "status": "Draft",
    "createdAt": "2025-02-01T10:00:00Z",
    "lines": [
      {
        "id": "uuid",
        "date": "2025-01-10",
        "glCode": "66300",
        "glCodeSuggestionTier": 2,
        "department": "07",
        "description": "Delta Airlines Flight - SFO to JFK",
        "descriptionOriginal": "DELTA AIR 0062363598531",
        "amount": 425.00,
        "receiptId": "uuid",
        "receiptUrl": "https://...",
        "hasReceipt": true
      },
      {
        "id": "uuid",
        "date": "2025-01-15",
        "glCode": "63300",
        "glCodeSuggestionTier": 1,
        "department": "07", 
        "description": "OpenAI ChatGPT Subscription",
        "amount": 20.00,
        "receiptId": null,
        "hasReceipt": false,
        "missingReceiptReason": null  // User must provide
      }
    ],
    "summary": {
      "totalAmount": 445.00,
      "lineCount": 2,
      "missingReceipts": 1,
      "tier1Hits": 1,
      "tier2Hits": 1,
      "tier3Hits": 0
    }
  }
}
```

### Risks
- Too many unmatched items → Mitigation: Show matching wizard before draft
- Cache misses spike AI costs → Mitigation: Alert if Tier 3 >30% of suggestions

---

## Sprint 9: Output Generation & Analytics
**Weeks 17-18 | Phase: Reports**

### Goal
Excel export matching AP template, receipt PDF with placeholders, month-over-month dashboard.

### User Stories Addressed
- US-013: Generate Excel Report
- US-014: Receipt PDF with Placeholders
- US-015: Month-over-Month Comparison

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 9.1 | Build Excel template engine | Uses existing AP template as base | 3 |
| 9.2 | Generate expense rows | Date, GL, Dept, Description, Units, Rate, Total | 3 |
| 9.3 | Preserve Excel formulas | =IF(ISBLANK...) formulas intact | 2 |
| 9.4 | GET /api/reports/{id}/excel | Downloads formatted .xlsx | 2 |
| 9.5 | Build PDF generator | Consolidates all receipts | 3 |
| 9.6 | Create missing receipt placeholder | Page with name, date, amount, justification | 3 |
| 9.7 | Implement justification dropdown | 'Not provided', 'Lost', 'Digital', 'Other' | 1 |
| 9.8 | GET /api/reports/{id}/receipts.pdf | Downloads consolidated PDF | 2 |
| 9.9 | Build MoM comparison query | Compare this month vs last month | 3 |
| 9.10 | GET /api/analytics/comparison | Returns comparison data | 2 |
| 9.11 | Build comparison dashboard UI | New vendors, missing recurring, amount changes | 3 |
| 9.12 | GET /api/analytics/cache-stats | Returns tier hit rates, AI costs | 2 |
| 9.13 | Build cache stats dashboard | Shows cost optimization metrics | 2 |

### Definition of Done
- [ ] Excel export matches exact AP template format
- [ ] Formulas preserved and functional
- [ ] Receipt PDF ordered by expense line sequence
- [ ] Missing receipt placeholders include justification
- [ ] MoM comparison highlights anomalies
- [ ] Cache stats dashboard shows tier hit rates

### Excel Template Matching
```
Required columns (exact match):
| Expense Date | GL Acct/Job | Dept/Phas | Expense Description | Units/Mileage | Rate/Amount | Expense Total |

Row format:
| 01/10/25 | 66300 | 07 | Delta Airlines Flight - SFO to JFK | 1 | 425.00 | =IF(ISBLANK(E14),"",E14*F14) |

Header section:
Employee Name: {user.name}
Period: {report.period}
```

### Missing Receipt Placeholder
```
┌──────────────────────────────────────────────┐
│                                              │
│          ⚠️ MISSING RECEIPT                  │
│                                              │
│  Expense Date:    January 15, 2025           │
│  Vendor:          OpenAI                     │
│  Amount:          $20.00                     │
│  Description:     ChatGPT Subscription       │
│                                              │
│  Justification:   Digital subscription -     │
│                   no receipt provided        │
│                                              │
│  Employee:        John Smith                 │
│  Report ID:       EXP-2025-01-001            │
│                                              │
└──────────────────────────────────────────────┘
```

### Month-over-Month Comparison
```typescript
// GET /api/analytics/comparison?current=2025-01&previous=2024-12
{
  "current": "2025-01",
  "previous": "2024-12",
  "summary": {
    "currentTotal": 1250.00,
    "previousTotal": 980.00,
    "change": 270.00,
    "changePercent": 27.5
  },
  "newVendors": [
    { "vendor": "Cursor AI", "amount": 20.00 }
  ],
  "missingRecurring": [
    { "vendor": "Adobe Creative Cloud", "expectedAmount": 54.99 }
  ],
  "significantChanges": [
    { 
      "vendor": "Amazon Web Services",
      "current": 150.00,
      "previous": 85.00,
      "change": 65.00,
      "changePercent": 76.5
    }
  ]
}
```

### Risks
- Excel library compatibility → Mitigation: Use ClosedXML or EPPlus
- PDF generation memory for many receipts → Mitigation: Stream PDF generation

---

## Sprint 10: Testing & Cache Warming
**Weeks 19-20 | Phase: Launch Prep**

### Goal
User acceptance testing, historical data import for cache warming, performance optimization.

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 10.1 | Create UAT test plan | Covers all user stories | 2 |
| 10.2 | Set up staging environment | Mirror of production config | 3 |
| 10.3 | Conduct UAT with 3-5 users | All critical paths tested | 5 |
| 10.4 | Fix UAT bugs | P1/P2 bugs resolved | 5 |
| 10.5 | Import historical expense reports | Last 6 months of data | 3 |
| 10.6 | Generate embeddings from historical | All verified expenses embedded | 3 |
| 10.7 | Extract vendor aliases | Unique vendor patterns identified | 2 |
| 10.8 | Populate description cache | All unique descriptions normalized | 2 |
| 10.9 | Create statement fingerprints | Known sources configured | 1 |
| 10.10 | Performance test: 50 receipts | Batch processing <5 minutes | 3 |
| 10.11 | Optimize slow queries | All queries <500ms | 2 |
| 10.12 | Load test: 20 concurrent users | No degradation | 2 |

### Definition of Done
- [ ] UAT completed with sign-off from 3+ users
- [ ] All P1/P2 bugs fixed
- [ ] Cache hit rate >50% on day one (from historical data)
- [ ] 50 receipt batch processes in <5 minutes
- [ ] System handles 20 concurrent users

### Cache Warming Script
```csharp
public async Task WarmCaches(string historicalDataPath) {
    var reports = await LoadHistoricalReports(historicalDataPath);
    
    foreach (var report in reports) {
        foreach (var line in report.Lines) {
            // 1. Cache description normalization
            var hash = ComputeHash(line.RawDescription);
            await _descriptionCache.AddIfNotExists(new DescriptionCache {
                RawDescriptionHash = hash,
                RawDescription = line.RawDescription,
                NormalizedDescription = line.NormalizedDescription
            });
            
            // 2. Create vendor alias
            var alias = ExtractVendorPattern(line.RawDescription);
            await _vendorAliases.AddIfNotExists(new VendorAlias {
                CanonicalName = NormalizeVendor(line.Vendor),
                AliasPattern = alias,
                DisplayName = line.Vendor,
                DefaultGLCode = line.GLCode,
                DefaultDepartment = line.Department
            });
            
            // 3. Generate embedding
            var embedding = await _openAI.GenerateEmbedding(line.NormalizedDescription);
            await _embeddings.Add(new ExpenseEmbedding {
                DescriptionText = line.NormalizedDescription,
                GLCode = line.GLCode,
                Department = line.Department,
                Embedding = embedding,
                Verified = true
            });
        }
    }
    
    _logger.LogInformation("Cache warming complete: {Descriptions} descriptions, {Aliases} aliases, {Embeddings} embeddings",
        await _descriptionCache.Count(),
        await _vendorAliases.Count(),
        await _embeddings.Count());
}
```

### UAT Test Scenarios
1. **Receipt Upload Flow**: Upload 10 receipts → verify extraction → check status
2. **Statement Import**: Import Chase CSV → verify fingerprint → check transactions
3. **Matching**: Run auto-match → review suggestions → confirm matches
4. **Categorization**: Generate draft → verify GL suggestions → edit and save
5. **Travel Detection**: Upload flight + hotel → verify travel period created
6. **Report Generation**: Generate Excel → verify template match → download PDF
7. **MoM Comparison**: Compare two months → verify anomaly detection

### Risks
- Historical data format issues → Mitigation: Build flexible import parser
- Embedding generation cost → Mitigation: Budget for one-time warming (~$10)

---

## Sprint 11: Security & Production Launch
**Weeks 21-22 | Phase: Launch**

### Goal
Security audit, production deployment, monitoring setup, go-live.

### Targets

| # | Task | Acceptance Criteria | Points |
|---|------|---------------------|--------|
| 11.1 | Security audit: authentication | Entra ID config reviewed | 2 |
| 11.2 | Security audit: authorization | Role-based access verified | 2 |
| 11.3 | Security audit: data encryption | At-rest and in-transit encryption | 2 |
| 11.4 | Security audit: secrets management | All secrets in Key Vault | 2 |
| 11.5 | Security audit: network policies | Pod-to-pod traffic restricted | 2 |
| 11.6 | Set up production namespace | `expenseflow-prod` configured | 1 |
| 11.7 | Deploy to production | All services running | 3 |
| 11.8 | Configure Azure Monitor | Metrics, logs, alerts | 3 |
| 11.9 | Set up cost alerts | Alert if AI spend >$40/month | 1 |
| 11.10 | Create runbook | Incident response procedures | 2 |
| 11.11 | User training session | 30-min walkthrough for all users | 2 |
| 11.12 | Go-live announcement | Email + Teams notification | 1 |
| 11.13 | Monitor first week | Active monitoring, quick fixes | 3 |

### Definition of Done
- [ ] Security audit passed with no critical findings
- [ ] Production deployment successful
- [ ] Monitoring and alerting configured
- [ ] All users trained
- [ ] System stable for first week

### Security Checklist
```markdown
## Authentication & Authorization
- [ ] Entra ID tokens validated on every request
- [ ] Token expiration handled correctly
- [ ] Admin role required for Hangfire dashboard
- [ ] Admin role required for cache management

## Data Protection
- [ ] Blob Storage encryption at rest enabled
- [ ] PostgreSQL SSL connections enforced
- [ ] HTTPS enforced via Ingress
- [ ] Sensitive data not logged

## Secrets Management
- [ ] All connection strings in Key Vault
- [ ] API keys (OpenAI, Anthropic) in Key Vault
- [ ] No secrets in source code or config files
- [ ] Key Vault access via managed identity

## Network Security
- [ ] AKS network policies restrict pod traffic
- [ ] Ingress allows only HTTPS (443)
- [ ] Database not exposed publicly
- [ ] Hangfire dashboard internal only
```

### Monitoring Configuration
```yaml
# Azure Monitor alerts
alerts:
  - name: "High AI API Spend"
    condition: "AI_API_COST > 40"
    period: "monthly"
    severity: "warning"
    
  - name: "Low Cache Hit Rate"
    condition: "TIER_1_HIT_RATE < 50%"
    period: "daily"
    severity: "warning"
    
  - name: "High Error Rate"
    condition: "ERROR_RATE > 5%"
    period: "hourly"
    severity: "critical"
    
  - name: "Slow Response Time"
    condition: "P95_LATENCY > 2000ms"
    period: "hourly"
    severity: "warning"
```

### Risks
- Security findings delay launch → Mitigation: Start audit early in sprint
- Production issues post-launch → Mitigation: Active monitoring, quick rollback plan

---

## Appendix: Sprint Metrics Summary

### Velocity Target
- **Points per sprint**: 25-30
- **Total points**: ~300 across 11 sprints

### Key Milestones
| Week | Milestone |
|------|-----------|
| 4 | Infrastructure complete, auth working |
| 8 | Receipt + statement import working |
| 14 | Full AI categorization with tiered cost |
| 18 | Complete report generation |
| 22 | Production launch |

### Cost Optimization Checkpoints
| Sprint | Check |
|--------|-------|
| 4 | Fingerprint cache working (Tier 1) |
| 6 | Embedding similarity working (Tier 2) |
| 8 | Tier usage metrics being logged |
| 10 | Cache hit rate >50% after warming |
| 11 | Monthly AI cost <$40 target |

---

*ExpenseFlow Sprint Plan v1.0*  
*22 Weeks • 11 Sprints • ~$25/month target*
