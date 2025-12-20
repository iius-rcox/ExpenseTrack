using ClosedXML.Excel;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for generating Excel expense reports matching the AP department template format.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    private readonly IExpenseReportRepository _reportRepository;
    private readonly IBlobStorageService _blobService;
    private readonly ExportOptions _options;
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(
        IExpenseReportRepository reportRepository,
        IBlobStorageService blobService,
        IOptions<ExportOptions> options,
        ILogger<ExcelExportService> logger)
    {
        _reportRepository = reportRepository;
        _blobService = blobService;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]> GenerateExcelAsync(Guid reportId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating Excel export for report {ReportId}", reportId);

        // Load report with expense lines
        var report = await _reportRepository.GetByIdWithLinesAsync(reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found");

        // Try to download template from blob storage, or create a default one
        XLWorkbook workbook;
        IXLWorksheet worksheet;

        try
        {
            var templateStream = await _blobService.DownloadAsync(
                _options.TemplateBlobContainer,
                _options.TemplateFileName,
                ct);
            workbook = new XLWorkbook(templateStream);
            worksheet = workbook.Worksheet(1);
            _logger.LogDebug("Using custom Excel template from blob storage");
        }
        catch (Exception ex)
        {
            // BUG-007 fix: Fall back to generating a default template if custom template is not found
            _logger.LogWarning(ex, "Custom Excel template not found at {Container}/{FileName}, using default template",
                _options.TemplateBlobContainer, _options.TemplateFileName);
            (workbook, worksheet) = CreateDefaultTemplate(report);
        }

        // Fill header section
        worksheet.Cell("B2").Value = report.User?.DisplayName ?? "Unknown";
        worksheet.Cell("B3").Value = report.Period;

        // Fill expense lines starting at row 7
        const int startRow = 7;
        var orderedLines = report.Lines.OrderBy(l => l.LineOrder).ToList();

        for (int i = 0; i < orderedLines.Count; i++)
        {
            var line = orderedLines[i];
            var row = startRow + i;

            // Column A: Expense Date
            worksheet.Cell(row, 1).Value = line.ExpenseDate.ToString(_options.DateFormat);

            // Column B: GL Acct/Job
            worksheet.Cell(row, 2).Value = line.GLCode ?? "";

            // Column C: Dept/Phas
            worksheet.Cell(row, 3).Value = line.DepartmentCode ?? "";

            // Column D: Expense Description
            worksheet.Cell(row, 4).Value = line.NormalizedDescription;

            // Column E: Units/Mileage (default to 1)
            worksheet.Cell(row, 5).Value = 1;

            // Column F: Rate/Amount
            worksheet.Cell(row, 6).Value = line.Amount;

            // Column G: Expense Total - formula should already be in template
            // =IF(ISBLANK(E{row}),"",E{row}*F{row})
            // We don't touch this column to preserve template formulas
        }

        // Serialize workbook to byte array
        await using var outputStream = new MemoryStream();
        workbook.SaveAs(outputStream);

        _logger.LogInformation(
            "Generated Excel export for report {ReportId} with {LineCount} lines",
            reportId, orderedLines.Count);

        return outputStream.ToArray();
    }

    /// <summary>
    /// Creates a default Excel template when custom template is not available.
    /// This ensures Excel export works even without deploying a template to blob storage.
    /// </summary>
    private static (XLWorkbook workbook, IXLWorksheet worksheet) CreateDefaultTemplate(ExpenseReport report)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Expense Report");

        // Set column widths
        worksheet.Column(1).Width = 12; // Date
        worksheet.Column(2).Width = 15; // GL Acct/Job
        worksheet.Column(3).Width = 12; // Dept/Phase
        worksheet.Column(4).Width = 40; // Description
        worksheet.Column(5).Width = 10; // Units
        worksheet.Column(6).Width = 12; // Rate/Amount
        worksheet.Column(7).Width = 14; // Total

        // Header section
        worksheet.Cell("A1").Value = "EXPENSE REPORT";
        worksheet.Cell("A1").Style.Font.Bold = true;
        worksheet.Cell("A1").Style.Font.FontSize = 14;

        worksheet.Cell("A2").Value = "Employee:";
        worksheet.Cell("A2").Style.Font.Bold = true;
        // B2 will be filled with employee name

        worksheet.Cell("A3").Value = "Period:";
        worksheet.Cell("A3").Style.Font.Bold = true;
        // B3 will be filled with period

        worksheet.Cell("A4").Value = "Report ID:";
        worksheet.Cell("A4").Style.Font.Bold = true;
        worksheet.Cell("B4").Value = report.Id.ToString();

        // Column headers (row 6)
        var headerRow = 6;
        worksheet.Cell(headerRow, 1).Value = "Date";
        worksheet.Cell(headerRow, 2).Value = "GL Acct/Job";
        worksheet.Cell(headerRow, 3).Value = "Dept/Phas";
        worksheet.Cell(headerRow, 4).Value = "Expense Description";
        worksheet.Cell(headerRow, 5).Value = "Units";
        worksheet.Cell(headerRow, 6).Value = "Rate/Amount";
        worksheet.Cell(headerRow, 7).Value = "Expense Total";

        // Style header row
        var headerRange = worksheet.Range(headerRow, 1, headerRow, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
        headerRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

        return (workbook, worksheet);
    }
}
