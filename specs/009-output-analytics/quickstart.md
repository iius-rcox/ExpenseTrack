# Quickstart: Output Generation & Analytics

**Feature**: 009-output-analytics
**Date**: 2025-12-16

## Prerequisites

Before starting Sprint 9 implementation, ensure:

- [ ] Sprint 8 (Draft Report Generation) is complete
- [ ] ExpenseReport and ExpenseLine entities exist and are populated
- [ ] TierUsageLog from Sprint 6 is capturing tier usage metrics
- [ ] Receipt files are stored in Azure Blob Storage (ccproctemp2025)
- [ ] AP Excel template is available (see Template Setup below)

## Quick Setup

### 1. Install Required NuGet Packages

```bash
cd backend/src/ExpenseFlow.Infrastructure

# Excel generation with formula support
dotnet add package ClosedXML --version 0.102.2

# PDF generation and merging
dotnet add package PdfSharpCore --version 1.3.62

# Image to PDF conversion
dotnet add package SixLabors.ImageSharp --version 3.1.5
```

### 2. Template Setup

Upload the AP expense report template to blob storage:

```powershell
# Create templates container
az storage container create `
  --account-name ccproctemp2025 `
  --name templates `
  --auth-mode login

# Upload template (adjust path to actual template file)
az storage blob upload `
  --account-name ccproctemp2025 `
  --container-name templates `
  --name expense-report-template.xlsx `
  --file ./templates/expense-report-template.xlsx `
  --auth-mode login
```

### 3. Register Services

Add to `Program.cs` or DI configuration:

```csharp
// Export services
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

// Analytics services
builder.Services.AddScoped<IComparisonService, MonthlyComparisonService>();
builder.Services.AddScoped<ICacheStatisticsService, CacheStatisticsService>();
```

### 4. Configuration

Add to `appsettings.json`:

```json
{
  "Export": {
    "TemplateBlobContainer": "templates",
    "TemplateFileName": "expense-report-template.xlsx",
    "MaxReceiptsPerPdf": 100,
    "DateFormat": "MM/dd/yy"
  },
  "Analytics": {
    "Tier1HitRateTarget": 50.0,
    "Tier2CostPerOperation": 0.00002,
    "Tier3CostPerOperation": 0.0003
  }
}
```

## Implementation Order

### Phase 1: Core Export (P1 Stories)

1. **ExcelExportService** - US-013: Excel report generation
   - Create service interface in `ExpenseFlow.Core/Interfaces/IExcelExportService.cs`
   - Implement in `ExpenseFlow.Infrastructure/Services/ExcelExportService.cs`
   - Add `GET /api/reports/{reportId}/excel` endpoint

2. **PdfGenerationService** - US-014: Receipt PDF consolidation
   - Create service interface in `ExpenseFlow.Core/Interfaces/IPdfGenerationService.cs`
   - Implement in `ExpenseFlow.Infrastructure/Services/PdfGenerationService.cs`
   - Add `GET /api/reports/{reportId}/receipts.pdf` endpoint

3. **MissingReceiptPlaceholder** - Part of US-014
   - Implement placeholder page generation within PdfGenerationService
   - Include all required fields from spec (date, vendor, amount, justification)

### Phase 2: Analytics (P2-P3 Stories)

4. **MonthlyComparisonService** - US-015: MoM comparison
   - Create service interface and implementation
   - Add `GET /api/analytics/comparison` endpoint
   - Implement SQL-based comparison query

5. **CacheStatisticsService** - Cache performance dashboard
   - Create service interface and implementation
   - Add `GET /api/analytics/cache-stats` endpoint
   - Aggregate from TierUsageLog table

## Testing Commands

### Verify Excel Export

```bash
# Generate Excel for report
curl -X GET "https://localhost:7001/api/reports/{reportId}/excel" \
  -H "Authorization: Bearer {token}" \
  -o test-export.xlsx

# Verify file opens and formulas work
```

### Verify PDF Generation

```bash
# Generate receipt PDF
curl -X GET "https://localhost:7001/api/reports/{reportId}/receipts.pdf" \
  -H "Authorization: Bearer {token}" \
  -o test-receipts.pdf

# Check headers for page/placeholder counts
curl -I "https://localhost:7001/api/reports/{reportId}/receipts.pdf" \
  -H "Authorization: Bearer {token}"
```

### Verify MoM Comparison

```bash
curl -X GET "https://localhost:7001/api/analytics/comparison?current=2025-01&previous=2024-12" \
  -H "Authorization: Bearer {token}" | jq
```

### Verify Cache Statistics

```bash
curl -X GET "https://localhost:7001/api/analytics/cache-stats?period=last30days&groupBy=operation" \
  -H "Authorization: Bearer {token}" | jq
```

## Key Implementation Patterns

### Excel Generation with ClosedXML

```csharp
public async Task<byte[]> GenerateExcelAsync(ExpenseReport report)
{
    // Download template from blob storage
    var templateStream = await _blobService.DownloadAsync(
        _options.TemplateBlobContainer,
        _options.TemplateFileName);

    using var workbook = new XLWorkbook(templateStream);
    var worksheet = workbook.Worksheet(1);

    // Fill header section
    worksheet.Cell("B2").Value = report.User.DisplayName;
    worksheet.Cell("B3").Value = report.Period;

    // Fill expense lines (starting at row 7)
    var startRow = 7;
    foreach (var line in report.Lines.OrderBy(l => l.LineOrder))
    {
        var row = startRow + line.LineOrder - 1;
        worksheet.Cell(row, 1).Value = line.ExpenseDate.ToString("MM/dd/yy");
        worksheet.Cell(row, 2).Value = line.GLCode;
        worksheet.Cell(row, 3).Value = line.DepartmentCode;
        worksheet.Cell(row, 4).Value = line.NormalizedDescription;
        worksheet.Cell(row, 5).Value = 1; // Units default
        worksheet.Cell(row, 6).Value = line.Amount;
        // Column G formula already in template: =IF(ISBLANK(E7),"",E7*F7)
    }

    using var outputStream = new MemoryStream();
    workbook.SaveAs(outputStream);
    return outputStream.ToArray();
}
```

### PDF Generation with PdfSharpCore

```csharp
public async Task<byte[]> GenerateReceiptPdfAsync(ExpenseReport report)
{
    using var outputDoc = new PdfDocument();

    foreach (var line in report.Lines.OrderBy(l => l.LineOrder))
    {
        if (line.HasReceipt && line.Receipt != null)
        {
            await AddReceiptPagesAsync(outputDoc, line.Receipt);
        }
        else
        {
            AddPlaceholderPage(outputDoc, line, report.User.DisplayName);
        }
    }

    using var stream = new MemoryStream();
    outputDoc.Save(stream, false);
    return stream.ToArray();
}

private void AddPlaceholderPage(PdfDocument doc, ExpenseLine line, string employeeName)
{
    var page = doc.AddPage();
    page.Size = PageSize.Letter;

    using var gfx = XGraphics.FromPdfPage(page);
    var font = new XFont("Arial", 12);
    var boldFont = new XFont("Arial", 14, XFontStyleEx.Bold);

    // Draw placeholder content
    gfx.DrawString("MISSING RECEIPT", boldFont, XBrushes.Red,
        new XRect(0, 50, page.Width, 30), XStringFormats.Center);

    var y = 120;
    gfx.DrawString($"Expense Date: {line.ExpenseDate:MMMM dd, yyyy}", font, XBrushes.Black, 72, y);
    y += 25;
    gfx.DrawString($"Vendor: {line.VendorName ?? "Unknown"}", font, XBrushes.Black, 72, y);
    y += 25;
    gfx.DrawString($"Amount: {line.Amount:C}", font, XBrushes.Black, 72, y);
    y += 25;
    gfx.DrawString($"Description: {line.NormalizedDescription}", font, XBrushes.Black, 72, y);
    y += 40;
    gfx.DrawString($"Justification: {GetJustificationText(line.MissingReceiptJustification)}", font, XBrushes.Black, 72, y);
    if (line.MissingReceiptJustification == MissingReceiptJustification.Other)
    {
        y += 25;
        gfx.DrawString($"Note: {line.JustificationNote}", font, XBrushes.Black, 72, y);
    }
    y += 50;
    gfx.DrawString($"Employee: {employeeName}", font, XBrushes.Black, 72, y);
}
```

### MoM Comparison Query (Entity Framework)

```csharp
public async Task<MonthlyComparisonDto> GetComparisonAsync(
    Guid userId, string currentPeriod, string previousPeriod)
{
    var currentVendors = await _context.ExpenseLines
        .Where(el => el.Report.UserId == userId
            && el.Report.Period == currentPeriod
            && !el.Report.IsDeleted)
        .GroupBy(el => el.VendorName)
        .Select(g => new { Vendor = g.Key, Total = g.Sum(x => x.Amount) })
        .ToListAsync();

    var previousVendors = await _context.ExpenseLines
        .Where(el => el.Report.UserId == userId
            && el.Report.Period == previousPeriod
            && !el.Report.IsDeleted)
        .GroupBy(el => el.VendorName)
        .Select(g => new { Vendor = g.Key, Total = g.Sum(x => x.Amount) })
        .ToListAsync();

    // Calculate recurring vendors (appeared 2+ consecutive months before previous)
    var recurringVendors = await GetRecurringVendorsAsync(userId, previousPeriod);

    // Build comparison
    return new MonthlyComparisonDto
    {
        CurrentPeriod = currentPeriod,
        PreviousPeriod = previousPeriod,
        Summary = CalculateSummary(currentVendors, previousVendors),
        NewVendors = FindNewVendors(currentVendors, previousVendors),
        MissingRecurring = FindMissingRecurring(currentVendors, previousVendors, recurringVendors),
        SignificantChanges = FindSignificantChanges(currentVendors, previousVendors)
    };
}
```

## Success Criteria Verification

| Criterion | How to Verify |
|-----------|---------------|
| SC-001: Excel < 5 seconds | Time `/api/reports/{id}/excel` with 50-line report |
| SC-002: 100% AP template match | Manual validation with AP department |
| SC-003: PDF < 30 seconds | Time `/api/reports/{id}/receipts.pdf` with 50 receipts |
| SC-004: PDF < 20MB average | Check Content-Length header on typical reports |
| SC-005: Complete placeholders | Visual inspection of placeholder pages |
| SC-006: 95% MoM detection | Compare known test data against results |
| SC-007: Dashboard < 2 seconds | Time `/api/analytics/cache-stats` response |
| SC-008: Cost < $40/month | Monitor cache-stats estimatedMonthlyCost |

## Common Issues

### Issue: Excel formulas showing as text

**Solution**: Ensure template file has formulas, not ClosedXML-generated formulas. The template should have `=IF(ISBLANK(E7),"",E7*F7)` pre-configured.

### Issue: PDF generation memory errors

**Solution**: Implement streaming and check `MaxReceiptsPerPdf` limit (default 100). For large reports, split into multiple PDFs.

### Issue: MoM comparison returning empty

**Solution**: Verify both periods have expense reports for the authenticated user. Check `is_deleted` filter is working correctly.

### Issue: Cache stats showing 0% Tier 1

**Solution**: Ensure Sprint 6 TierUsageLog is being populated by categorization service. Check `operation_type` values match expected enum.
