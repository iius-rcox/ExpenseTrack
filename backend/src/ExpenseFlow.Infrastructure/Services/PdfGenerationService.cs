using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Configuration;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SixLabors.ImageSharp;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for generating consolidated receipt PDFs with missing receipt placeholders.
/// </summary>
public class PdfGenerationService : IPdfGenerationService
{
    private readonly IExpenseReportRepository _reportRepository;
    private readonly IBlobStorageService _blobService;
    private readonly ExportOptions _options;
    private readonly ILogger<PdfGenerationService> _logger;

    // PDF page dimensions (Letter size in points)
    private const double PageWidth = 612;
    private const double PageHeight = 792;
    private const double Margin = 36; // 0.5 inch margin

    // Font family name - use a cross-platform compatible font
    // Helvetica is a built-in PDF base font that doesn't require installation
    private const string FontFamily = "Helvetica";

    public PdfGenerationService(
        IExpenseReportRepository reportRepository,
        IBlobStorageService blobService,
        IOptions<ExportOptions> options,
        ILogger<PdfGenerationService> logger)
    {
        _reportRepository = reportRepository;
        _blobService = blobService;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ReceiptPdfDto> GenerateReceiptPdfAsync(Guid reportId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating receipt PDF for report {ReportId}", reportId);

        var report = await _reportRepository.GetByIdWithLinesAsync(reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found");

        using var document = new PdfDocument();
        document.Info.Title = $"Expense Report Receipts - {report.Period}";
        document.Info.Author = report.User?.DisplayName ?? "ExpenseFlow";

        var orderedLines = report.Lines
            .OrderBy(l => l.LineOrder)
            .Take(_options.MaxReceiptsPerPdf)
            .ToList();

        int pageCount = 0;
        int placeholderCount = 0;

        foreach (var line in orderedLines)
        {
            ct.ThrowIfCancellationRequested();

            if (line.HasReceipt && line.Receipt != null)
            {
                // Add receipt image page(s)
                var pagesAdded = await AddReceiptPageAsync(document, line, ct);
                pageCount += pagesAdded;
            }
            else
            {
                // Add placeholder page for missing receipt
                AddPlaceholderPage(document, line, report);
                pageCount++;
                placeholderCount++;
            }
        }

        // If no pages were added, add an informational page
        if (pageCount == 0)
        {
            AddEmptyReportPage(document, report);
            pageCount = 1;
        }

        await using var outputStream = new MemoryStream();
        document.Save(outputStream, false);

        _logger.LogInformation(
            "Generated receipt PDF for report {ReportId}: {PageCount} pages, {PlaceholderCount} placeholders",
            reportId, pageCount, placeholderCount);

        return new ReceiptPdfDto
        {
            FileName = $"{report.Period}-receipts.pdf",
            ContentType = "application/pdf",
            FileContents = outputStream.ToArray(),
            PageCount = pageCount,
            PlaceholderCount = placeholderCount
        };
    }

    private async Task<int> AddReceiptPageAsync(
        PdfDocument document,
        ExpenseLine line,
        CancellationToken ct)
    {
        if (line.Receipt == null)
            return 0;

        try
        {
            using var imageStream = await _blobService.DownloadAsync(line.Receipt.BlobUrl);
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;

            // Detect image format and convert if necessary
            var imageBytes = memoryStream.ToArray();

            // Handle HEIC/HEIF using ImageSharp
            if (IsHeicFormat(line.Receipt.ContentType, line.Receipt.OriginalFilename))
            {
                imageBytes = await ConvertHeicToJpegAsync(imageBytes, ct);
            }

            // For PDF source files, we'd need special handling - for now, treat as image
            if (line.Receipt.ContentType == "application/pdf")
            {
                // PDF embedding would require different handling
                // For MVP, create a placeholder noting it's a PDF receipt
                AddPdfReceiptPlaceholder(document, line);
                return 1;
            }

            // Add image to PDF
            var page = document.AddPage();
            page.Width = PageWidth;
            page.Height = PageHeight;

            using var gfx = XGraphics.FromPdfPage(page);

            // Load image using PdfSharpCore
            using var imageMemStream = new MemoryStream(imageBytes);
            var xImage = XImage.FromStream(() => new MemoryStream(imageBytes));

            // Calculate scaling to fit page with margins
            var availableWidth = PageWidth - (2 * Margin);
            var availableHeight = PageHeight - (2 * Margin) - 60; // Leave room for header

            var scale = Math.Min(
                availableWidth / xImage.PixelWidth,
                availableHeight / xImage.PixelHeight);

            var scaledWidth = xImage.PixelWidth * scale;
            var scaledHeight = xImage.PixelHeight * scale;

            // Center the image
            var x = (PageWidth - scaledWidth) / 2;
            var y = Margin + 50; // After header

            // Draw header
            DrawReceiptHeader(gfx, line);

            // Draw image
            gfx.DrawImage(xImage, x, y, scaledWidth, scaledHeight);

            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to process receipt image for line {LineId}, adding error placeholder",
                line.Id);

            AddReceiptErrorPage(document, line, ex.Message);
            return 1;
        }
    }

    private void DrawReceiptHeader(XGraphics gfx, ExpenseLine line)
    {
        var fontBold = new XFont(FontFamily, 12, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 10, XFontStyle.Regular);

        var y = Margin;

        gfx.DrawString(
            $"Receipt - {line.ExpenseDate:MM/dd/yyyy}",
            fontBold,
            XBrushes.Black,
            new XRect(Margin, y, PageWidth - (2 * Margin), 20),
            XStringFormats.TopLeft);

        y += 18;

        var description = !string.IsNullOrEmpty(line.VendorName)
            ? $"{line.VendorName} - {line.Amount:C}"
            : $"{line.NormalizedDescription} - {line.Amount:C}";

        gfx.DrawString(
            description,
            fontRegular,
            XBrushes.DarkGray,
            new XRect(Margin, y, PageWidth - (2 * Margin), 16),
            XStringFormats.TopLeft);
    }

    private void AddPlaceholderPage(PdfDocument document, ExpenseLine line, ExpenseReport report)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 18, XFontStyle.Bold);
        var fontHeader = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);
        var fontSmall = new XFont(FontFamily, 10, XFontStyle.Regular);

        var y = Margin + 40;
        var lineHeight = 24;

        // Title
        gfx.DrawString(
            "MISSING RECEIPT DECLARATION",
            fontTitle,
            XBrushes.DarkRed,
            new XRect(0, y, PageWidth, 30),
            XStringFormats.TopCenter);

        y += 60;

        // Draw border box
        var boxX = Margin + 20;
        var boxWidth = PageWidth - (2 * (Margin + 20));
        var boxHeight = 300;

        gfx.DrawRectangle(
            new XPen(XColors.Gray, 1),
            boxX, y, boxWidth, boxHeight);

        y += 20;
        var textX = boxX + 15;
        var textWidth = boxWidth - 30;

        // Expense details
        DrawLabelValue(gfx, fontHeader, fontRegular, "Employee:",
            report.User?.DisplayName ?? "Unknown", textX, y, textWidth);
        y += lineHeight + 10;

        DrawLabelValue(gfx, fontHeader, fontRegular, "Report Period:",
            report.Period, textX, y, textWidth);
        y += lineHeight + 10;

        DrawLabelValue(gfx, fontHeader, fontRegular, "Expense Date:",
            line.ExpenseDate.ToString("MMMM d, yyyy"), textX, y, textWidth);
        y += lineHeight + 10;

        var vendorName = !string.IsNullOrEmpty(line.VendorName) ? line.VendorName : "Unknown";
        DrawLabelValue(gfx, fontHeader, fontRegular, "Vendor:",
            vendorName, textX, y, textWidth);
        y += lineHeight + 10;

        DrawLabelValue(gfx, fontHeader, fontRegular, "Amount:",
            line.Amount.ToString("C"), textX, y, textWidth);
        y += lineHeight + 10;

        DrawLabelValue(gfx, fontHeader, fontRegular, "Description:",
            line.NormalizedDescription, textX, y, textWidth);
        y += lineHeight + 20;

        // Justification section
        var justification = GetJustificationText(line.MissingReceiptJustification);
        DrawLabelValue(gfx, fontHeader, fontRegular, "Reason for Missing Receipt:",
            justification, textX, y, textWidth);
        y += lineHeight + 10;

        if (!string.IsNullOrEmpty(line.JustificationNote))
        {
            DrawLabelValue(gfx, fontHeader, fontRegular, "Additional Notes:",
                line.JustificationNote, textX, y, textWidth);
        }

        // Footer with reference
        y = PageHeight - Margin - 60;

        gfx.DrawString(
            $"Report ID: {report.Id}",
            fontSmall,
            XBrushes.Gray,
            new XRect(0, y, PageWidth, 16),
            XStringFormats.TopCenter);

        y += 16;

        gfx.DrawString(
            $"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC",
            fontSmall,
            XBrushes.Gray,
            new XRect(0, y, PageWidth, 16),
            XStringFormats.TopCenter);
    }

    private void DrawLabelValue(
        XGraphics gfx,
        XFont labelFont,
        XFont valueFont,
        string label,
        string value,
        double x,
        double y,
        double width)
    {
        gfx.DrawString(
            label,
            labelFont,
            XBrushes.Black,
            new XRect(x, y, 150, 20),
            XStringFormats.TopLeft);

        gfx.DrawString(
            value,
            valueFont,
            XBrushes.DarkGray,
            new XRect(x + 160, y, width - 160, 20),
            XStringFormats.TopLeft);
    }

    private void AddPdfReceiptPlaceholder(PdfDocument document, ExpenseLine line)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);

        var y = PageHeight / 2 - 60;

        gfx.DrawString(
            "PDF Receipt Attached",
            fontTitle,
            XBrushes.Black,
            new XRect(0, y, PageWidth, 20),
            XStringFormats.TopCenter);

        y += 30;

        gfx.DrawString(
            $"Date: {line.ExpenseDate:MM/dd/yyyy}",
            fontRegular,
            XBrushes.DarkGray,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);

        y += 24;

        gfx.DrawString(
            $"Amount: {line.Amount:C}",
            fontRegular,
            XBrushes.DarkGray,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);

        y += 24;

        gfx.DrawString(
            "(Original PDF receipt available separately)",
            fontRegular,
            XBrushes.Gray,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);
    }

    private void AddReceiptErrorPage(PdfDocument document, ExpenseLine line, string errorMessage)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);
        var fontSmall = new XFont(FontFamily, 10, XFontStyle.Regular);

        var y = PageHeight / 2 - 80;

        gfx.DrawString(
            "Receipt Image Unavailable",
            fontTitle,
            XBrushes.DarkRed,
            new XRect(0, y, PageWidth, 20),
            XStringFormats.TopCenter);

        y += 40;

        gfx.DrawString(
            $"Date: {line.ExpenseDate:MM/dd/yyyy}",
            fontRegular,
            XBrushes.Black,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);

        y += 24;

        gfx.DrawString(
            $"Amount: {line.Amount:C}",
            fontRegular,
            XBrushes.Black,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);

        y += 40;

        gfx.DrawString(
            "The receipt image could not be processed.",
            fontSmall,
            XBrushes.Gray,
            new XRect(0, y, PageWidth, 16),
            XStringFormats.TopCenter);

        y += 20;

        // Truncate error message if too long
        var truncatedError = errorMessage.Length > 80
            ? errorMessage[..77] + "..."
            : errorMessage;

        gfx.DrawString(
            $"Error: {truncatedError}",
            fontSmall,
            XBrushes.Gray,
            new XRect(Margin, y, PageWidth - (2 * Margin), 16),
            XStringFormats.TopCenter);
    }

    private void AddEmptyReportPage(PdfDocument document, ExpenseReport report)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 16, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);

        var y = PageHeight / 2 - 40;

        gfx.DrawString(
            $"Expense Report - {report.Period}",
            fontTitle,
            XBrushes.Black,
            new XRect(0, y, PageWidth, 24),
            XStringFormats.TopCenter);

        y += 40;

        gfx.DrawString(
            "No receipt images to display.",
            fontRegular,
            XBrushes.Gray,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);
    }

    private static string GetJustificationText(MissingReceiptJustification? justification)
    {
        return justification switch
        {
            MissingReceiptJustification.Lost => "Receipt was lost",
            MissingReceiptJustification.NotProvided => "Vendor did not provide receipt",
            MissingReceiptJustification.DigitalSubscription => "Digital subscription - no receipt available",
            MissingReceiptJustification.UnderThreshold => "Amount below receipt threshold",
            MissingReceiptJustification.Other => "Other (see notes)",
            MissingReceiptJustification.None => "Not specified",
            _ => "Not specified"
        };
    }

    private static bool IsHeicFormat(string contentType, string filename)
    {
        if (contentType.Contains("heic", StringComparison.OrdinalIgnoreCase) ||
            contentType.Contains("heif", StringComparison.OrdinalIgnoreCase))
            return true;

        var extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension is ".heic" or ".heif";
    }

    private static async Task<byte[]> ConvertHeicToJpegAsync(byte[] heicBytes, CancellationToken ct)
    {
        using var image = await Image.LoadAsync(new MemoryStream(heicBytes), ct);
        using var outputStream = new MemoryStream();
        await image.SaveAsJpegAsync(outputStream, ct);
        return outputStream.ToArray();
    }

    /// <inheritdoc />
    public Task<byte[]> GenerateSummaryPdfAsync(
        ExportPreviewRequest request,
        string employeeName,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Generating summary PDF from preview for period {Period} with {LineCount} lines",
            request.Period, request.Lines.Count);

        var document = new PdfDocument();
        document.Info.Title = $"Expense Report - {request.Period}";
        document.Info.Author = employeeName;

        var page = document.AddPage();
        var gfx = XGraphics.FromPdfPage(page);
        var fontBold = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 10, XFontStyle.Regular);
        var fontSmall = new XFont(FontFamily, 8, XFontStyle.Regular);

        double y = Margin;

        // Header
        gfx.DrawString(
            $"EXPENSE REPORT - {request.Period}",
            fontBold,
            XBrushes.Black,
            new XRect(Margin, y, PageWidth - (2 * Margin), 20),
            XStringFormats.TopCenter);

        y += 30;

        gfx.DrawString(
            $"Employee: {employeeName}",
            fontRegular,
            XBrushes.Black,
            new XRect(Margin, y, PageWidth - (2 * Margin), 15),
            XStringFormats.TopLeft);

        y += 20;

        gfx.DrawString(
            $"Total Expenses: {request.Lines.Count} | Amount: {request.Lines.Sum(l => l.Amount):C}",
            fontRegular,
            XBrushes.Black,
            new XRect(Margin, y, PageWidth - (2 * Margin), 15),
            XStringFormats.TopLeft);

        y += 30;

        // Table header
        var colWidths = new[] { 70.0, 120.0, 70.0, 60.0, 140.0, 60.0 }; // Date, Vendor, GL, Dept, Desc, Amount
        var colX = Margin;

        gfx.DrawString("Date", fontBold, XBrushes.Black, new XRect(colX, y, colWidths[0], 12), XStringFormats.TopLeft);
        colX += colWidths[0];
        gfx.DrawString("Vendor", fontBold, XBrushes.Black, new XRect(colX, y, colWidths[1], 12), XStringFormats.TopLeft);
        colX += colWidths[1];
        gfx.DrawString("GL Code", fontBold, XBrushes.Black, new XRect(colX, y, colWidths[2], 12), XStringFormats.TopLeft);
        colX += colWidths[2];
        gfx.DrawString("Dept", fontBold, XBrushes.Black, new XRect(colX, y, colWidths[3], 12), XStringFormats.TopLeft);
        colX += colWidths[3];
        gfx.DrawString("Description", fontBold, XBrushes.Black, new XRect(colX, y, colWidths[4], 12), XStringFormats.TopLeft);
        colX += colWidths[4];
        gfx.DrawString("Amount", fontBold, XBrushes.Black, new XRect(colX, y, colWidths[5], 12), XStringFormats.TopRight);

        y += 15;
        gfx.DrawLine(XPens.Black, Margin, y, PageWidth - Margin, y);
        y += 5;

        // Table rows
        foreach (var line in request.Lines.Take(30)) // First 30 lines to fit on page
        {
            colX = Margin;

            gfx.DrawString(line.ExpenseDate.ToString("MM/dd/yy"), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[0], 10), XStringFormats.TopLeft);
            colX += colWidths[0];
            gfx.DrawString(TruncateString(line.VendorName, 18), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[1], 10), XStringFormats.TopLeft);
            colX += colWidths[1];
            gfx.DrawString(line.GlCode, fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[2], 10), XStringFormats.TopLeft);
            colX += colWidths[2];
            gfx.DrawString(line.DepartmentCode, fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[3], 10), XStringFormats.TopLeft);
            colX += colWidths[3];
            gfx.DrawString(TruncateString(line.Description, 22), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[4], 10), XStringFormats.TopLeft);
            colX += colWidths[4];
            gfx.DrawString(line.Amount.ToString("C"), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[5], 10), XStringFormats.TopRight);

            y += 12;

            if (y > PageHeight - Margin - 20)
            {
                // Add new page if needed (simplified for MVP)
                break;
            }
        }

        // Save to memory stream
        using var stream = new MemoryStream();
        document.Save(stream, false);
        return Task.FromResult(stream.ToArray());
    }

    private static string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value.Substring(0, maxLength - 3) + "...";
    }
}
