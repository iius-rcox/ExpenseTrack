using ClosedXML.Excel;
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

        // Download template from blob storage
        Stream templateStream;
        try
        {
            templateStream = await _blobService.DownloadAsync(
                _options.TemplateBlobContainer,
                _options.TemplateFileName,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download Excel template from {Container}/{FileName}",
                _options.TemplateBlobContainer, _options.TemplateFileName);
            throw new InvalidOperationException(
                $"Excel template not found: {_options.TemplateFileName}. Please upload template to blob storage.", ex);
        }

        using var workbook = new XLWorkbook(templateStream);
        var worksheet = workbook.Worksheet(1);

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
}
