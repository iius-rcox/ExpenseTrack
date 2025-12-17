# Implementation Tasks: Output Generation & Analytics

**Feature Branch**: `009-output-analytics`
**Generated**: 2025-12-16
**Input Sources**: spec.md, plan.md, data-model.md, research.md, contracts/output-api.yaml

## Task Overview

| Phase | Description | Task Count |
|-------|-------------|------------|
| Phase 0 | Setup (NuGet Packages & Configuration) | 3 |
| Phase 1 | Foundational (DTOs & Interfaces) | 6 |
| Phase 2 | User Story 1 - Excel Export (P1) | 4 |
| Phase 3 | User Story 2 & 3 - PDF Generation (P1) | 5 |
| Phase 4 | User Story 4 - MoM Comparison (P2) | 4 |
| Phase 5 | User Story 5 - Cache Statistics (P3) | 4 |
| Phase 6 | Polish & Validation | 3 |
| **Total** | | **29** |

---

## Phase 0: Setup (NuGet Packages & Configuration)

### Task 0.1: Install NuGet Packages
**Priority**: P1 | **Story**: All | **Estimate**: XS

**Description**: Install required NuGet packages for Excel and PDF generation.

**Commands**:
```bash
cd backend/src/ExpenseFlow.Infrastructure
dotnet add package ClosedXML --version 0.102.2
dotnet add package PdfSharpCore --version 1.3.62
dotnet add package SixLabors.ImageSharp --version 3.1.5
dotnet add package HeyRed.ImageSharp.Heif --version 2.1.3
dotnet add package LibHeif.Native --version 1.15.1
```

**Acceptance**:
- [X] ClosedXML 0.102.2 installed
- [X] PdfSharpCore 1.3.62 installed
- [X] SixLabors.ImageSharp 3.1.5 installed
- [X] HeyRed.ImageSharp.Heif 2.1.3 + LibHeif.Native 1.15.1 installed (HEIC support)
- [X] Solution builds without errors

**Dependencies**: None

---

### Task 0.2: Add Export Configuration
**Priority**: P1 | **Story**: US1, US2 | **Estimate**: XS

**Description**: Add configuration section to appsettings.json for export settings.

**Files**:
- Modify: `backend/src/ExpenseFlow.Api/appsettings.json`

**Implementation**:
```json
{
  "Export": {
    "TemplateBlobContainer": "templates",
    "TemplateFileName": "expense-report-template.xlsx",
    "MaxReceiptsPerPdf": 100,
    "DateFormat": "MM/dd/yy"
  }
}
```

**Acceptance**:
- [X] Export section added to appsettings.json
- [X] Template container and filename configured
- [X] MaxReceiptsPerPdf limit set to 100

**Dependencies**: None

---

### Task 0.3: Add Analytics Configuration
**Priority**: P2 | **Story**: US5 | **Estimate**: XS

**Description**: Add configuration section for analytics cost estimation.

**Files**:
- Modify: `backend/src/ExpenseFlow.Api/appsettings.json`

**Implementation**:
```json
{
  "Analytics": {
    "Tier1HitRateTarget": 50.0,
    "Tier2CostPerOperation": 0.00002,
    "Tier3CostPerOperation": 0.0003
  }
}
```

**Acceptance**:
- [X] Analytics section added to appsettings.json
- [X] Cost estimation parameters configured

**Dependencies**: None

---

## Phase 1: Foundational (DTOs & Interfaces)

**Purpose**: Create DTOs and interfaces that all user stories depend on

**CRITICAL**: No user story implementation can begin until DTOs are complete

### Task 1.1: Create Export DTOs
**Priority**: P1 | **Story**: US1, US2 | **Estimate**: S

**Description**: Create DTOs for Excel and PDF export responses.

**Files**:
- Create: `backend/src/ExpenseFlow.Shared/DTOs/ExcelExportDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/ReceiptPdfDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/MissingReceiptPlaceholderDto.cs`

**Implementation**:
Per data-model.md:
- ExcelExportDto: FileName, ContentType, FileContents
- ReceiptPdfDto: FileName, ContentType, FileContents, PageCount, PlaceholderCount
- MissingReceiptPlaceholderDto: ExpenseDate, VendorName, Amount, Description, Justification, JustificationNote, EmployeeName, ReportId

**Acceptance**:
- [X] All DTOs match data-model.md specifications
- [X] Proper nullable annotations
- [X] XML documentation comments added

**Dependencies**: None

---

### Task 1.2: Create Analytics DTOs
**Priority**: P2 | **Story**: US4, US5 | **Estimate**: S

**Description**: Create DTOs for month-over-month comparison and cache statistics.

**Files**:
- Create: `backend/src/ExpenseFlow.Shared/DTOs/MonthlyComparisonDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/ComparisonSummaryDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/VendorAmountDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/VendorChangeDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/CacheStatisticsDto.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/CacheStatisticsResponse.cs`
- Create: `backend/src/ExpenseFlow.Shared/DTOs/CacheStatsByOperationDto.cs`

**Implementation**:
Per contracts/output-api.yaml schemas and data-model.md.

**Acceptance**:
- [X] All DTOs match OpenAPI contract schemas
- [X] Decimal types for monetary values
- [X] Nullable types for optional fields

**Dependencies**: None

---

### Task 1.3: Create IExcelExportService Interface
**Priority**: P1 | **Story**: US1 | **Estimate**: XS

**Description**: Define interface for Excel export functionality.

**Files**:
- Create: `backend/src/ExpenseFlow.Core/Interfaces/IExcelExportService.cs`

**Implementation**:
```csharp
public interface IExcelExportService
{
    Task<byte[]> GenerateExcelAsync(Guid reportId, CancellationToken ct = default);
}
```

**Acceptance**:
- [X] Interface follows existing naming patterns
- [X] CancellationToken parameter included

**Dependencies**: None

---

### Task 1.4: Create IPdfGenerationService Interface
**Priority**: P1 | **Story**: US2, US3 | **Estimate**: XS

**Description**: Define interface for PDF generation and receipt consolidation.

**Files**:
- Create: `backend/src/ExpenseFlow.Core/Interfaces/IPdfGenerationService.cs`

**Implementation**:
```csharp
public interface IPdfGenerationService
{
    Task<ReceiptPdfDto> GenerateReceiptPdfAsync(Guid reportId, CancellationToken ct = default);
}
```

**Acceptance**:
- [X] Returns ReceiptPdfDto with page/placeholder counts
- [X] CancellationToken parameter included

**Dependencies**: Task 1.1

---

### Task 1.5: Create IComparisonService Interface
**Priority**: P2 | **Story**: US4 | **Estimate**: XS

**Description**: Define interface for month-over-month comparison.

**Files**:
- Create: `backend/src/ExpenseFlow.Core/Interfaces/IComparisonService.cs`

**Implementation**:
```csharp
public interface IComparisonService
{
    Task<MonthlyComparisonDto> GetComparisonAsync(
        Guid userId,
        string currentPeriod,
        string previousPeriod,
        CancellationToken ct = default);
}
```

**Acceptance**:
- [X] Interface matches analytics endpoint requirements
- [X] Period parameters as strings (YYYY-MM format)

**Dependencies**: Task 1.2

---

### Task 1.6: Create ICacheStatisticsService Interface
**Priority**: P3 | **Story**: US5 | **Estimate**: XS

**Description**: Define interface for cache statistics aggregation.

**Files**:
- Create: `backend/src/ExpenseFlow.Core/Interfaces/ICacheStatisticsService.cs`

**Implementation**:
```csharp
public interface ICacheStatisticsService
{
    Task<CacheStatisticsResponse> GetStatisticsAsync(
        Guid userId,
        string period,
        string? groupBy,
        CancellationToken ct = default);
}
```

**Acceptance**:
- [X] Supports period filtering (YYYY-MM, last30days, last7days)
- [X] Optional groupBy parameter

**Dependencies**: Task 1.2

---

## Phase 2: User Story 1 - Excel Export (Priority: P1)

**Goal**: Generate Excel expense reports matching AP department template format

**Independent Test**: Export a draft report with multiple expense lines and verify output matches AP template structure with working formulas

### Task 2.1: Implement ExcelExportService
**Priority**: P1 | **Story**: US1 | **Estimate**: L

**Description**: Implement Excel generation using ClosedXML with template-based approach.

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Services/ExcelExportService.cs`

**Implementation**:
Per quickstart.md pattern:
1. Download template from blob storage
2. Open workbook with XLWorkbook
3. Fill header section (employee name, period)
4. Fill expense rows starting at row 7
5. Preserve formulas in column G
6. Return serialized byte array

**Key Logic**:
```csharp
public async Task<byte[]> GenerateExcelAsync(Guid reportId, CancellationToken ct)
{
    var report = await _reportRepository.GetByIdWithLinesAsync(reportId, ct);
    var templateStream = await _blobService.DownloadAsync(_options.TemplateBlobContainer, _options.TemplateFileName);

    using var workbook = new XLWorkbook(templateStream);
    var worksheet = workbook.Worksheet(1);

    // Fill header
    worksheet.Cell("B2").Value = report.User.DisplayName;
    worksheet.Cell("B3").Value = report.Period;

    // Fill expense lines
    var startRow = 7;
    foreach (var line in report.Lines.OrderBy(l => l.LineOrder))
    {
        var row = startRow + line.LineOrder - 1;
        worksheet.Cell(row, 1).Value = line.ExpenseDate.ToString(_options.DateFormat);
        worksheet.Cell(row, 2).Value = line.GLCode;
        worksheet.Cell(row, 3).Value = line.DepartmentCode;
        worksheet.Cell(row, 4).Value = line.NormalizedDescription;
        worksheet.Cell(row, 5).Value = 1; // Units default
        worksheet.Cell(row, 6).Value = line.Amount;
    }

    using var outputStream = new MemoryStream();
    workbook.SaveAs(outputStream);
    return outputStream.ToArray();
}
```

**Acceptance**:
- [ ] Template downloaded from blob storage
- [ ] Header section populated (FR-003)
- [ ] All columns mapped per AP template (FR-001)
- [ ] Formulas preserved, not converted to static values (FR-002)
- [ ] Performance: <5 seconds for 50 lines (SC-001)

**Dependencies**: Task 1.3, Task 0.2

---

### Task 2.2: Create ExportOptions Configuration Class
**Priority**: P1 | **Story**: US1 | **Estimate**: XS

**Description**: Create strongly-typed options class for export configuration.

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Configuration/ExportOptions.cs`

**Implementation**:
```csharp
public class ExportOptions
{
    public const string SectionName = "Export";
    public string TemplateBlobContainer { get; set; } = "templates";
    public string TemplateFileName { get; set; } = "expense-report-template.xlsx";
    public int MaxReceiptsPerPdf { get; set; } = 100;
    public string DateFormat { get; set; } = "MM/dd/yy";
}
```

**Acceptance**:
- [ ] Options class matches appsettings.json structure
- [ ] Default values provided

**Dependencies**: Task 0.2

---

### Task 2.3: Register ExcelExportService and Options
**Priority**: P1 | **Story**: US1 | **Estimate**: XS

**Description**: Register Excel export service and options in DI container.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/ServiceCollectionExtensions.cs`

**Implementation**:
```csharp
// Sprint 9: Output Generation & Analytics
services.Configure<ExportOptions>(configuration.GetSection(ExportOptions.SectionName));
services.AddScoped<IExcelExportService, ExcelExportService>();
```

**Acceptance**:
- [ ] Options bound to configuration
- [ ] Service registered as scoped

**Dependencies**: Task 2.1, Task 2.2

---

### Task 2.4: Add Excel Export Endpoint to ReportsController
**Priority**: P1 | **Story**: US1 | **Estimate**: S

**Description**: Add GET endpoint for Excel export to existing ReportsController.

**Files**:
- Modify: `backend/src/ExpenseFlow.Api/Controllers/ReportsController.cs`

**Implementation**:
Per contracts/output-api.yaml:
```csharp
[HttpGet("{reportId}/excel")]
[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> ExportToExcel(Guid reportId, CancellationToken ct)
{
    var report = await _reportService.GetReportAsync(UserId, reportId, ct);
    if (report == null)
        return NotFound();

    if (report.LineCount == 0)
        return BadRequest(new ProblemDetails { Title = "Report has zero expense lines" });

    var excelBytes = await _excelExportService.GenerateExcelAsync(reportId, ct);

    return File(
        excelBytes,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        $"{report.Period}-expense-report.xlsx");
}
```

**Acceptance**:
- [ ] Returns 200 with Excel file on success
- [ ] Returns 400 if report has zero lines
- [ ] Returns 404 if report not found
- [ ] Content-Disposition header set with filename

**Dependencies**: Task 2.3

---

**Checkpoint**: User Story 1 (Excel Export) should be fully functional and testable independently

---

## Phase 3: User Story 2 & 3 - PDF Generation (Priority: P1)

**Goal**: Generate consolidated receipt PDF with missing receipt placeholders

**Independent Test**: Generate PDF from report with mixed receipts (some present, some missing) and verify ordering and placeholder content

### Task 3.1: Implement PdfGenerationService - Receipt Consolidation
**Priority**: P1 | **Story**: US2 | **Estimate**: L

**Description**: Implement PDF consolidation for existing receipt files (PDF, JPG, PNG).

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Services/PdfGenerationService.cs`

**Implementation**:
Per quickstart.md pattern:
1. Load report with lines and receipts
2. Validate receipt count <= MaxReceiptsPerPdf
3. For each line in LineOrder:
   - If HasReceipt: Add receipt pages (PDF merge or image convert)
   - Else: Add placeholder page
4. Return ReceiptPdfDto with counts

**Key Logic for Receipt Addition**:
```csharp
private async Task AddReceiptPagesAsync(PdfDocument outputDoc, Receipt receipt, CancellationToken ct)
{
    var receiptStream = await _blobService.DownloadAsync(receipt.BlobUrl);

    if (receipt.FileType == ".pdf")
    {
        using var inputDoc = PdfReader.Open(receiptStream, PdfDocumentOpenMode.Import);
        foreach (var page in inputDoc.Pages)
            outputDoc.AddPage(page);
    }
    else // Image (JPG, PNG, HEIC/HEIF)
    {
        // ImageSharp automatically handles HEIC/HEIF with Formats.Heif package
        using var image = await Image.LoadAsync(receiptStream, ct);
        var page = outputDoc.AddPage();
        page.Size = PageSize.Letter;

        using var gfx = XGraphics.FromPdfPage(page);
        using var xImage = XImage.FromStream(() => new MemoryStream(ImageToBytes(image)));

        // Scale to fit page with margins
        var scale = Math.Min(
            (page.Width - 72) / xImage.PixelWidth,
            (page.Height - 72) / xImage.PixelHeight);

        gfx.DrawImage(xImage, 36, 36, xImage.PixelWidth * scale, xImage.PixelHeight * scale);
    }
}
```

**Acceptance**:
- [ ] PDF receipts merged page-by-page
- [ ] JPG/PNG converted to PDF pages (FR-004)
- [ ] Receipts ordered by expense line sequence (FR-005)
- [ ] Multi-page receipts kept together
- [ ] Performance: <30 seconds for 50 receipts (SC-003)

**Dependencies**: Task 1.4, Task 2.2

---

### Task 3.2: Implement PdfGenerationService - Placeholder Pages
**Priority**: P1 | **Story**: US3 | **Estimate**: M

**Description**: Implement missing receipt placeholder page generation.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/Services/PdfGenerationService.cs`

**Implementation**:
Per quickstart.md pattern:
```csharp
private void AddPlaceholderPage(PdfDocument doc, ExpenseLine line, string employeeName)
{
    var page = doc.AddPage();
    page.Size = PageSize.Letter;

    using var gfx = XGraphics.FromPdfPage(page);
    var font = new XFont("Arial", 12);
    var boldFont = new XFont("Arial", 14, XFontStyleEx.Bold);

    // Header
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

private static string GetJustificationText(MissingReceiptJustification? justification) => justification switch
{
    MissingReceiptJustification.NotProvided => "Not provided by vendor",
    MissingReceiptJustification.Lost => "Lost",
    MissingReceiptJustification.DigitalSubscription => "Digital subscription - no receipt issued",
    MissingReceiptJustification.Other => "Other",
    _ => "Not specified"
};
```

**Acceptance**:
- [ ] Placeholder includes expense date, vendor, amount, description (FR-007)
- [ ] Placeholder includes justification reason (FR-007)
- [ ] "Other" justification shows custom note
- [ ] Justification options match FR-008

**Dependencies**: Task 3.1

---

### Task 3.3: Add Validation for Missing Receipt Justifications
**Priority**: P1 | **Story**: US3 | **Estimate**: S

**Description**: Validate that all missing receipts have justifications before PDF generation.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/Services/PdfGenerationService.cs`

**Implementation**:
```csharp
public async Task<ReceiptPdfDto> GenerateReceiptPdfAsync(Guid reportId, CancellationToken ct)
{
    var report = await _reportRepository.GetByIdWithLinesAsync(reportId, ct);

    // Validate receipt limit
    if (report.Lines.Count > _options.MaxReceiptsPerPdf)
        throw new ValidationException($"Report exceeds {_options.MaxReceiptsPerPdf} receipt limit");

    // Validate justifications
    var missingJustifications = report.Lines
        .Where(l => !l.HasReceipt && l.MissingReceiptJustification == null)
        .ToList();

    if (missingJustifications.Any())
        throw new ValidationException("Missing receipt justifications incomplete");

    // Generate PDF...
}
```

**Acceptance**:
- [ ] Throws ValidationException if receipt limit exceeded
- [ ] Throws ValidationException if missing justifications incomplete
- [ ] Error messages are user-friendly

**Dependencies**: Task 3.2

---

### Task 3.4: Register PdfGenerationService
**Priority**: P1 | **Story**: US2, US3 | **Estimate**: XS

**Description**: Register PDF generation service in DI container.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/ServiceCollectionExtensions.cs`

**Implementation**:
```csharp
services.AddScoped<IPdfGenerationService, PdfGenerationService>();
```

**Acceptance**:
- [ ] Service registered as scoped

**Dependencies**: Task 3.3

---

### Task 3.5: Add PDF Export Endpoint to ReportsController
**Priority**: P1 | **Story**: US2, US3 | **Estimate**: S

**Description**: Add GET endpoint for receipt PDF export.

**Files**:
- Modify: `backend/src/ExpenseFlow.Api/Controllers/ReportsController.cs`

**Implementation**:
Per contracts/output-api.yaml:
```csharp
[HttpGet("{reportId}/receipts.pdf")]
[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> DownloadReceiptsPdf(Guid reportId, CancellationToken ct)
{
    var report = await _reportService.GetReportAsync(UserId, reportId, ct);
    if (report == null)
        return NotFound();

    try
    {
        var result = await _pdfGenerationService.GenerateReceiptPdfAsync(reportId, ct);

        Response.Headers.Append("X-Page-Count", result.PageCount.ToString());
        Response.Headers.Append("X-Placeholder-Count", result.PlaceholderCount.ToString());

        return File(result.FileContents, "application/pdf", $"{report.Period}-receipts.pdf");
    }
    catch (ValidationException ex)
    {
        return BadRequest(new ProblemDetails { Title = ex.Message });
    }
}
```

**Acceptance**:
- [ ] Returns 200 with PDF file on success
- [ ] Returns 400 if receipt limit exceeded or justifications missing
- [ ] Returns 404 if report not found
- [ ] X-Page-Count and X-Placeholder-Count headers set

**Dependencies**: Task 3.4

---

**Checkpoint**: User Stories 2 & 3 (PDF Generation with Placeholders) should be fully functional and testable independently

---

## Phase 4: User Story 4 - MoM Comparison (Priority: P2)

**Goal**: Provide month-over-month spending comparison with anomaly detection

**Independent Test**: Compare two months of expense data and verify new vendors, missing recurring, and significant changes are correctly identified

### Task 4.1: Implement MonthlyComparisonService
**Priority**: P2 | **Story**: US4 | **Estimate**: L

**Description**: Implement month-over-month comparison with SQL-based aggregations.

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Services/MonthlyComparisonService.cs`

**Implementation**:
Per quickstart.md pattern using Entity Framework:
```csharp
public async Task<MonthlyComparisonDto> GetComparisonAsync(
    Guid userId, string currentPeriod, string previousPeriod, CancellationToken ct)
{
    // Get current period vendors
    var currentVendors = await _context.ExpenseLines
        .Where(el => el.Report.UserId == userId
            && el.Report.Period == currentPeriod
            && !el.Report.IsDeleted)
        .GroupBy(el => el.VendorName)
        .Select(g => new { Vendor = g.Key, Total = g.Sum(x => x.Amount) })
        .ToListAsync(ct);

    // Get previous period vendors
    var previousVendors = await _context.ExpenseLines
        .Where(el => el.Report.UserId == userId
            && el.Report.Period == previousPeriod
            && !el.Report.IsDeleted)
        .GroupBy(el => el.VendorName)
        .Select(g => new { Vendor = g.Key, Total = g.Sum(x => x.Amount) })
        .ToListAsync(ct);

    // Get recurring vendors (2+ consecutive months before previous)
    var recurringVendors = await GetRecurringVendorsAsync(userId, previousPeriod, ct);

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

**Key Methods**:
- `FindNewVendors`: Vendors in current but not previous (FR-010)
- `FindMissingRecurring`: Vendors in 2+ months before but not current (FR-011)
- `FindSignificantChanges`: Vendors with >50% variance (FR-012)
- `CalculateSummary`: Totals and percentage change (FR-009)

**Acceptance**:
- [ ] Identifies new vendors correctly (FR-010)
- [ ] Identifies missing recurring vendors (FR-011)
- [ ] Identifies significant changes >50% (FR-012)
- [ ] Summary includes totals and percentage change (FR-009)
- [ ] Accuracy: 95%+ detection rate (SC-006)

**Dependencies**: Task 1.5

---

### Task 4.2: Register MonthlyComparisonService
**Priority**: P2 | **Story**: US4 | **Estimate**: XS

**Description**: Register comparison service in DI container.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/ServiceCollectionExtensions.cs`

**Implementation**:
```csharp
services.AddScoped<IComparisonService, MonthlyComparisonService>();
```

**Acceptance**:
- [ ] Service registered as scoped

**Dependencies**: Task 4.1

---

### Task 4.3: Create AnalyticsController
**Priority**: P2 | **Story**: US4, US5 | **Estimate**: S

**Description**: Create new controller for analytics endpoints.

**Files**:
- Create: `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs`

**Implementation**:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IComparisonService _comparisonService;
    private readonly ICacheStatisticsService _cacheStatisticsService;

    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Endpoints added in subsequent tasks
}
```

**Acceptance**:
- [ ] Controller follows existing patterns
- [ ] [Authorize] attribute applied
- [ ] UserId helper property implemented

**Dependencies**: Task 4.2

---

### Task 4.4: Add Comparison Endpoint
**Priority**: P2 | **Story**: US4 | **Estimate**: S

**Description**: Add GET endpoint for month-over-month comparison.

**Files**:
- Modify: `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs`

**Implementation**:
Per contracts/output-api.yaml:
```csharp
[HttpGet("comparison")]
[ProducesResponseType(typeof(MonthlyComparisonDto), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetComparison(
    [FromQuery] string current,
    [FromQuery] string previous,
    CancellationToken ct)
{
    // Validate period format (YYYY-MM)
    if (!IsValidPeriod(current) || !IsValidPeriod(previous))
        return BadRequest(new ProblemDetails { Title = "Invalid period format. Use YYYY-MM." });

    if (current == previous)
        return BadRequest(new ProblemDetails { Title = "Current and previous periods must be different." });

    var result = await _comparisonService.GetComparisonAsync(UserId, current, previous, ct);

    if (result.Summary.CurrentTotal == 0 && result.Summary.PreviousTotal == 0)
        return NotFound(new ProblemDetails { Title = "No data for specified periods." });

    return Ok(result);
}

private static bool IsValidPeriod(string period) =>
    Regex.IsMatch(period, @"^\d{4}-(0[1-9]|1[0-2])$");
```

**Acceptance**:
- [ ] Validates YYYY-MM format
- [ ] Returns 400 if periods are equal
- [ ] Returns 404 if no data for periods
- [ ] Returns MonthlyComparisonDto on success

**Dependencies**: Task 4.3

---

**Checkpoint**: User Story 4 (MoM Comparison) should be fully functional and testable independently

---

## Phase 5: User Story 5 - Cache Statistics (Priority: P3)

**Goal**: Display cache tier usage statistics and cost estimates

**Independent Test**: Process expenses through tiered system and verify statistics accurately reflect actual tier usage

### Task 5.1: Create AnalyticsOptions Configuration Class
**Priority**: P3 | **Story**: US5 | **Estimate**: XS

**Description**: Create strongly-typed options class for analytics configuration.

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Configuration/AnalyticsOptions.cs`

**Implementation**:
```csharp
public class AnalyticsOptions
{
    public const string SectionName = "Analytics";
    public decimal Tier1HitRateTarget { get; set; } = 50.0m;
    public decimal Tier2CostPerOperation { get; set; } = 0.00002m;
    public decimal Tier3CostPerOperation { get; set; } = 0.0003m;
}
```

**Acceptance**:
- [ ] Options class matches appsettings.json structure
- [ ] Default values provided

**Dependencies**: Task 0.3

---

### Task 5.2: Implement CacheStatisticsService
**Priority**: P3 | **Story**: US5 | **Estimate**: M

**Description**: Implement cache statistics aggregation from TierUsageLog.

**Files**:
- Create: `backend/src/ExpenseFlow.Infrastructure/Services/CacheStatisticsService.cs`

**Implementation**:
Per data-model.md query pattern:
```csharp
public async Task<CacheStatisticsResponse> GetStatisticsAsync(
    Guid userId, string period, string? groupBy, CancellationToken ct)
{
    var (startDate, endDate) = ParsePeriod(period);

    var query = _context.TierUsageLogs
        .Where(t => t.UserId == userId
            && t.CreatedAt >= startDate
            && t.CreatedAt <= endDate);

    var overall = await CalculateOverallStatsAsync(query, ct);

    List<CacheStatsByOperationDto>? byOperation = null;
    if (groupBy == "operation")
    {
        byOperation = await query
            .GroupBy(t => t.OperationType)
            .Select(g => new CacheStatsByOperationDto
            {
                OperationType = g.Key,
                Tier1Hits = g.Count(x => x.TierUsed == 1),
                Tier2Hits = g.Count(x => x.TierUsed == 2),
                Tier3Hits = g.Count(x => x.TierUsed == 3),
                Tier1HitRate = CalculateRate(g.Count(x => x.TierUsed == 1), g.Count())
            })
            .ToListAsync(ct);
    }

    return new CacheStatisticsResponse
    {
        Period = period,
        Overall = overall,
        ByOperation = byOperation
    };
}

private (DateTime start, DateTime end) ParsePeriod(string period) => period switch
{
    "last7days" => (DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
    "last30days" => (DateTime.UtcNow.AddDays(-30), DateTime.UtcNow),
    _ => (DateTime.Parse($"{period}-01"), DateTime.Parse($"{period}-01").AddMonths(1).AddSeconds(-1))
};
```

**Cost Estimation**:
```csharp
private decimal EstimateMonthlyAICost(CacheStatisticsDto stats)
{
    var tier2Cost = stats.Tier2Hits * _options.Tier2CostPerOperation;
    var tier3Cost = stats.Tier3Hits * _options.Tier3CostPerOperation;
    return tier2Cost + tier3Cost;
}
```

**Acceptance**:
- [ ] Supports YYYY-MM, last30days, last7days periods
- [ ] Calculates tier hit rates correctly (FR-013)
- [ ] Estimates monthly cost (FR-014)
- [ ] BelowTarget flag when Tier1 < 50%
- [ ] Optional groupBy operation type
- [ ] Performance: <2 seconds (SC-007)

**Dependencies**: Task 1.6, Task 5.1

---

### Task 5.3: Register CacheStatisticsService and AnalyticsOptions
**Priority**: P3 | **Story**: US5 | **Estimate**: XS

**Description**: Register cache statistics service and options in DI container.

**Files**:
- Modify: `backend/src/ExpenseFlow.Infrastructure/ServiceCollectionExtensions.cs`

**Implementation**:
```csharp
services.Configure<AnalyticsOptions>(configuration.GetSection(AnalyticsOptions.SectionName));
services.AddScoped<ICacheStatisticsService, CacheStatisticsService>();
```

**Acceptance**:
- [ ] Options bound to configuration
- [ ] Service registered as scoped

**Dependencies**: Task 5.2

---

### Task 5.4: Add Cache Statistics Endpoint
**Priority**: P3 | **Story**: US5 | **Estimate**: S

**Description**: Add GET endpoint for cache statistics.

**Files**:
- Modify: `backend/src/ExpenseFlow.Api/Controllers/AnalyticsController.cs`

**Implementation**:
Per contracts/output-api.yaml:
```csharp
[HttpGet("cache-stats")]
[Authorize(Policy = "AdminOnly")]
[ProducesResponseType(typeof(CacheStatisticsResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> GetCacheStatistics(
    [FromQuery] string period = "last30days",
    [FromQuery] string? groupBy = null,
    CancellationToken ct = default)
{
    if (groupBy != null && groupBy != "none" && groupBy != "operation")
        return BadRequest(new ProblemDetails { Title = "groupBy must be 'none' or 'operation'" });

    var result = await _cacheStatisticsService.GetStatisticsAsync(
        UserId,
        period,
        groupBy == "none" ? null : groupBy,
        ct);

    return Ok(result);
}
```

**Acceptance**:
- [ ] Default period is "last30days"
- [ ] Validates groupBy parameter
- [ ] Returns CacheStatisticsResponse on success
- [ ] Endpoint restricted to admin role via policy
- [ ] Returns 403 Forbidden for non-admin users

**Dependencies**: Task 5.3, Task 4.3

---

**Checkpoint**: User Story 5 (Cache Statistics) should be fully functional and testable independently

---

## Phase 6: Polish & Validation

### Task 6.1: Add Unit Tests for Export Services
**Priority**: P1 | **Story**: US1, US2, US3 | **Estimate**: L

**Description**: Create unit tests for Excel and PDF export services.

**Files**:
- Create: `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/ExcelExportServiceTests.cs`
- Create: `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/PdfGenerationServiceTests.cs`

**Test Cases - Excel**:
1. GenerateExcelAsync returns byte array for valid report
2. GenerateExcelAsync throws when report not found
3. Excel includes all expense lines in correct order
4. Excel header contains employee name and period

**Test Cases - PDF**:
1. GenerateReceiptPdfAsync returns ReceiptPdfDto with correct counts
2. GenerateReceiptPdfAsync throws when receipt limit exceeded
3. GenerateReceiptPdfAsync throws when justifications incomplete
4. Placeholder pages contain required information
5. Receipts ordered by LineOrder

**Acceptance**:
- [ ] All test cases pass
- [ ] Mocks used for blob storage, repositories
- [ ] Edge cases covered

**Dependencies**: Task 2.4, Task 3.5

---

### Task 6.2: Add Unit Tests for Analytics Services
**Priority**: P2 | **Story**: US4, US5 | **Estimate**: M

**Description**: Create unit tests for comparison and cache statistics services.

**Files**:
- Create: `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/MonthlyComparisonServiceTests.cs`
- Create: `backend/tests/ExpenseFlow.Infrastructure.Tests/Services/CacheStatisticsServiceTests.cs`

**Test Cases - Comparison**:
1. Identifies new vendors correctly
2. Identifies missing recurring vendors
3. Identifies significant changes (>50%)
4. Calculates summary totals correctly
5. Returns empty lists when no anomalies

**Test Cases - Cache Stats**:
1. Calculates tier hit rates correctly
2. Estimates monthly cost correctly
3. Sets BelowTarget flag when Tier1 < 50%
4. Groups by operation type when requested
5. Parses period formats correctly

**Acceptance**:
- [ ] All test cases pass
- [ ] In-memory DbContext for database tests
- [ ] Edge cases covered

**Dependencies**: Task 4.4, Task 5.4

---

### Task 6.3: Manual Validation per Quickstart
**Priority**: P1 | **Story**: All | **Estimate**: S

**Description**: Execute manual validation against quickstart.md checklist.

**Validation Steps**:
Per quickstart.md:
- [ ] Excel export completes in <5 seconds for 50 lines (SC-001)
- [ ] Excel matches AP template structure (SC-002)
- [ ] PDF generation completes in <30 seconds for 50 receipts (SC-003)
- [ ] PDF file size under 20MB average (SC-004)
- [ ] Placeholder pages contain all required fields (SC-005)
- [ ] MoM comparison identifies 95%+ anomalies (SC-006)
- [ ] Cache stats dashboard loads in <2 seconds (SC-007)
- [ ] Estimated AI cost displayed correctly (SC-008)

**Dependencies**: Task 6.1, Task 6.2

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 0)**: No dependencies - can start immediately
- **Foundational (Phase 1)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phases 2-5)**: All depend on Foundational phase completion
  - User stories can proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Excel Export - Can start after Phase 1
- **User Story 2 & 3 (P1)**: PDF Generation - Can start after Phase 1, independent of US1
- **User Story 4 (P2)**: MoM Comparison - Can start after Phase 1, independent of US1-3
- **User Story 5 (P3)**: Cache Statistics - Can start after Phase 1, shares controller with US4

### Parallel Opportunities

```
Phase 0: T0.1, T0.2, T0.3 can run in parallel
         │
         ▼
Phase 1: T1.1, T1.2, T1.3, T1.4, T1.5, T1.6 can run in parallel
         │
         ├──────────────────┬────────────────────┬──────────────────┐
         ▼                  ▼                    ▼                  ▼
Phase 2: US1 (Excel)   Phase 3: US2/3 (PDF)  Phase 4: US4 (MoM)  Phase 5: US5 (Cache)
         │                  │                    │                  │
         └──────────────────┴────────────────────┴──────────────────┘
                                     │
                                     ▼
                            Phase 6: Polish & Validation
```

---

## Size Legend

| Size | Description | Typical Duration |
|------|-------------|------------------|
| XS | Trivial change, single file | < 15 min |
| S | Small, well-defined task | 15-30 min |
| M | Medium complexity, multiple files | 30-60 min |
| L | Large, significant implementation | 1-2 hours |
| XL | Very large, consider splitting | 2+ hours |

---

## Notes

- All tasks use existing entities from Sprints 6-8 (no new database migrations)
- ClosedXML, PdfSharpCore, and ImageSharp are MIT licensed
- Template file must be uploaded to blob storage before Excel export works
- Receipt limit of 100 per PDF to prevent memory issues
- Cache statistics depend on TierUsageLog from Sprint 6
