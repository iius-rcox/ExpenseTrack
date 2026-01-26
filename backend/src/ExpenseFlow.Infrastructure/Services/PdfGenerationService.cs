using System.Globalization;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Configuration;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static MigraDocCore.DocumentObjectModel.MigraDoc.DocumentObjectModel.Shapes.ImageSource;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

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

    // Currency formatting - always use US culture for consistent $ display
    // This ensures "$" instead of culture-dependent symbols like "¤" in containers
    private static readonly CultureInfo UsCulture = CultureInfo.CreateSpecificCulture("en-US");

    // Security: Maximum image dimensions to prevent DoS via oversized images
    private const int MaxImageWidth = 8000;
    private const int MaxImageHeight = 8000;
    private const long MaxImageBytes = 50 * 1024 * 1024; // 50MB max

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

        // Filter out split child lines - only include parent lines
        // (split children inherit receipt from parent, so we only need one copy)
        var orderedLines = report.Lines
            .Where(l => !l.IsSplitChild)
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
        return await AddReceiptPageWithRefAsync(document, line, null, ct);
    }

    /// <summary>
    /// Adds a receipt page with an optional line reference number for cross-referencing with the itemized list.
    /// </summary>
    private async Task<int> AddReceiptPageWithRefAsync(
        PdfDocument document,
        ExpenseLine line,
        int? lineRef,
        CancellationToken ct)
    {
        if (line.Receipt == null)
            return 0;

        // Validate BlobUrl exists before attempting download
        if (string.IsNullOrWhiteSpace(line.Receipt.BlobUrl))
        {
            _logger.LogWarning(
                "Receipt {ReceiptId} for line {LineId} has no BlobUrl - cannot download image",
                line.Receipt.Id, line.Id);
            AddReceiptErrorPageWithRef(document, line, "Receipt image not available.", lineRef);
            return 1;
        }

        try
        {
            // Handle HTML email receipts - they can't be loaded as images directly
            // Use thumbnail if available, otherwise create a placeholder
            if (IsHtmlContentType(line.Receipt.ContentType))
            {
                _logger.LogDebug(
                    "Receipt {ReceiptId} is HTML content type, checking for thumbnail",
                    line.Receipt.Id);

                if (!string.IsNullOrWhiteSpace(line.Receipt.ThumbnailUrl))
                {
                    // Use the thumbnail image instead of the HTML content
                    _logger.LogDebug(
                        "Using thumbnail for HTML receipt {ReceiptId}: {ThumbnailUrl}",
                        line.Receipt.Id, line.Receipt.ThumbnailUrl);

                    return await AddThumbnailPageAsync(document, line, lineRef, ct);
                }
                else
                {
                    // No thumbnail available - create placeholder for HTML receipt
                    _logger.LogDebug(
                        "No thumbnail available for HTML receipt {ReceiptId}, adding placeholder",
                        line.Receipt.Id);

                    AddHtmlReceiptPlaceholderWithRef(document, line, lineRef);
                    return 1;
                }
            }

            // Handle PDF source files - use thumbnail if available, otherwise show placeholder
            // IMPORTANT: Check BEFORE downloading the blob to avoid loading the PDF unnecessarily
            if (string.Equals(line.Receipt.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug(
                    "Receipt {ReceiptId} is PDF content type, checking for thumbnail",
                    line.Receipt.Id);

                if (!string.IsNullOrWhiteSpace(line.Receipt.ThumbnailUrl))
                {
                    // Use the thumbnail image (first page preview) instead of the PDF
                    _logger.LogDebug(
                        "Using thumbnail for PDF receipt {ReceiptId}: {ThumbnailUrl}",
                        line.Receipt.Id, line.Receipt.ThumbnailUrl);

                    return await AddThumbnailPageAsync(document, line, lineRef, ct);
                }
                else
                {
                    // No thumbnail available - create placeholder for PDF receipt
                    _logger.LogDebug(
                        "No thumbnail available for PDF receipt {ReceiptId}, adding placeholder",
                        line.Receipt.Id);

                    AddPdfReceiptPlaceholderWithRef(document, line, lineRef);
                    return 1;
                }
            }

            _logger.LogDebug(
                "Downloading receipt image for line {LineId}, ReceiptId: {ReceiptId}, BlobUrl: {BlobUrl}",
                line.Id, line.Receipt.Id, line.Receipt.BlobUrl);

            using var imageStream = await _blobService.DownloadAsync(line.Receipt.BlobUrl);
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;

            // Detect image format and convert if necessary
            var imageBytes = memoryStream.ToArray();

            // Security: Validate image size and dimensions before processing
            if (!ValidateImageDimensions(imageBytes, out var validationError))
            {
                AddReceiptErrorPageWithRef(document, line, validationError!, lineRef);
                return 1;
            }

            // Handle HEIC/HEIF using ImageSharp
            if (IsHeicFormat(line.Receipt.ContentType, line.Receipt.OriginalFilename))
            {
                imageBytes = await ConvertHeicToJpegAsync(imageBytes, ct);

                // Re-validate converted image
                if (!ValidateImageDimensions(imageBytes, out validationError))
                {
                    AddReceiptErrorPageWithRef(document, line, validationError!, lineRef);
                    return 1;
                }
            }

            // Add image to PDF
            var page = document.AddPage();
            page.Width = PageWidth;
            page.Height = PageHeight;

            using var gfx = XGraphics.FromPdfPage(page);

            // Load image using our custom ImageSharp 3.x compatible implementation
            // (bypasses PdfSharpCore's internal ImageSharp 2.x API calls)
            using var imageSource = ImageSharp3ImageSource.FromBytes(imageBytes);
            var xImage = XImage.FromImageSource(imageSource);

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

            // Draw header with line reference
            DrawReceiptHeaderWithRef(gfx, line, lineRef);

            // Draw image
            gfx.DrawImage(xImage, x, y, scaledWidth, scaledHeight);

            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to process receipt image for line {LineId}, ReceiptId: {ReceiptId}, BlobUrl: {BlobUrl}. Adding error placeholder.",
                line.Id, line.Receipt?.Id, line.Receipt?.BlobUrl);

            // Security: Use sanitized error message - never expose internal details in PDF
            var safeMessage = GetSafeErrorMessage(ex);
            AddReceiptErrorPageWithRef(document, line, safeMessage, lineRef);
            return 1;
        }
    }

    /// <summary>
    /// Draws receipt header with optional line reference (e.g., "#3" to match itemized list).
    /// </summary>
    private void DrawReceiptHeaderWithRef(XGraphics gfx, ExpenseLine line, int? lineRef)
    {
        var fontBold = new XFont(FontFamily, 12, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 10, XFontStyle.Regular);
        var fontRef = new XFont(FontFamily, 14, XFontStyle.Bold);

        var y = Margin;

        // Line reference badge on the right
        if (lineRef.HasValue)
        {
            gfx.DrawString(
                $"#{lineRef.Value}",
                fontRef,
                XBrushes.DarkGreen,
                new XRect(PageWidth - Margin - 40, y, 40, 20),
                XStringFormats.TopRight);
        }

        gfx.DrawString(
            $"Receipt - {line.ExpenseDate:MM/dd/yyyy}",
            fontBold,
            XBrushes.Black,
            new XRect(Margin, y, PageWidth - (2 * Margin) - 50, 20),
            XStringFormats.TopLeft);

        y += 18;

        // Security: Sanitize user-controlled text before rendering
        var vendorDisplay = SanitizeForPdf(line.VendorName, 50);
        var descDisplay = SanitizeForPdf(line.NormalizedDescription, 50);

        var description = !string.IsNullOrEmpty(vendorDisplay)
            ? $"{vendorDisplay} - {line.Amount.ToString("C", UsCulture)}"
            : $"{descDisplay} - {line.Amount.ToString("C", UsCulture)}";

        gfx.DrawString(
            description,
            fontRegular,
            XBrushes.DarkGray,
            new XRect(Margin, y, PageWidth - (2 * Margin), 16),
            XStringFormats.TopLeft);
    }

    private void AddPlaceholderPage(PdfDocument document, ExpenseLine line, ExpenseReport report)
    {
        AddPlaceholderPageWithRef(document, line, report, null);
    }

    /// <summary>
    /// Adds a missing receipt placeholder page with optional line reference.
    /// </summary>
    private void AddPlaceholderPageWithRef(PdfDocument document, ExpenseLine line, ExpenseReport report, int? lineRef)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 18, XFontStyle.Bold);
        var fontHeader = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);
        var fontSmall = new XFont(FontFamily, 10, XFontStyle.Regular);
        var fontRef = new XFont(FontFamily, 16, XFontStyle.Bold);

        var y = Margin + 40;
        var lineHeight = 24;

        // Line reference badge on the right (if provided)
        if (lineRef.HasValue)
        {
            gfx.DrawString(
                $"#{lineRef.Value}",
                fontRef,
                XBrushes.DarkOrange,
                new XRect(PageWidth - Margin - 50, Margin + 10, 50, 24),
                XStringFormats.TopRight);
        }

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

        // Expense details - sanitize all user-controlled text
        DrawLabelValue(gfx, fontHeader, fontRegular, "Employee:",
            SanitizeForPdf(report.User?.DisplayName, 60) ?? "Unknown", textX, y, textWidth);
        y += lineHeight + 10;

        DrawLabelValue(gfx, fontHeader, fontRegular, "Report Period:",
            SanitizeForPdf(report.Period, 30), textX, y, textWidth);
        y += lineHeight + 10;

        DrawLabelValue(gfx, fontHeader, fontRegular, "Expense Date:",
            line.ExpenseDate.ToString("MMMM d, yyyy"), textX, y, textWidth);
        y += lineHeight + 10;

        var vendorName = SanitizeForPdf(line.VendorName, 60);
        vendorName = !string.IsNullOrEmpty(vendorName) ? vendorName : "Unknown";
        DrawLabelValue(gfx, fontHeader, fontRegular, "Vendor:",
            vendorName, textX, y, textWidth);
        y += lineHeight + 10;

        DrawLabelValue(gfx, fontHeader, fontRegular, "Amount:",
            line.Amount.ToString("C", UsCulture), textX, y, textWidth);
        y += lineHeight + 10;

        DrawLabelValue(gfx, fontHeader, fontRegular, "Description:",
            SanitizeForPdf(line.NormalizedDescription, 80), textX, y, textWidth);
        y += lineHeight + 20;

        // Justification section
        var justification = GetJustificationText(line.MissingReceiptJustification);
        DrawLabelValue(gfx, fontHeader, fontRegular, "Reason for Missing Receipt:",
            justification, textX, y, textWidth);
        y += lineHeight + 10;

        if (!string.IsNullOrEmpty(line.JustificationNote))
        {
            // Security: User-provided justification notes need sanitization
            DrawLabelValue(gfx, fontHeader, fontRegular, "Additional Notes:",
                SanitizeForPdf(line.JustificationNote, 150), textX, y, textWidth);
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

    private void AddPdfReceiptPlaceholderWithRef(PdfDocument document, ExpenseLine line, int? lineRef)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);
        var fontRef = new XFont(FontFamily, 16, XFontStyle.Bold);

        // Line reference badge
        if (lineRef.HasValue)
        {
            gfx.DrawString(
                $"#{lineRef.Value}",
                fontRef,
                XBrushes.DarkGreen,
                new XRect(PageWidth - Margin - 50, Margin + 10, 50, 24),
                XStringFormats.TopRight);
        }

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
            $"Amount: {line.Amount.ToString("C", UsCulture)}",
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

    /// <summary>
    /// Checks if the content type indicates an HTML receipt (email receipts).
    /// </summary>
    private static bool IsHtmlContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        return contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
               contentType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Adds a page using the receipt's thumbnail image (for HTML and PDF receipts with thumbnails).
    /// </summary>
    private async Task<int> AddThumbnailPageAsync(
        PdfDocument document,
        ExpenseLine line,
        int? lineRef,
        CancellationToken ct)
    {
        try
        {
            using var imageStream = await _blobService.DownloadAsync(line.Receipt!.ThumbnailUrl!);
            using var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;

            var imageBytes = memoryStream.ToArray();

            // Validate thumbnail dimensions
            if (!ValidateImageDimensions(imageBytes, out var validationError))
            {
                AddReceiptErrorPageWithRef(document, line, validationError!, lineRef);
                return 1;
            }

            // Add thumbnail image to PDF
            var page = document.AddPage();
            page.Width = PageWidth;
            page.Height = PageHeight;

            using var gfx = XGraphics.FromPdfPage(page);

            // Use custom ImageSharp 3.x compatible image source
            using var imageSource = ImageSharp3ImageSource.FromBytes(imageBytes);
            var xImage = XImage.FromImageSource(imageSource);

            // Calculate scaling to fit page with margins
            var availableWidth = PageWidth - (2 * Margin);
            var availableHeight = PageHeight - (2 * Margin) - 60;

            var scale = Math.Min(
                availableWidth / xImage.PixelWidth,
                availableHeight / xImage.PixelHeight);

            var scaledWidth = xImage.PixelWidth * scale;
            var scaledHeight = xImage.PixelHeight * scale;

            var x = (PageWidth - scaledWidth) / 2;
            var y = Margin + 50;

            // Draw header
            DrawReceiptHeaderWithRef(gfx, line, lineRef);

            // Draw thumbnail image
            gfx.DrawImage(xImage, x, y, scaledWidth, scaledHeight);

            // Add note based on content type
            var fontSmall = new XFont(FontFamily, 8, XFontStyle.Italic);
            var footnote = GetThumbnailFootnote(line.Receipt.ContentType);
            gfx.DrawString(
                footnote,
                fontSmall,
                XBrushes.Gray,
                new XRect(0, PageHeight - Margin - 10, PageWidth, 12),
                XStringFormats.BottomCenter);

            return 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load thumbnail for receipt {ReceiptId} (type: {ContentType}), adding placeholder",
                line.Receipt?.Id, line.Receipt?.ContentType);

            // Fall back to appropriate placeholder based on content type
            if (IsHtmlContentType(line.Receipt?.ContentType))
            {
                AddHtmlReceiptPlaceholderWithRef(document, line, lineRef);
            }
            else if (string.Equals(line.Receipt?.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                AddPdfReceiptPlaceholderWithRef(document, line, lineRef);
            }
            else
            {
                AddReceiptErrorPageWithRef(document, line, "Thumbnail could not be loaded.", lineRef);
            }
            return 1;
        }
    }

    /// <summary>
    /// Gets the appropriate footnote text for a thumbnail based on the original receipt content type.
    /// </summary>
    private static string GetThumbnailFootnote(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return "(Thumbnail preview)";

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            return "(PDF receipt - first page preview)";

        if (contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) ||
            contentType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
            return "(Email receipt - thumbnail preview)";

        return "(Thumbnail preview)";
    }

    /// <summary>
    /// Creates a placeholder page for HTML email receipts without thumbnails.
    /// </summary>
    private void AddHtmlReceiptPlaceholderWithRef(PdfDocument document, ExpenseLine line, int? lineRef)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);
        var fontRef = new XFont(FontFamily, 16, XFontStyle.Bold);

        // Line reference badge
        if (lineRef.HasValue)
        {
            gfx.DrawString(
                $"#{lineRef.Value}",
                fontRef,
                XBrushes.DarkBlue,
                new XRect(PageWidth - Margin - 50, Margin + 10, 50, 24),
                XStringFormats.TopRight);
        }

        var y = PageHeight / 2 - 80;

        gfx.DrawString(
            "Email Receipt",
            fontTitle,
            XBrushes.DarkBlue,
            new XRect(0, y, PageWidth, 20),
            XStringFormats.TopCenter);

        y += 30;

        gfx.DrawString(
            line.VendorName ?? "Unknown Vendor",
            fontRegular,
            XBrushes.Black,
            new XRect(0, y, PageWidth, 18),
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
            $"Amount: {line.Amount.ToString("C", UsCulture)}",
            fontRegular,
            XBrushes.DarkGray,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);

        y += 40;

        gfx.DrawString(
            "(Original email receipt available in system)",
            fontRegular,
            XBrushes.Gray,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);
    }

    private void AddReceiptErrorPage(PdfDocument document, ExpenseLine line, string errorMessage)
    {
        AddReceiptErrorPageWithRef(document, line, errorMessage, null);
    }

    private void AddReceiptErrorPageWithRef(PdfDocument document, ExpenseLine line, string errorMessage, int? lineRef)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);
        var fontSmall = new XFont(FontFamily, 10, XFontStyle.Regular);
        var fontRef = new XFont(FontFamily, 16, XFontStyle.Bold);

        // Line reference badge
        if (lineRef.HasValue)
        {
            gfx.DrawString(
                $"#{lineRef.Value}",
                fontRef,
                XBrushes.DarkRed,
                new XRect(PageWidth - Margin - 50, Margin + 10, 50, 24),
                XStringFormats.TopRight);
        }

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
            $"Amount: {line.Amount.ToString("C", UsCulture)}",
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

    /// <summary>
    /// Sanitizes text for safe PDF rendering by removing control characters
    /// and limiting length to prevent buffer/display issues.
    /// </summary>
    private static string SanitizeForPdf(string? text, int maxLength = 200)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // Remove control characters (except newline/tab which are sometimes intentional)
        var sanitized = new string(text
            .Where(c => !char.IsControl(c) || c == '\n' || c == '\t')
            .ToArray());

        // Trim and limit length
        sanitized = sanitized.Trim();
        if (sanitized.Length > maxLength)
            sanitized = sanitized[..(maxLength - 3)] + "...";

        return sanitized;
    }

    /// <summary>
    /// Creates a safe, generic error message for PDF display.
    /// Actual error details are logged server-side only.
    /// </summary>
    private static string GetSafeErrorMessage(Exception ex)
    {
        // Map known exception types to user-friendly messages
        // Never expose internal paths, stack traces, or implementation details
        return ex switch
        {
            FileNotFoundException => "The receipt file could not be found.",
            UnauthorizedAccessException => "Access to the receipt file was denied.",
            InvalidOperationException when ex.Message.Contains("format", StringComparison.OrdinalIgnoreCase)
                => "The receipt image format is not supported.",
            _ => "The receipt could not be processed. Please contact support if this persists."
        };
    }

    /// <summary>
    /// Validates image dimensions are within safe limits to prevent DoS attacks.
    /// Note: If ImageSharp validation fails due to library issues, we allow the image
    /// to proceed to PdfSharpCore loading which may succeed with its own decoders.
    /// </summary>
    private bool ValidateImageDimensions(byte[] imageBytes, out string? errorMessage)
    {
        errorMessage = null;

        if (imageBytes.Length > MaxImageBytes)
        {
            errorMessage = "Receipt image file size exceeds maximum allowed.";
            _logger.LogWarning("Image rejected: size {Size} bytes exceeds limit {Limit}",
                imageBytes.Length, MaxImageBytes);
            return false;
        }

        try
        {
            using var stream = new MemoryStream(imageBytes);
            var imageInfo = Image.Identify(stream);

            if (imageInfo == null)
            {
                // ImageSharp couldn't identify the format, but let PdfSharpCore try
                _logger.LogDebug("ImageSharp couldn't identify format, allowing PdfSharpCore to try");
                return true;
            }

            if (imageInfo.Width > MaxImageWidth || imageInfo.Height > MaxImageHeight)
            {
                errorMessage = "Receipt image dimensions exceed maximum allowed.";
                _logger.LogWarning("Image rejected: dimensions {Width}x{Height} exceed limit {MaxW}x{MaxH}",
                    imageInfo.Width, imageInfo.Height, MaxImageWidth, MaxImageHeight);
                return false;
            }

            return true;
        }
        catch (MissingMethodException ex)
        {
            // ImageSharp version/assembly mismatch - allow PdfSharpCore to try
            _logger.LogWarning(ex, "ImageSharp method not found (version mismatch), bypassing validation");
            return true;
        }
        catch (TypeLoadException ex)
        {
            // ImageSharp assembly loading issue - allow PdfSharpCore to try
            _logger.LogWarning(ex, "ImageSharp type load error, bypassing validation");
            return true;
        }
        catch (UnknownImageFormatException ex)
        {
            // Image format not recognized by ImageSharp, but PdfSharpCore might handle it
            _logger.LogDebug(ex, "ImageSharp unknown format, allowing PdfSharpCore to try");
            return true;
        }
        catch (Exception ex)
        {
            // For other errors (e.g., corrupt data), allow PdfSharpCore to make final decision
            _logger.LogWarning(ex, "Image validation failed, allowing PdfSharpCore to attempt loading");
            return true;
        }
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

        using var document = new PdfDocument();
        document.Info.Title = $"Expense Report - {request.Period}";
        document.Info.Author = employeeName;

        var page = document.AddPage();
        using var gfx = XGraphics.FromPdfPage(page);
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

        // Security: Sanitize user-controlled employee name
        gfx.DrawString(
            $"Employee: {SanitizeForPdf(employeeName, 60)}",
            fontRegular,
            XBrushes.Black,
            new XRect(Margin, y, PageWidth - (2 * Margin), 15),
            XStringFormats.TopLeft);

        y += 20;

        gfx.DrawString(
            $"Total Expenses: {request.Lines.Count} | Amount: {request.Lines.Sum(l => l.Amount).ToString("C", UsCulture)}",
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
            // Security: Sanitize user-controlled text
            gfx.DrawString(TruncateString(SanitizeForPdf(line.VendorName, 20), 18), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[1], 10), XStringFormats.TopLeft);
            colX += colWidths[1];
            gfx.DrawString(SanitizeForPdf(line.GlCode, 15), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[2], 10), XStringFormats.TopLeft);
            colX += colWidths[2];
            gfx.DrawString(SanitizeForPdf(line.DepartmentCode, 10), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[3], 10), XStringFormats.TopLeft);
            colX += colWidths[3];
            gfx.DrawString(TruncateString(SanitizeForPdf(line.Description, 25), 22), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[4], 10), XStringFormats.TopLeft);
            colX += colWidths[4];
            gfx.DrawString(line.Amount.ToString("C", UsCulture), fontSmall, XBrushes.Black, new XRect(colX, y, colWidths[5], 10), XStringFormats.TopRight);

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

    /// <inheritdoc />
    public async Task<ReceiptPdfDto> GenerateCompleteReportPdfAsync(Guid reportId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating complete PDF report (itemized + receipts) for report {ReportId}", reportId);

        var report = await _reportRepository.GetByIdWithLinesAsync(reportId, ct)
            ?? throw new InvalidOperationException($"Report {reportId} not found");

        using var document = new PdfDocument();
        document.Info.Title = $"Expense Report - {report.Period}";
        document.Info.Author = report.User?.DisplayName ?? "ExpenseFlow";

        // Filter out split child lines - only include parent lines
        // (child allocations are shown as nested rows in the itemized section)
        var orderedLines = report.Lines
            .Where(l => !l.IsSplitChild)
            .OrderBy(l => l.LineOrder)
            .Take(_options.MaxReceiptsPerPdf)
            .ToList();

        // Section 1: Generate itemized expense list pages
        var summaryPageCount = AddItemizedSummarySection(document, report, orderedLines);

        // Section 2: Generate receipt pages with line references
        int receiptPageCount = 0;
        int placeholderCount = 0;
        int lineRef = 0;

        foreach (var line in orderedLines)
        {
            ct.ThrowIfCancellationRequested();
            lineRef++;

            if (line.HasReceipt && line.Receipt != null)
            {
                var pagesAdded = await AddReceiptPageWithRefAsync(document, line, lineRef, ct);
                receiptPageCount += pagesAdded;
            }
            else
            {
                AddPlaceholderPageWithRef(document, line, report, lineRef);
                receiptPageCount++;
                placeholderCount++;
            }
        }

        // If no receipts at all, add informational page
        if (receiptPageCount == 0)
        {
            AddNoReceiptsInfoPage(document);
            receiptPageCount = 1;
        }

        await using var outputStream = new MemoryStream();
        document.Save(outputStream, false);

        var totalPageCount = summaryPageCount + receiptPageCount;

        _logger.LogInformation(
            "Generated complete PDF report for {ReportId}: {SummaryPages} summary + {ReceiptPages} receipts ({Placeholders} placeholders)",
            reportId, summaryPageCount, receiptPageCount, placeholderCount);

        return new ReceiptPdfDto
        {
            FileName = $"{report.Period}-complete-report.pdf",
            ContentType = "application/pdf",
            FileContents = outputStream.ToArray(),
            PageCount = totalPageCount,
            PlaceholderCount = placeholderCount
        };
    }

    private int AddItemizedSummarySection(PdfDocument document, ExpenseReport report, List<ExpenseLine> lines)
    {
        int pageCount = 0;
        var linesPerPage = 30;
        var totalPages = (int)Math.Ceiling((double)lines.Count / linesPerPage);

        for (int pageNum = 0; pageNum < Math.Max(1, totalPages); pageNum++)
        {
            var page = document.AddPage();
            page.Width = PageWidth;
            page.Height = PageHeight;
            pageCount++;

            using var gfx = XGraphics.FromPdfPage(page);

            var fontTitle = new XFont(FontFamily, 16, XFontStyle.Bold);
            var fontHeader = new XFont(FontFamily, 11, XFontStyle.Bold);
            var fontRegular = new XFont(FontFamily, 10, XFontStyle.Regular);
            var fontSmall = new XFont(FontFamily, 8, XFontStyle.Regular);

            double y = Margin;

            // Title - only on first page
            if (pageNum == 0)
            {
                gfx.DrawString(
                    $"EXPENSE REPORT - {report.Period}",
                    fontTitle,
                    XBrushes.Black,
                    new XRect(Margin, y, PageWidth - (2 * Margin), 24),
                    XStringFormats.TopCenter);

                y += 35;

                gfx.DrawString(
                    $"Employee: {report.User?.DisplayName ?? "Unknown"}",
                    fontRegular,
                    XBrushes.Black,
                    new XRect(Margin, y, PageWidth - (2 * Margin), 16),
                    XStringFormats.TopLeft);

                y += 20;

                var totalAmount = lines.Sum(l => l.Amount);
                gfx.DrawString(
                    $"Total Expenses: {lines.Count} items | Total Amount: {totalAmount.ToString("C", UsCulture)}",
                    fontRegular,
                    XBrushes.Black,
                    new XRect(Margin, y, PageWidth - (2 * Margin), 16),
                    XStringFormats.TopLeft);

                y += 30;
            }
            else
            {
                // Continuation header
                gfx.DrawString(
                    $"Expense Report - {report.Period} (Page {pageNum + 1})",
                    fontHeader,
                    XBrushes.Black,
                    new XRect(Margin, y, PageWidth - (2 * Margin), 18),
                    XStringFormats.TopLeft);
                y += 25;
            }

            // Table header
            var colWidths = new[] { 65.0, 100.0, 55.0, 50.0, 120.0, 60.0, 60.0 };
            // Date, Vendor, GL, Dept, Description, Amount, Receipt
            var colX = Margin;

            gfx.DrawString("Date", fontHeader, XBrushes.Black, new XRect(colX, y, colWidths[0], 14), XStringFormats.TopLeft);
            colX += colWidths[0];
            gfx.DrawString("Vendor", fontHeader, XBrushes.Black, new XRect(colX, y, colWidths[1], 14), XStringFormats.TopLeft);
            colX += colWidths[1];
            gfx.DrawString("GL Code", fontHeader, XBrushes.Black, new XRect(colX, y, colWidths[2], 14), XStringFormats.TopLeft);
            colX += colWidths[2];
            gfx.DrawString("Dept", fontHeader, XBrushes.Black, new XRect(colX, y, colWidths[3], 14), XStringFormats.TopLeft);
            colX += colWidths[3];
            gfx.DrawString("Description", fontHeader, XBrushes.Black, new XRect(colX, y, colWidths[4], 14), XStringFormats.TopLeft);
            colX += colWidths[4];
            gfx.DrawString("Amount", fontHeader, XBrushes.Black, new XRect(colX, y, colWidths[5], 14), XStringFormats.TopRight);
            colX += colWidths[5];
            gfx.DrawString("Receipt", fontHeader, XBrushes.Black, new XRect(colX, y, colWidths[6], 14), XStringFormats.TopCenter);

            y += 16;
            gfx.DrawLine(XPens.Black, Margin, y, PageWidth - Margin, y);
            y += 6;

            // Draw table rows for this page
            var pageLines = lines.Skip(pageNum * linesPerPage).Take(linesPerPage).ToList();
            var lineIndex = pageNum * linesPerPage;

            foreach (var line in pageLines)
            {
                colX = Margin;
                lineIndex++;

                // Check if this is a split parent with child allocations
                var isSplitLine = line.IsSplitParent && line.ChildAllocations.Any();

                gfx.DrawString(line.ExpenseDate.ToString("MM/dd/yy"), fontSmall, XBrushes.Black,
                    new XRect(colX, y, colWidths[0], 12), XStringFormats.TopLeft);
                colX += colWidths[0];

                // Security: Sanitize user-controlled vendor name
                var vendorName = SanitizeForPdf(line.VendorName, 20);
                vendorName = !string.IsNullOrEmpty(vendorName) ? vendorName : "Unknown";
                gfx.DrawString(TruncateString(vendorName, 16), fontSmall, XBrushes.Black,
                    new XRect(colX, y, colWidths[1], 12), XStringFormats.TopLeft);
                colX += colWidths[1];

                // For split lines, show "[Split]" in GL column; otherwise show GL code
                var glDisplay = isSplitLine ? "[Split]" : (line.GLCode ?? "");
                gfx.DrawString(glDisplay, fontSmall, isSplitLine ? XBrushes.Blue : XBrushes.Black,
                    new XRect(colX, y, colWidths[2], 12), XStringFormats.TopLeft);
                colX += colWidths[2];

                // For split lines, leave department blank (shown in allocations below)
                var deptDisplay = isSplitLine ? "" : (line.DepartmentCode ?? "");
                gfx.DrawString(deptDisplay, fontSmall, XBrushes.Black,
                    new XRect(colX, y, colWidths[3], 12), XStringFormats.TopLeft);
                colX += colWidths[3];

                // Security: Sanitize user-controlled description
                var descriptionDisplay = SanitizeForPdf(line.NormalizedDescription, 25);
                gfx.DrawString(TruncateString(descriptionDisplay, 20), fontSmall, XBrushes.Black,
                    new XRect(colX, y, colWidths[4], 12), XStringFormats.TopLeft);
                colX += colWidths[4];

                gfx.DrawString(line.Amount.ToString("C", UsCulture), fontSmall, XBrushes.Black,
                    new XRect(colX, y, colWidths[5], 12), XStringFormats.TopRight);
                colX += colWidths[5];

                // Receipt indicator with line reference
                var receiptIndicator = line.HasReceipt ? $"#{lineIndex}" : "—";
                var receiptBrush = line.HasReceipt ? XBrushes.DarkGreen : XBrushes.DarkOrange;
                gfx.DrawString(receiptIndicator, fontSmall, receiptBrush,
                    new XRect(colX, y, colWidths[6], 12), XStringFormats.TopCenter);

                y += 14;

                // Draw split allocation rows if this is a split parent
                if (isSplitLine)
                {
                    var fontAlloc = new XFont(FontFamily, 7, XFontStyle.Italic);
                    var allocations = line.ChildAllocations
                        .OrderBy(a => a.AllocationOrder ?? 0)
                        .ThenBy(a => a.DepartmentCode)
                        .ToList();

                    foreach (var alloc in allocations)
                    {
                        colX = Margin + 10; // Indent allocation rows

                        // Arrow indicator for allocation row
                        gfx.DrawString("└→", fontAlloc, XBrushes.Gray,
                            new XRect(colX, y, 20, 10), XStringFormats.TopLeft);
                        colX += colWidths[0] - 10;

                        // Skip vendor column for allocations
                        colX += colWidths[1];

                        // GL Code for this allocation
                        gfx.DrawString(alloc.GLCode ?? line.GLCode ?? "", fontAlloc, XBrushes.DarkBlue,
                            new XRect(colX, y, colWidths[2], 10), XStringFormats.TopLeft);
                        colX += colWidths[2];

                        // Department for this allocation
                        gfx.DrawString(alloc.DepartmentCode ?? "", fontAlloc, XBrushes.DarkBlue,
                            new XRect(colX, y, colWidths[3], 10), XStringFormats.TopLeft);
                        colX += colWidths[3];

                        // Percentage in description column
                        var percentText = alloc.SplitPercentage.HasValue
                            ? $"{alloc.SplitPercentage.Value:F1}%"
                            : "";
                        gfx.DrawString(percentText, fontAlloc, XBrushes.Gray,
                            new XRect(colX, y, colWidths[4], 10), XStringFormats.TopLeft);
                        colX += colWidths[4];

                        // Amount for this allocation
                        gfx.DrawString(alloc.Amount.ToString("C", UsCulture), fontAlloc, XBrushes.DarkBlue,
                            new XRect(colX, y, colWidths[5], 10), XStringFormats.TopRight);

                        y += 11;

                        if (y > PageHeight - Margin - 30)
                            break;
                    }
                }

                if (y > PageHeight - Margin - 30)
                    break;
            }

            // Footer with page number
            gfx.DrawString(
                $"Page {pageNum + 1} of {Math.Max(1, totalPages)} (Itemized List)",
                fontSmall,
                XBrushes.Gray,
                new XRect(0, PageHeight - Margin, PageWidth, 14),
                XStringFormats.TopCenter);
        }

        return pageCount;
    }

    private void AddReceiptsSectionHeader(PdfDocument document)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 24, XFontStyle.Bold);
        var fontSubtitle = new XFont(FontFamily, 12, XFontStyle.Regular);

        // Center the section header
        var y = PageHeight / 2 - 60;

        gfx.DrawString(
            "RECEIPT IMAGES",
            fontTitle,
            XBrushes.Black,
            new XRect(0, y, PageWidth, 30),
            XStringFormats.TopCenter);

        y += 50;

        gfx.DrawString(
            "The following pages contain receipt images for the expense items listed above.",
            fontSubtitle,
            XBrushes.Gray,
            new XRect(Margin, y, PageWidth - (2 * Margin), 20),
            XStringFormats.TopCenter);

        y += 25;

        gfx.DrawString(
            "Each receipt is labeled with its corresponding expense item reference number.",
            fontSubtitle,
            XBrushes.Gray,
            new XRect(Margin, y, PageWidth - (2 * Margin), 20),
            XStringFormats.TopCenter);
    }

    private void AddNoReceiptsInfoPage(PdfDocument document)
    {
        var page = document.AddPage();
        page.Width = PageWidth;
        page.Height = PageHeight;

        using var gfx = XGraphics.FromPdfPage(page);

        var fontTitle = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontRegular = new XFont(FontFamily, 12, XFontStyle.Regular);

        var y = PageHeight / 2 - 30;

        gfx.DrawString(
            "No Receipt Images Available",
            fontTitle,
            XBrushes.Gray,
            new XRect(0, y, PageWidth, 20),
            XStringFormats.TopCenter);

        y += 30;

        gfx.DrawString(
            "No receipt images were found for this expense report.",
            fontRegular,
            XBrushes.Gray,
            new XRect(0, y, PageWidth, 18),
            XStringFormats.TopCenter);
    }

    /// <summary>
    /// Custom IImageSource implementation that uses ImageSharp 3.x APIs.
    /// This bypasses PdfSharpCore's internal ImageSharp usage which is incompatible with ImageSharp 3.x.
    /// </summary>
    private sealed class ImageSharp3ImageSource : IImageSource, IDisposable
    {
        private readonly Image<Rgba32> _image;
        private readonly int _quality;
        private bool _disposed;

        public int Width => _image.Width;
        public int Height => _image.Height;
        public string Name { get; }
        public bool Transparent { get; set; }

        private ImageSharp3ImageSource(string name, Image<Rgba32> image, int quality, bool isTransparent)
        {
            Name = name;
            _image = image;
            _quality = quality;
            Transparent = isTransparent;
        }

        /// <summary>
        /// Creates an ImageSharp3ImageSource from a byte array using ImageSharp 3.x APIs.
        /// Returns concrete type for proper IDisposable support.
        /// </summary>
        public static ImageSharp3ImageSource FromBytes(byte[] imageBytes, int quality = 75)
        {
            using var stream = new MemoryStream(imageBytes);
            var image = Image.Load<Rgba32>(stream);

            // Detect if image has transparency (PNG with alpha channel)
            var hasTransparency = false;
            if (image.PixelType.AlphaRepresentation != null)
            {
                // Check a sample of pixels for actual transparency
                hasTransparency = HasActualTransparency(image);
            }

            return new ImageSharp3ImageSource(
                Guid.NewGuid().ToString(),
                image,
                quality,
                hasTransparency);
        }

        private static bool HasActualTransparency(Image<Rgba32> image)
        {
            // Sample a subset of pixels to check for transparency
            var step = Math.Max(1, Math.Min(image.Width, image.Height) / 20);
            for (var y = 0; y < image.Height; y += step)
            {
                for (var x = 0; x < image.Width; x += step)
                {
                    if (image[x, y].A < 255)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void SaveAsJpeg(MemoryStream ms)
        {
            _image.SaveAsJpeg(ms, new JpegEncoder { Quality = _quality });
        }

        public void SaveAsPdfBitmap(MemoryStream ms)
        {
            var encoder = new BmpEncoder { BitsPerPixel = BmpBitsPerPixel.Pixel32 };
            _image.Save(ms, encoder);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _image.Dispose();
                _disposed = true;
            }
        }
    }
}
