# Research: Output Generation & Analytics

**Feature**: 009-output-analytics
**Date**: 2025-12-16

## Research Tasks

### 1. Excel Generation Library Selection

**Question**: Which .NET library best fits the requirement to generate Excel files with preserved formulas from a template?

**Decision**: ClosedXML

**Rationale**:
- Already referenced in CLAUDE.md for Sprint 4 (statement fingerprinting)
- Supports reading/writing .xlsx files with formula preservation
- Can open template file, populate data rows, and save without breaking formulas
- MIT licensed, actively maintained
- Better formula support than EPPlus (licensing concerns with v5+)

**Alternatives Considered**:
- EPPlus: Commercial license required for v5+; v4 has limited formula support
- NPOI: Java port, less idiomatic .NET API, formula preservation issues
- OpenXML SDK: Low-level, requires significant boilerplate for formula handling

**Implementation Pattern**:
```csharp
using var workbook = new XLWorkbook("template.xlsx");
var worksheet = workbook.Worksheet(1);
// Write data starting at row N, formulas in column G remain intact
worksheet.Cell(startRow, 1).Value = expenseDate;
workbook.SaveAs(outputStream);
```

---

### 2. PDF Generation and Consolidation

**Question**: What approach for generating PDF placeholders and consolidating multiple file formats (PDF, JPG, PNG) into a single PDF?

**Decision**: PdfSharpCore + ImageSharp combination

**Rationale**:
- PdfSharpCore: Cross-platform .NET Core PDF generation and manipulation
- ImageSharp: Image processing for converting JPG/PNG to PDF pages
- Both MIT licensed, well-maintained
- Supports streaming for memory efficiency with large receipt collections
- Already aligned with .NET 8 target platform

**Alternatives Considered**:
- iTextSharp: AGPL licensing requires open source or commercial license
- Syncfusion: Commercial, overkill for this use case
- Aspose.PDF: Commercial, expensive
- QuestPDF: Excellent for generation but limited PDF merging capability

**Implementation Pattern**:
```csharp
// Create placeholder page
using var doc = new PdfDocument();
var page = doc.AddPage();
var gfx = XGraphics.FromPdfPage(page);
gfx.DrawString("MISSING RECEIPT", font, brush, rect);

// Merge existing PDFs
using var finalDoc = new PdfDocument();
foreach (var receiptPath in receiptPaths)
{
    if (Path.GetExtension(receiptPath) == ".pdf")
    {
        var inputDoc = PdfReader.Open(receiptPath, PdfDocumentOpenMode.Import);
        foreach (var inputPage in inputDoc.Pages)
            finalDoc.AddPage(inputPage);
    }
    else // Image
    {
        using var image = Image.Load(receiptPath);
        var imagePage = finalDoc.AddPage();
        // Convert and draw image to page
    }
}
```

---

### 3. Month-over-Month Comparison Algorithm

**Question**: How to efficiently calculate MoM comparison with new vendors, missing recurring, and significant changes?

**Decision**: SQL-based comparison with materialized aggregations

**Rationale**:
- Leverage PostgreSQL for heavy lifting (JOIN, GROUP BY, aggregations)
- Calculate in single query to minimize round trips
- Use CTEs for readability and optimization
- No additional entities needed - calculated on-demand from transactions

**Alternatives Considered**:
- In-memory C# calculation: Higher memory usage, slower for large datasets
- Materialized view: Adds maintenance complexity for low-frequency operation
- Pre-computed table: Over-engineering for 10-20 user scale

**Implementation Pattern**:
```sql
WITH current_month AS (
    SELECT vendor_normalized, SUM(amount) as total
    FROM expense_lines el
    JOIN expense_reports er ON el.report_id = er.id
    WHERE er.period = :current AND er.user_id = :userId
    GROUP BY vendor_normalized
),
previous_month AS (
    SELECT vendor_normalized, SUM(amount) as total
    FROM expense_lines el
    JOIN expense_reports er ON el.report_id = er.id
    WHERE er.period = :previous AND er.user_id = :userId
    GROUP BY vendor_normalized
)
SELECT
    COALESCE(c.vendor_normalized, p.vendor_normalized) as vendor,
    c.total as current_amount,
    p.total as previous_amount,
    CASE
        WHEN p.total IS NULL THEN 'new'
        WHEN c.total IS NULL THEN 'missing'
        WHEN ABS(c.total - p.total) / p.total > 0.5 THEN 'significant_change'
        ELSE 'normal'
    END as change_type
FROM current_month c
FULL OUTER JOIN previous_month p ON c.vendor_normalized = p.vendor_normalized;
```

---

### 4. Cache Statistics Aggregation

**Question**: How to efficiently compute tier usage statistics from TierUsageLog for the dashboard?

**Decision**: Aggregation query with date range filtering

**Rationale**:
- TierUsageLog already exists from Sprint 6 with appropriate indexes
- Simple COUNT/GROUP BY queries meet the 2-second performance target
- Can optionally add materialized hourly/daily rollups if volume increases

**Alternatives Considered**:
- Real-time counters (Redis): Over-engineering for current scale
- Separate analytics database: Unnecessary complexity
- Time-series database: Overkill for simple counters

**Query Pattern**:
```sql
SELECT
    tier_used,
    COUNT(*) as count,
    AVG(response_time_ms) as avg_response_ms
FROM tier_usage_logs
WHERE user_id = :userId
  AND created_at >= :startDate
  AND created_at <= :endDate
GROUP BY tier_used;
```

**Cost Estimation**: Based on tier usage and token counts:
- Tier 1: $0 (cache)
- Tier 2: ~$0.00002 per embedding lookup
- Tier 3: ~$0.0003 per GPT-4o-mini call
- Tier 4: ~$0.01 per GPT-4o/Claude call (rare)

---

### 5. AP Template Structure

**Question**: What is the exact structure of the AP department's Excel template?

**Decision**: Use fixed template format from Sprint Plan

**Rationale**: Sprint Plan explicitly defines the required columns:
| Expense Date | GL Acct/Job | Dept/Phas | Expense Description | Units/Mileage | Rate/Amount | Expense Total |

**Template Structure**:
```
Row 1-5: Header section
  - Employee Name: {user.displayName}
  - Period: {report.period}
  - Report ID: {report.id}

Row 6: Column headers (as above)
Row 7+: Expense lines
  - Column A: Date (MM/DD/YY format)
  - Column B: GL code
  - Column C: Department code
  - Column D: Normalized description
  - Column E: Units/Mileage (default 1)
  - Column F: Rate/Amount (expense amount)
  - Column G: =IF(ISBLANK(E{row}),"",E{row}*F{row})

Last Row: Totals with SUM formulas
```

---

### 6. Streaming Large PDF Files

**Question**: How to handle PDF generation for reports with 100+ receipts without memory issues?

**Decision**: Streaming PDF generation with file-based intermediates

**Rationale**:
- PdfSharpCore supports incremental page addition
- Store intermediate results in temp files if memory pressure detected
- Set reasonable limits (warn at 50, limit at 100 receipts)

**Memory Management Strategy**:
```csharp
public async Task<Stream> GenerateReceiptPdfAsync(Guid reportId)
{
    var receipts = await GetOrderedReceiptsAsync(reportId);

    if (receipts.Count > 100)
        throw new BusinessException("Report exceeds 100 receipts. Please split into multiple reports.");

    using var outputDoc = new PdfDocument();

    foreach (var receipt in receipts)
    {
        await AddReceiptToPdfAsync(outputDoc, receipt);

        // Yield to prevent CPU monopolization
        if (receipts.IndexOf(receipt) % 10 == 0)
            await Task.Yield();
    }

    var stream = new MemoryStream();
    outputDoc.Save(stream, false);
    stream.Position = 0;
    return stream;
}
```

---

## Dependencies Summary

| Package | Version | Purpose |
|---------|---------|---------|
| ClosedXML | 0.102.x | Excel generation with formula preservation |
| PdfSharpCore | 1.3.x | PDF generation and merging |
| SixLabors.ImageSharp | 3.1.x | Image to PDF conversion |

---

## Constitution Compliance

| Principle | Compliance |
|-----------|------------|
| I. Cost-First AI | ✅ No AI used in output generation (pure data transformation) |
| II. Self-Improving | ✅ N/A - read-only output operations |
| III. Receipt Accountability | ✅ Placeholders with justification for missing receipts |
| IV. Infrastructure Optimization | ✅ Uses existing AKS, no new managed services |
| V. Cache-First | ✅ N/A - no cacheable operations |

---

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Large PDF memory usage | Streaming generation, 100-receipt limit |
| Excel template drift | Store template in blob storage, version control |
| MoM query performance | Indexed columns, limit to single user context |
| TierUsageLog volume | Existing indexes sufficient; partition if needed |
