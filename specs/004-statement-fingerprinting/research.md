# Research: Statement Import & Fingerprinting

**Feature**: 004-statement-fingerprinting
**Date**: 2025-12-05

## Technology Decisions

### 1. CSV/Excel Parsing Library

**Decision**: CsvHelper for CSV, ClosedXML for Excel

**Rationale**:
- CsvHelper is the de facto standard for .NET CSV parsing with 500M+ NuGet downloads
- Supports streaming for large files, configurable delimiters, encoding detection
- ClosedXML provides full Excel read/write without requiring Office installation
- Both are MIT licensed and actively maintained

**Alternatives Considered**:
- EPPlus: Changed to paid license for commercial use
- NPOI: More complex API, primarily for Java port compatibility
- System.IO.StreamReader: No automatic header detection, encoding issues

### 2. AI Column Inference

**Decision**: Azure OpenAI GPT-4o-mini via Semantic Kernel

**Rationale**:
- Constitution requires Tier 3 = GPT-4o-mini for "simple inference"
- Semantic Kernel already configured in project (per constitution)
- Azure OpenAI endpoint already exists (iius-embedding)
- Structured output via JSON mode ensures parseable responses

**Alternatives Considered**:
- Direct Azure.AI.OpenAI SDK: Semantic Kernel provides retry/resilience built-in
- Claude API: Reserved for Tier 4 complex reasoning per constitution

### 3. Header Hash Algorithm

**Decision**: SHA-256 of normalized, sorted header row

**Rationale**:
- SHA-256 is fast, collision-resistant, and standard
- Normalizing (lowercase, trim whitespace) handles minor variations
- Sorting ensures column order doesn't affect fingerprint
- Already used in existing StatementFingerprint.HeaderHash field

**Alternatives Considered**:
- MD5: Deprecated for security-sensitive contexts
- xxHash: Faster but less standard library support in .NET
- Raw string comparison: Column order changes break matching

### 4. Duplicate Detection

**Decision**: SHA-256 hash of (date + amount + description)

**Rationale**:
- Combination of all three fields creates unique transaction identity
- Same algorithm as header hash for consistency
- Stored as indexed column for fast lookup during import

**Alternatives Considered**:
- Database unique constraint: Too restrictive, prevents re-import after fixes
- Amount-only dedup: Common amounts would cause false positives
- Include bank reference number: Not all statements provide this

### 5. System Fingerprint Storage

**Decision**: Null UserId indicates system-wide fingerprint

**Rationale**:
- Minimal change to existing StatementFingerprint entity
- UserId already nullable-capable with FK constraint
- Query: `WHERE UserId = @userId OR UserId IS NULL`
- Seed data inserts with UserId = null for Chase/Amex

**Alternatives Considered**:
- Separate SystemFingerprint table: Duplicates structure, complicates queries
- Boolean IsSystem flag: Adds column, same query complexity
- Special GUID (e.g., all zeros): Non-standard, confusing

### 6. File Encoding Detection

**Decision**: UTF8Encoding with BOM detection, Latin-1 fallback

**Rationale**:
- Most modern exports are UTF-8
- Legacy bank systems often use ISO-8859-1 (Latin-1)
- StreamReader can detect BOM automatically
- Fallback to Latin-1 covers 99% of Western character sets

**Alternatives Considered**:
- ude (Mozilla Universal Charset Detector): Heavy dependency for edge case
- Always assume UTF-8: Breaks on legacy exports
- User-selectable encoding: Unnecessary complexity for users

### 6a. Date Format Fallback Strategy

**Decision**: Multi-layer fallback with deterministic parsing before AI

**Rationale**:
- Fingerprint provides expected dateFormat (e.g., "MM/dd/yyyy")
- If parsing fails with detected format, try common alternatives before failing
- AI inference is expensive; deterministic fallbacks are cheaper

**Fallback Order**:
1. **Detected/Fingerprint format**: Use the dateFormat from fingerprint or AI inference
2. **ISO 8601**: "yyyy-MM-dd" (international standard, unambiguous)
3. **US format**: "MM/dd/yyyy" (common in US bank exports)
4. **EU format**: "dd/MM/yyyy" (common in European exports)
5. **US with dashes**: "MM-dd-yyyy"
6. **EU with dashes**: "dd-MM-yyyy"
7. **Fail row**: Skip row and increment SkippedCount

**Ambiguity Handling**:
- For dates like "01/02/2025", MM/dd vs dd/MM is ambiguous
- Use format from fingerprint as authoritative
- For AI inference, include sample dates in prompt to detect convention
- Log warning if multiple formats could parse the same value

**Implementation**:
```csharp
private static readonly string[] DateFormats = {
    "yyyy-MM-dd", "MM/dd/yyyy", "dd/MM/yyyy",
    "MM-dd-yyyy", "dd-MM-yyyy", "M/d/yyyy", "d/M/yyyy"
};

public DateOnly? ParseDate(string value, string? preferredFormat)
{
    // Try preferred format first
    if (preferredFormat != null &&
        DateOnly.TryParseExact(value, preferredFormat, out var date))
        return date;

    // Fallback to common formats
    foreach (var format in DateFormats)
    {
        if (DateOnly.TryParseExact(value, format, out date))
            return date;
    }

    return null; // Row will be skipped
}
```

### 7. Frontend State Management

**Decision**: React useState + Context for import wizard flow

**Rationale**:
- Multi-step wizard (upload → mapping → confirm → results)
- Local state sufficient for single-page flow
- Context shares state between step components
- No need for Redux complexity for this feature

**Alternatives Considered**:
- Redux: Overkill for isolated feature flow
- URL state (query params): Doesn't persist file data
- SessionStorage: Complicates component testing

## Pre-configured Fingerprints

### Chase Business Card

```json
{
  "sourceName": "Chase Business Card",
  "headerHash": "<computed at seed time>",
  "columnMapping": {
    "Transaction Date": "date",
    "Post Date": "post_date",
    "Description": "description",
    "Category": "category",
    "Type": "type",
    "Amount": "amount",
    "Memo": "memo"
  },
  "dateFormat": "MM/dd/yyyy",
  "amountSign": "negative_charges"
}
```

### American Express

```json
{
  "sourceName": "American Express Business",
  "headerHash": "<computed at seed time>",
  "columnMapping": {
    "Date": "date",
    "Description": "description",
    "Amount": "amount"
  },
  "dateFormat": "MM/dd/yyyy",
  "amountSign": "positive_charges"
}
```

## AI Prompt Strategy

### Column Mapping Inference Prompt

```text
You are a financial data analyst. Analyze the CSV headers and first 3 data rows to infer column mappings.

Headers: {headers}
Sample rows:
{row1}
{row2}
{row3}

Map each column to one of: date, post_date, description, amount, category, memo, reference, ignore

Also determine:
- dateFormat: The date pattern (e.g., "MM/dd/yyyy", "yyyy-MM-dd")
- amountSign: "negative_charges" if negative amounts are expenses, "positive_charges" if positive amounts are expenses

Respond in JSON format:
{
  "columnMapping": { "<header>": "<field>" },
  "dateFormat": "<pattern>",
  "amountSign": "<sign_convention>",
  "confidence": <0.0-1.0>
}
```

### Confidence Thresholds

- **>0.9**: Auto-accept mapping (still show confirmation UI)
- **0.7-0.9**: Show mapping with warning indicators
- **<0.7**: Require manual review of each column

## Performance Considerations

### Batch Processing for Large Files

- Files >500 rows: Process in 500-row batches
- Each batch: Parse → validate → dedupe check → insert
- Progress callback updates UI every batch
- Transaction wraps entire import for rollback on failure

### Fingerprint Lookup Optimization

- Index on (UserId, HeaderHash) already exists
- Query order: User fingerprints first, then system fingerprints
- Cache frequently used fingerprints in memory (future optimization)

## Security Considerations

### File Upload Validation

- Max file size: 10MB (configurable)
- Allowed extensions: .csv, .xlsx, .xls
- Content-Type validation matches extension
- Virus scanning via Azure Blob Storage (if enabled)

### Data Privacy

- Statement files not permanently stored (deleted after processing)
- Transaction data associated only with authenticated user
- AI inference sends only headers + 3 sample rows (no PII in typical statements)
