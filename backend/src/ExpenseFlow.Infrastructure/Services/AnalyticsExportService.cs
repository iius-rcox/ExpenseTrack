using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for exporting analytics data to CSV and Excel formats.
/// Supports multiple sections: trends, categories, vendors, and transactions.
/// </summary>
public class AnalyticsExportService : IAnalyticsExportService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsExportService> _logger;

    /// <summary>
    /// Maximum date range allowed for export (5 years as per spec).
    /// </summary>
    private static readonly TimeSpan MaxDateRange = TimeSpan.FromDays(365 * 5);

    public AnalyticsExportService(
        ExpenseFlowDbContext dbContext,
        IAnalyticsService analyticsService,
        ILogger<AnalyticsExportService> logger)
    {
        _dbContext = dbContext;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(byte[] FileBytes, string ContentType, string FileName)> ExportAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        string format,
        IReadOnlyList<string> sections,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Starting analytics export for user {UserId} from {StartDate} to {EndDate}, format: {Format}, sections: {Sections}",
            userId, startDate, endDate, format, string.Join(",", sections));

        // Validate date range (5 years max)
        var dateRange = endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue);
        if (dateRange > MaxDateRange)
        {
            throw new InvalidOperationException(
                $"Date range exceeds maximum of 5 years. Requested range: {dateRange.Days} days.");
        }

        if (startDate > endDate)
        {
            throw new InvalidOperationException("Start date must be before or equal to end date.");
        }

        // Gather data for all requested sections
        var exportData = await GatherExportDataAsync(userId, startDate, endDate, sections, ct);

        // Generate file based on format
        var normalizedFormat = format.ToLowerInvariant();
        return normalizedFormat switch
        {
            "xlsx" => GenerateExcelExport(exportData, startDate, endDate),
            _ => GenerateCsvExport(exportData, startDate, endDate) // Default to CSV
        };
    }

    private async Task<AnalyticsExportData> GatherExportDataAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyList<string> sections,
        CancellationToken ct)
    {
        var data = new AnalyticsExportData
        {
            StartDate = startDate,
            EndDate = endDate
        };

        foreach (var section in sections)
        {
            switch (section.ToLowerInvariant())
            {
                case "trends":
                    data.Trends = await _analyticsService.GetSpendingTrendAsync(
                        userId, startDate, endDate, "month", ct);
                    break;

                case "categories":
                    data.Categories = await _analyticsService.GetSpendingByCategoryAsync(
                        userId, startDate, endDate, ct);
                    break;

                case "vendors":
                    data.Vendors = await _analyticsService.GetSpendingByVendorAsync(
                        userId, startDate, endDate, ct);
                    break;

                case "transactions":
                    data.Transactions = await GetTransactionsForExportAsync(
                        userId, startDate, endDate, ct);
                    break;
            }
        }

        return data;
    }

    private async Task<List<TransactionExportDto>> GetTransactionsForExportAsync(
        Guid userId,
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken ct)
    {
        var transactions = await _dbContext.Transactions
            .Where(t => t.UserId == userId &&
                       t.TransactionDate >= startDate &&
                       t.TransactionDate <= endDate)
            .OrderBy(t => t.TransactionDate)
            .ThenBy(t => t.Description)
            .Select(t => new TransactionExportDto
            {
                Date = t.TransactionDate.ToString("yyyy-MM-dd"),
                Description = t.Description,
                OriginalDescription = t.OriginalDescription,
                Amount = t.Amount,
                Category = _analyticsService.DeriveCategory(t.Description),
                HasReceipt = t.MatchedReceiptId != null
            })
            .ToListAsync(ct);

        _logger.LogDebug(
            "Retrieved {Count} transactions for export for user {UserId}",
            transactions.Count, userId);

        return transactions;
    }

    private (byte[] FileBytes, string ContentType, string FileName) GenerateCsvExport(
        AnalyticsExportData data,
        DateOnly startDate,
        DateOnly endDate)
    {
        using var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        };

        // For CSV, we concatenate all sections with headers
        // The most requested section (transactions if present, otherwise first available) is primary
        if (data.Transactions?.Count > 0)
        {
            writer.WriteLine("# Transactions Export");
            writer.WriteLine($"# Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");
            writer.WriteLine();

            using var csv = new CsvWriter(writer, config, leaveOpen: true);
            csv.WriteRecords(data.Transactions);
            writer.WriteLine();
        }

        if (data.Trends?.Count > 0)
        {
            writer.WriteLine("# Spending Trends (Monthly)");
            writer.WriteLine();

            using var csv = new CsvWriter(writer, config, leaveOpen: true);
            csv.WriteRecords(data.Trends);
            writer.WriteLine();
        }

        if (data.Categories?.Count > 0)
        {
            writer.WriteLine("# Spending by Category");
            writer.WriteLine();

            using var csv = new CsvWriter(writer, config, leaveOpen: true);
            csv.WriteRecords(data.Categories);
            writer.WriteLine();
        }

        if (data.Vendors?.Count > 0)
        {
            writer.WriteLine("# Spending by Vendor");
            writer.WriteLine();

            using var csv = new CsvWriter(writer, config, leaveOpen: true);
            csv.WriteRecords(data.Vendors);
        }

        writer.Flush();
        var bytes = memoryStream.ToArray();

        var fileName = $"Analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv";

        _logger.LogInformation(
            "Generated CSV export: {FileName}, size: {Size} bytes",
            fileName, bytes.Length);

        return (bytes, "text/csv", fileName);
    }

    private (byte[] FileBytes, string ContentType, string FileName) GenerateExcelExport(
        AnalyticsExportData data,
        DateOnly startDate,
        DateOnly endDate)
    {
        using var workbook = new XLWorkbook();

        // Add sheets for each section with data
        if (data.Trends?.Count > 0)
        {
            AddTrendsSheet(workbook, data.Trends);
        }

        if (data.Categories?.Count > 0)
        {
            AddCategoriesSheet(workbook, data.Categories);
        }

        if (data.Vendors?.Count > 0)
        {
            AddVendorsSheet(workbook, data.Vendors);
        }

        if (data.Transactions?.Count > 0)
        {
            AddTransactionsSheet(workbook, data.Transactions);
        }

        // Add summary sheet if we have any data
        if (workbook.Worksheets.Count > 0)
        {
            AddSummarySheet(workbook, data, startDate, endDate);
        }
        else
        {
            // If no data, add an empty summary sheet
            var sheet = workbook.Worksheets.Add("Summary");
            sheet.Cell("A1").Value = "No Data";
            sheet.Cell("A2").Value = $"Date Range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";
        }

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        var bytes = memoryStream.ToArray();

        var fileName = $"Analytics_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";

        _logger.LogInformation(
            "Generated Excel export: {FileName}, size: {Size} bytes, sheets: {SheetCount}",
            fileName, bytes.Length, workbook.Worksheets.Count);

        return (bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private static void AddTrendsSheet(XLWorkbook workbook, List<SpendingTrendItemDto> trends)
    {
        var sheet = workbook.Worksheets.Add("Trends");

        // Headers
        sheet.Cell("A1").Value = "Period";
        sheet.Cell("B1").Value = "Amount";
        sheet.Cell("C1").Value = "Transaction Count";

        // Style headers
        var headerRange = sheet.Range("A1:C1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data
        for (int i = 0; i < trends.Count; i++)
        {
            var row = i + 2;
            sheet.Cell($"A{row}").Value = trends[i].Date;
            sheet.Cell($"B{row}").Value = trends[i].Amount;
            sheet.Cell($"C{row}").Value = trends[i].TransactionCount;
        }

        // Format amount column as currency
        sheet.Column("B").Style.NumberFormat.Format = "$#,##0.00";

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
    }

    private static void AddCategoriesSheet(XLWorkbook workbook, List<SpendingByCategoryItemDto> categories)
    {
        var sheet = workbook.Worksheets.Add("Categories");

        // Headers
        sheet.Cell("A1").Value = "Category";
        sheet.Cell("B1").Value = "Amount";
        sheet.Cell("C1").Value = "Transaction Count";
        sheet.Cell("D1").Value = "% of Total";

        // Style headers
        var headerRange = sheet.Range("A1:D1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data
        for (int i = 0; i < categories.Count; i++)
        {
            var row = i + 2;
            sheet.Cell($"A{row}").Value = categories[i].Category;
            sheet.Cell($"B{row}").Value = categories[i].Amount;
            sheet.Cell($"C{row}").Value = categories[i].TransactionCount;
            sheet.Cell($"D{row}").Value = categories[i].PercentageOfTotal / 100; // Convert to decimal for % format
        }

        // Format columns
        sheet.Column("B").Style.NumberFormat.Format = "$#,##0.00";
        sheet.Column("D").Style.NumberFormat.Format = "0.00%";

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
    }

    private static void AddVendorsSheet(XLWorkbook workbook, List<SpendingByVendorItemDto> vendors)
    {
        var sheet = workbook.Worksheets.Add("Vendors");

        // Headers
        sheet.Cell("A1").Value = "Vendor";
        sheet.Cell("B1").Value = "Amount";
        sheet.Cell("C1").Value = "Transaction Count";
        sheet.Cell("D1").Value = "% of Total";

        // Style headers
        var headerRange = sheet.Range("A1:D1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data
        for (int i = 0; i < vendors.Count; i++)
        {
            var row = i + 2;
            sheet.Cell($"A{row}").Value = vendors[i].VendorName;
            sheet.Cell($"B{row}").Value = vendors[i].Amount;
            sheet.Cell($"C{row}").Value = vendors[i].TransactionCount;
            sheet.Cell($"D{row}").Value = vendors[i].PercentageOfTotal / 100;
        }

        // Format columns
        sheet.Column("B").Style.NumberFormat.Format = "$#,##0.00";
        sheet.Column("D").Style.NumberFormat.Format = "0.00%";

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
    }

    private static void AddTransactionsSheet(XLWorkbook workbook, List<TransactionExportDto> transactions)
    {
        var sheet = workbook.Worksheets.Add("Transactions");

        // Headers
        sheet.Cell("A1").Value = "Date";
        sheet.Cell("B1").Value = "Description";
        sheet.Cell("C1").Value = "Original Description";
        sheet.Cell("D1").Value = "Amount";
        sheet.Cell("E1").Value = "Category";
        sheet.Cell("F1").Value = "Has Receipt";

        // Style headers
        var headerRange = sheet.Range("A1:F1");
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data
        for (int i = 0; i < transactions.Count; i++)
        {
            var row = i + 2;
            sheet.Cell($"A{row}").Value = transactions[i].Date;
            sheet.Cell($"B{row}").Value = transactions[i].Description;
            sheet.Cell($"C{row}").Value = transactions[i].OriginalDescription;
            sheet.Cell($"D{row}").Value = transactions[i].Amount;
            sheet.Cell($"E{row}").Value = transactions[i].Category;
            sheet.Cell($"F{row}").Value = transactions[i].HasReceipt ? "Yes" : "No";
        }

        // Format columns
        sheet.Column("D").Style.NumberFormat.Format = "$#,##0.00";

        // Auto-fit columns (limit width for description columns)
        sheet.Columns().AdjustToContents();
        if (sheet.Column("B").Width > 50) sheet.Column("B").Width = 50;
        if (sheet.Column("C").Width > 50) sheet.Column("C").Width = 50;
    }

    private static void AddSummarySheet(
        XLWorkbook workbook,
        AnalyticsExportData data,
        DateOnly startDate,
        DateOnly endDate)
    {
        var sheet = workbook.Worksheets.Add("Summary");

        // Move summary to first position
        sheet.Position = 1;

        // Title
        sheet.Cell("A1").Value = "Analytics Export Summary";
        sheet.Cell("A1").Style.Font.Bold = true;
        sheet.Cell("A1").Style.Font.FontSize = 14;

        sheet.Cell("A3").Value = "Date Range:";
        sheet.Cell("B3").Value = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";

        sheet.Cell("A4").Value = "Generated:";
        sheet.Cell("B4").Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");

        // Section summaries
        int row = 6;

        sheet.Cell($"A{row}").Value = "Included Sections";
        sheet.Cell($"A{row}").Style.Font.Bold = true;
        row++;

        if (data.Trends?.Count > 0)
        {
            sheet.Cell($"A{row}").Value = "• Trends";
            sheet.Cell($"B{row}").Value = $"{data.Trends.Count} periods";
            row++;
        }

        if (data.Categories?.Count > 0)
        {
            sheet.Cell($"A{row}").Value = "• Categories";
            sheet.Cell($"B{row}").Value = $"{data.Categories.Count} categories";
            sheet.Cell($"C{row}").Value = data.Categories.Sum(c => c.Amount);
            sheet.Cell($"C{row}").Style.NumberFormat.Format = "$#,##0.00";
            row++;
        }

        if (data.Vendors?.Count > 0)
        {
            sheet.Cell($"A{row}").Value = "• Vendors";
            sheet.Cell($"B{row}").Value = $"{data.Vendors.Count} vendors";
            row++;
        }

        if (data.Transactions?.Count > 0)
        {
            sheet.Cell($"A{row}").Value = "• Transactions";
            sheet.Cell($"B{row}").Value = $"{data.Transactions.Count} transactions";
            sheet.Cell($"C{row}").Value = data.Transactions.Sum(t => t.Amount);
            sheet.Cell($"C{row}").Style.NumberFormat.Format = "$#,##0.00";
        }

        // Auto-fit columns
        sheet.Columns().AdjustToContents();
    }

    /// <summary>
    /// Internal DTO for transaction export data.
    /// </summary>
    private class TransactionExportDto
    {
        public string Date { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string OriginalDescription { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool HasReceipt { get; set; }
    }

    /// <summary>
    /// Container for all export data sections.
    /// </summary>
    private class AnalyticsExportData
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public List<SpendingTrendItemDto>? Trends { get; set; }
        public List<SpendingByCategoryItemDto>? Categories { get; set; }
        public List<SpendingByVendorItemDto>? Vendors { get; set; }
        public List<TransactionExportDto>? Transactions { get; set; }
    }
}
