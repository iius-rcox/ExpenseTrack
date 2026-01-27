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
    private readonly IUserPreferencesService _preferencesService;
    private readonly ExportOptions _options;
    private readonly ILogger<PdfGenerationService> _logger;

    // PDF page dimensions (Letter size in points)
    private const double PageWidth = 612;
    private const double PageHeight = 792;
    private const double Margin = 36; // 0.5 inch margin

    // Grid layout for receipts (6 per page in 2 columns × 3 rows)
    private const int ReceiptsPerPage = 6;
    private const int GridColumns = 2;
    private const int GridRows = 3;
    private const double GridGapX = 12; // Horizontal gap between cells
    private const double GridGapY = 10; // Vertical gap between cells
    private const double GridHeaderHeight = 40; // Space for page header

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
        IUserPreferencesService preferencesService,
        IOptions<ExportOptions> options,
        ILogger<PdfGenerationService> logger)
    {
        _reportRepository = reportRepository;
        _blobService = blobService;
        _preferencesService = preferencesService;
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

        // Load user preferences for PDF header customization
        UserPreferences? userPrefs = null;
        if (report.UserId != Guid.Empty)
        {
            userPrefs = await _preferencesService.GetOrCreateDefaultsAsync(report.UserId);
        }

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

        // Build mapping of shared receipts (combined transactions)
        // Key: ReceiptId, Value: list of (lineIndex, LineId) tuples
        var sharedReceiptMap = new Dictionary<Guid, List<(int lineIndex, Guid lineId)>>();
        for (int i = 0; i < orderedLines.Count; i++)
        {
            var line = orderedLines[i];
            if (line.HasReceipt && line.ReceiptId.HasValue)
            {
                if (!sharedReceiptMap.ContainsKey(line.ReceiptId.Value))
                    sharedReceiptMap[line.ReceiptId.Value] = new List<(int, Guid)>();
                sharedReceiptMap[line.ReceiptId.Value].Add((i + 1, line.Id)); // 1-based index
            }
        }

        // Identify which receipt groups are combined (more than 1 line)
        var combinedReceiptIds = sharedReceiptMap
            .Where(kvp => kvp.Value.Count > 1)
            .Select(kvp => kvp.Key)
            .ToHashSet();

        // Section 1: Generate itemized expense list pages
        var summaryPageCount = AddItemizedSummarySection(document, report, orderedLines, sharedReceiptMap, combinedReceiptIds, userPrefs);

        // Section 2: Generate receipt pages with line references (6 per page grid layout)
        int receiptPageCount = 0;
        int placeholderCount = 0;

        // Collect receipt data for grid rendering, deduplicating by ReceiptId
        // Combined transactions (multiple lines sharing one receipt) show as single cell
        var receiptItems = new List<ReceiptGridItem>();
        var receiptIdToItem = new Dictionary<Guid, ReceiptGridItem>();
        int lineRef = 0;

        // Build mapping of line references and track which receipts are shared
        var lineRefMap = new Dictionary<Guid, int>(); // LineId -> lineRef
        foreach (var line in orderedLines)
        {
            lineRef++;
            lineRefMap[line.Id] = lineRef;
        }

        lineRef = 0;
        foreach (var line in orderedLines)
        {
            ct.ThrowIfCancellationRequested();
            lineRef++;

            // Check if this receipt was already processed (combined transaction)
            if (line.HasReceipt && line.ReceiptId.HasValue && receiptIdToItem.TryGetValue(line.ReceiptId.Value, out var existingItem))
            {
                // Add this line's reference to existing item
                existingItem.LineRefs.Add(lineRef);
                existingItem.CombinedAmount += line.Amount;
                continue; // Skip creating new item - receipt already in grid
            }

            var item = new ReceiptGridItem
            {
                Line = line,
                LineRefs = new List<int> { lineRef },
                CombinedAmount = line.Amount,
                Report = report
            };

            // Pre-load image data for receipts
            if (line.HasReceipt && line.Receipt != null)
            {
                item.ImageData = await LoadReceiptImageDataAsync(line, ct);
                if (item.ImageData == null)
                {
                    // Track placeholders for receipts that couldn't be loaded
                    item.IsPlaceholder = true;
                    item.PlaceholderReason = GetPlaceholderReason(line);
                }

                // Track by ReceiptId for deduplication
                if (line.ReceiptId.HasValue)
                {
                    receiptIdToItem[line.ReceiptId.Value] = item;
                }
            }
            else
            {
                item.IsPlaceholder = true;
                item.PlaceholderReason = "Missing";
                placeholderCount++;
            }

            receiptItems.Add(item);
        }

        // Render receipts in grid pages (6 per page)
        receiptPageCount = AddReceiptGridPages(document, receiptItems, report);

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

    private int AddItemizedSummarySection(
        PdfDocument document,
        ExpenseReport report,
        List<ExpenseLine> lines,
        Dictionary<Guid, List<(int lineIndex, Guid lineId)>> sharedReceiptMap,
        HashSet<Guid> combinedReceiptIds,
        UserPreferences? userPrefs)
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

            // Header - only on first page
            if (pageNum == 0)
            {
                var totalAmount = lines.Sum(l => l.Amount);
                y = DrawFormHeader(gfx, report, totalAmount, lines, userPrefs);
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
                // For combined receipts, show shared reference (e.g., "★#1" to indicate it's grouped)
                string receiptIndicator;
                XBrush receiptBrush;

                if (!line.HasReceipt)
                {
                    receiptIndicator = "—";
                    receiptBrush = XBrushes.DarkOrange;
                }
                else if (line.ReceiptId.HasValue && combinedReceiptIds.Contains(line.ReceiptId.Value))
                {
                    // Combined receipt - find the first line index in the group (the "primary" reference)
                    var group = sharedReceiptMap[line.ReceiptId.Value];
                    var primaryRef = group.OrderBy(g => g.lineIndex).First().lineIndex;
                    receiptIndicator = lineIndex == primaryRef ? $"★#{primaryRef}" : $"→#{primaryRef}";
                    receiptBrush = XBrushes.DodgerBlue;
                }
                else
                {
                    receiptIndicator = $"#{lineIndex}";
                    receiptBrush = XBrushes.DarkGreen;
                }

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
    /// Draws the company form header with logo area, form info, and employee details.
    /// Matches the I&amp;I Expense &amp; Mileage Reimbursement form design.
    /// Uses user preferences for employee info when available.
    /// </summary>
    /// <returns>The Y position after the header for content to continue.</returns>
    private double DrawFormHeader(
        XGraphics gfx,
        ExpenseReport report,
        decimal totalAmount,
        List<ExpenseLine> lines,
        UserPreferences? userPrefs)
    {
        // Colors
        var logoBlue = XColor.FromArgb(0, 51, 102); // Dark navy blue for logo
        var borderPen = new XPen(XColors.Black, 1);
        var thinBorderPen = new XPen(XColors.Black, 0.5);

        // Fonts
        var fontLogo = new XFont(FontFamily, 28, XFontStyle.Bold);
        var fontFormLabel = new XFont(FontFamily, 9, XFontStyle.Regular);
        var fontFormTitle = new XFont(FontFamily, 12, XFontStyle.Bold);
        var fontRevision = new XFont(FontFamily, 9, XFontStyle.Regular);
        var fontRevisionValue = new XFont(FontFamily, 14, XFontStyle.Bold);
        var fontDateLabel = new XFont(FontFamily, 8, XFontStyle.Regular);
        var fontDateValue = new XFont(FontFamily, 9, XFontStyle.Bold);
        var fontInfoLabel = new XFont(FontFamily, 9, XFontStyle.Regular);
        var fontInfoValue = new XFont(FontFamily, 10, XFontStyle.Regular);

        double y = Margin;
        double contentWidth = PageWidth - (2 * Margin);

        // ============================================
        // TOP SECTION: Logo + Form Title + Revision + Dates
        // ============================================
        double topBoxHeight = 50;
        double topBoxY = y;

        // Draw outer border for top box
        gfx.DrawRectangle(borderPen, Margin, topBoxY, contentWidth, topBoxHeight);

        // Section widths
        double logoSectionWidth = 280;  // Logo + Form title
        double revisionSectionWidth = 80;
        double dateSectionWidth = contentWidth - logoSectionWidth - revisionSectionWidth;

        // Vertical dividers
        double divider1X = Margin + logoSectionWidth;
        double divider2X = divider1X + revisionSectionWidth;
        gfx.DrawLine(thinBorderPen, divider1X, topBoxY, divider1X, topBoxY + topBoxHeight);
        gfx.DrawLine(thinBorderPen, divider2X, topBoxY, divider2X, topBoxY + topBoxHeight);

        // --- LOGO SECTION (Left) ---
        // Draw company logo image from base64
        if (!string.IsNullOrEmpty(_options.LogoBase64))
        {
            try
            {
                var logoBytes = Convert.FromBase64String(_options.LogoBase64);
                var logoImage = XImage.FromStream(() => new MemoryStream(logoBytes));

                // Scale logo to fit within the logo section while maintaining aspect ratio
                double maxWidth = 55;
                double maxHeight = 35;
                double logoWidth = logoImage.PixelWidth;
                double logoHeight = logoImage.PixelHeight;
                double scale = Math.Min(maxWidth / logoWidth, maxHeight / logoHeight);
                double drawWidth = logoWidth * scale;
                double drawHeight = logoHeight * scale;

                // Center the logo in the logo area
                double logoX = Margin + 8 + (maxWidth - drawWidth) / 2;
                double logoY = topBoxY + 8 + (maxHeight - drawHeight) / 2;

                gfx.DrawImage(logoImage, logoX, logoY, drawWidth, drawHeight);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to draw logo image, falling back to text");
                var logoBrush = new XSolidBrush(logoBlue);
                gfx.DrawString(
                    _options.CompanyName,
                    fontLogo,
                    logoBrush,
                    new XRect(Margin + 8, topBoxY + 8, 60, 40),
                    XStringFormats.TopLeft);
            }
        }
        else
        {
            // Fallback to text if no logo configured
            var logoBrush = new XSolidBrush(logoBlue);
            gfx.DrawString(
                _options.CompanyName,
                fontLogo,
                logoBrush,
                new XRect(Margin + 8, topBoxY + 8, 60, 40),
                XStringFormats.TopLeft);
        }

        // Form label and title
        gfx.DrawString(
            $"{_options.CompanyName} Form:",
            fontFormLabel,
            XBrushes.Black,
            new XRect(Margin + 70, topBoxY + 10, 200, 12),
            XStringFormats.TopLeft);

        gfx.DrawString(
            _options.FormName,
            fontFormTitle,
            XBrushes.Black,
            new XRect(Margin + 70, topBoxY + 24, 200, 20),
            XStringFormats.TopLeft);

        // --- REVISION SECTION (Center) ---
        gfx.DrawString(
            "Revision",
            fontRevision,
            XBrushes.Black,
            new XRect(divider1X + 5, topBoxY + 10, revisionSectionWidth - 10, 12),
            XStringFormats.TopCenter);

        gfx.DrawString(
            _options.FormRevision,
            fontRevisionValue,
            XBrushes.Black,
            new XRect(divider1X + 5, topBoxY + 24, revisionSectionWidth - 10, 20),
            XStringFormats.TopCenter);

        // --- DATE SECTION (Right) ---
        // Horizontal divider in date section
        double dateMidY = topBoxY + (topBoxHeight / 2);
        gfx.DrawLine(thinBorderPen, divider2X, dateMidY, Margin + contentWidth, dateMidY);

        // Date Reviewed (top)
        var generatedDate = report.GeneratedAt?.ToString("M/d/yyyy") ?? DateTime.UtcNow.ToString("M/d/yyyy");
        gfx.DrawString(
            "Date Reviewed",
            fontDateLabel,
            XBrushes.Black,
            new XRect(divider2X + 5, topBoxY + 5, 70, 12),
            XStringFormats.TopLeft);
        gfx.DrawString(
            generatedDate,
            fontDateValue,
            XBrushes.Black,
            new XRect(divider2X + 75, topBoxY + 5, dateSectionWidth - 80, 12),
            XStringFormats.TopRight);

        // Date Issued (bottom)
        var submittedDate = report.SubmittedAt?.ToString("M/d/yyyy") ?? generatedDate;
        gfx.DrawString(
            "Date Issued",
            fontDateLabel,
            XBrushes.Black,
            new XRect(divider2X + 5, dateMidY + 5, 70, 12),
            XStringFormats.TopLeft);
        gfx.DrawString(
            submittedDate,
            fontDateValue,
            XBrushes.Black,
            new XRect(divider2X + 75, dateMidY + 5, dateSectionWidth - 80, 12),
            XStringFormats.TopRight);

        y = topBoxY + topBoxHeight + 8;

        // ============================================
        // EMPLOYEE INFO ROW
        // ============================================
        double infoRowHeight = 20;

        // Get employee data with fallbacks - prefer user preferences, then user entity, then defaults
        var employeeId = userPrefs?.EmployeeId
            ?? report.User?.Id.ToString().Substring(0, 8).ToUpper()
            ?? "N/A";
        var employeeName = report.User?.DisplayName ?? "Unknown";
        var reportDate = report.Period; // Use period as the report date

        // Draw employee info row
        double col1Width = 180;
        double col2Width = 250;
        double col3Width = contentWidth - col1Width - col2Width;

        // Employee ID
        gfx.DrawString("Employee ID:", fontInfoLabel, XBrushes.Black,
            new XRect(Margin, y, 70, infoRowHeight), XStringFormats.CenterLeft);
        gfx.DrawString(employeeId, fontInfoValue, XBrushes.Black,
            new XRect(Margin + 70, y, col1Width - 75, infoRowHeight), XStringFormats.CenterLeft);
        // Underline
        gfx.DrawLine(thinBorderPen, Margin + 70, y + infoRowHeight - 2, Margin + col1Width - 10, y + infoRowHeight - 2);

        // Employee Name
        gfx.DrawString("Employee:", fontInfoLabel, XBrushes.Black,
            new XRect(Margin + col1Width, y, 60, infoRowHeight), XStringFormats.CenterLeft);
        gfx.DrawString(employeeName, fontInfoValue, XBrushes.Black,
            new XRect(Margin + col1Width + 60, y, col2Width - 70, infoRowHeight), XStringFormats.CenterLeft);
        // Underline
        gfx.DrawLine(thinBorderPen, Margin + col1Width + 60, y + infoRowHeight - 2, Margin + col1Width + col2Width - 10, y + infoRowHeight - 2);

        // Date
        gfx.DrawString("Date:", fontInfoLabel, XBrushes.Black,
            new XRect(Margin + col1Width + col2Width, y, 35, infoRowHeight), XStringFormats.CenterLeft);
        gfx.DrawString(reportDate, fontInfoValue, XBrushes.Black,
            new XRect(Margin + col1Width + col2Width + 35, y, col3Width - 40, infoRowHeight), XStringFormats.CenterLeft);
        // Underline
        gfx.DrawLine(thinBorderPen, Margin + col1Width + col2Width + 35, y + infoRowHeight - 2, Margin + contentWidth, y + infoRowHeight - 2);

        y += infoRowHeight + 8;

        // ============================================
        // DEPARTMENT ROW
        // ============================================
        // Get department and supervisor data - prefer user preferences, then existing fields, then defaults
        var departmentCode = userPrefs?.DepartmentName
            ?? report.User?.Department
            ?? lines.FirstOrDefault()?.DepartmentCode
            ?? "N/A";
        var supervisorName = userPrefs?.SupervisorName ?? _options.DefaultSupervisor;

        // Department
        gfx.DrawString("Department:", fontInfoLabel, XBrushes.Black,
            new XRect(Margin, y, 70, infoRowHeight), XStringFormats.CenterLeft);
        gfx.DrawString(departmentCode, fontInfoValue, XBrushes.Black,
            new XRect(Margin + 70, y, col1Width - 75, infoRowHeight), XStringFormats.CenterLeft);
        // Underline
        gfx.DrawLine(thinBorderPen, Margin + 70, y + infoRowHeight - 2, Margin + col1Width - 10, y + infoRowHeight - 2);

        // Supervisor
        gfx.DrawString("Supervisor:", fontInfoLabel, XBrushes.Black,
            new XRect(Margin + col1Width, y, 60, infoRowHeight), XStringFormats.CenterLeft);
        gfx.DrawString(supervisorName, fontInfoValue, XBrushes.Black,
            new XRect(Margin + col1Width + 60, y, col2Width - 70, infoRowHeight), XStringFormats.CenterLeft);
        // Underline
        gfx.DrawLine(thinBorderPen, Margin + col1Width + 60, y + infoRowHeight - 2, Margin + col1Width + col2Width - 10, y + infoRowHeight - 2);

        // Total
        gfx.DrawString("Total:", fontInfoLabel, XBrushes.Black,
            new XRect(Margin + col1Width + col2Width, y, 35, infoRowHeight), XStringFormats.CenterLeft);
        gfx.DrawString($"$ {totalAmount:N2}", fontInfoValue, XBrushes.Black,
            new XRect(Margin + col1Width + col2Width + 35, y, col3Width - 40, infoRowHeight), XStringFormats.CenterLeft);
        // Underline
        gfx.DrawLine(thinBorderPen, Margin + col1Width + col2Width + 35, y + infoRowHeight - 2, Margin + contentWidth, y + infoRowHeight - 2);

        y += infoRowHeight + 15;

        return y;
    }

    /// <summary>
    /// Data container for receipt grid rendering.
    /// Holds pre-loaded image data and metadata for each receipt cell.
    /// Supports combined transactions where multiple lines share the same receipt.
    /// </summary>
    private sealed class ReceiptGridItem
    {
        public required ExpenseLine Line { get; init; }
        public required ExpenseReport Report { get; init; }
        public byte[]? ImageData { get; set; }
        public bool IsPlaceholder { get; set; }
        public string? PlaceholderReason { get; set; }

        /// <summary>
        /// Line reference numbers this receipt covers.
        /// For combined transactions, contains multiple values (e.g., [1, 2, 3]).
        /// </summary>
        public List<int> LineRefs { get; init; } = new();

        /// <summary>
        /// Combined amount when receipt covers multiple transactions.
        /// </summary>
        public decimal CombinedAmount { get; set; }

        /// <summary>
        /// True if this receipt covers multiple transactions (combined).
        /// </summary>
        public bool IsCombined => LineRefs.Count > 1;

        /// <summary>
        /// Formats line references for display (e.g., "#1, #2, #3" or "#1-3" for consecutive).
        /// </summary>
        public string FormatLineRefs()
        {
            if (LineRefs.Count == 0) return "";
            if (LineRefs.Count == 1) return $"#{LineRefs[0]}";

            // Check if consecutive - if so, show as range
            var sorted = LineRefs.OrderBy(x => x).ToList();
            bool isConsecutive = true;
            for (int i = 1; i < sorted.Count; i++)
            {
                if (sorted[i] != sorted[i - 1] + 1)
                {
                    isConsecutive = false;
                    break;
                }
            }

            if (isConsecutive)
                return $"#{sorted.First()}-{sorted.Last()}";

            return string.Join(", ", sorted.Select(r => $"#{r}"));
        }
    }

    /// <summary>
    /// Pre-loads receipt image data for grid rendering.
    /// Returns null if image cannot be loaded (will show placeholder).
    /// </summary>
    private async Task<byte[]?> LoadReceiptImageDataAsync(ExpenseLine line, CancellationToken ct)
    {
        if (line.Receipt == null)
            return null;

        try
        {
            // For HTML/PDF receipts, use thumbnail if available
            if (IsHtmlContentType(line.Receipt.ContentType) ||
                string.Equals(line.Receipt.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrWhiteSpace(line.Receipt.ThumbnailUrl))
                {
                    using var thumbnailStream = await _blobService.DownloadAsync(line.Receipt.ThumbnailUrl);
                    using var thumbnailBuffer = new MemoryStream();
                    await thumbnailStream.CopyToAsync(thumbnailBuffer, ct);
                    return thumbnailBuffer.ToArray();
                }
                return null; // No thumbnail available
            }

            // For regular images, download from blob storage
            if (string.IsNullOrWhiteSpace(line.Receipt.BlobUrl))
                return null;

            using var imageStream = await _blobService.DownloadAsync(line.Receipt.BlobUrl);
            using var imageBuffer = new MemoryStream();
            await imageStream.CopyToAsync(imageBuffer, ct);
            var imageBytes = imageBuffer.ToArray();

            // Validate and convert if needed
            if (!ValidateImageDimensions(imageBytes, out _))
                return null;

            // Handle HEIC/HEIF format
            if (IsHeicFormat(line.Receipt.ContentType, line.Receipt.OriginalFilename))
            {
                imageBytes = await ConvertHeicToJpegAsync(imageBytes, ct);
            }

            return imageBytes;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load receipt image for line {LineId}", line.Id);
            return null;
        }
    }

    /// <summary>
    /// Gets placeholder reason text based on receipt type.
    /// </summary>
    private static string GetPlaceholderReason(ExpenseLine line)
    {
        if (line.Receipt == null)
            return "Missing";

        if (IsHtmlContentType(line.Receipt.ContentType))
            return "Email Receipt";

        if (string.Equals(line.Receipt.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            return "PDF Receipt";

        return "Unavailable";
    }

    /// <summary>
    /// Renders receipts in a grid layout (6 per page, 2 columns × 3 rows).
    /// </summary>
    private int AddReceiptGridPages(PdfDocument document, List<ReceiptGridItem> items, ExpenseReport report)
    {
        if (items.Count == 0)
            return 0;

        // Calculate cell dimensions
        var availableWidth = PageWidth - (2 * Margin);
        var availableHeight = PageHeight - (2 * Margin) - GridHeaderHeight;

        var cellWidth = (availableWidth - (GridGapX * (GridColumns - 1))) / GridColumns;
        var cellHeight = (availableHeight - (GridGapY * (GridRows - 1))) / GridRows;

        // Image area within each cell (leave room for header text)
        var cellHeaderHeight = 36.0;
        var imageWidth = cellWidth - 8; // 4pt padding each side
        var imageHeight = cellHeight - cellHeaderHeight - 8;

        int pageCount = 0;
        var totalPages = (int)Math.Ceiling((double)items.Count / ReceiptsPerPage);

        for (int pageNum = 0; pageNum < totalPages; pageNum++)
        {
            var page = document.AddPage();
            page.Width = PageWidth;
            page.Height = PageHeight;
            pageCount++;

            using var gfx = XGraphics.FromPdfPage(page);

            // Draw page header
            var fontHeader = new XFont(FontFamily, 11, XFontStyle.Bold);
            var fontSmall = new XFont(FontFamily, 8, XFontStyle.Regular);

            gfx.DrawString(
                $"Receipt Images - {report.Period}",
                fontHeader,
                XBrushes.Black,
                new XRect(Margin, Margin, availableWidth, 20),
                XStringFormats.TopLeft);

            gfx.DrawString(
                $"Page {pageNum + 1} of {totalPages}",
                fontSmall,
                XBrushes.Gray,
                new XRect(Margin, Margin, availableWidth, 20),
                XStringFormats.TopRight);

            // Get items for this page
            var pageItems = items.Skip(pageNum * ReceiptsPerPage).Take(ReceiptsPerPage).ToList();

            for (int i = 0; i < pageItems.Count; i++)
            {
                var item = pageItems[i];
                var col = i % GridColumns;
                var row = i / GridColumns;

                var cellX = Margin + (col * (cellWidth + GridGapX));
                var cellY = Margin + GridHeaderHeight + (row * (cellHeight + GridGapY));

                DrawReceiptCell(gfx, item, cellX, cellY, cellWidth, cellHeight, cellHeaderHeight, imageWidth, imageHeight);
            }

            // Draw page footer
            gfx.DrawString(
                "(Numbers correspond to expense line items in the itemized list)",
                fontSmall,
                XBrushes.Gray,
                new XRect(0, PageHeight - Margin + 5, PageWidth, 14),
                XStringFormats.TopCenter);
        }

        return pageCount;
    }

    /// <summary>
    /// Draws a single receipt cell in the grid layout.
    /// Supports combined transactions showing multiple line references.
    /// </summary>
    private void DrawReceiptCell(
        XGraphics gfx,
        ReceiptGridItem item,
        double cellX,
        double cellY,
        double cellWidth,
        double cellHeight,
        double headerHeight,
        double imageWidth,
        double imageHeight)
    {
        var fontRef = new XFont(FontFamily, item.IsCombined ? 9 : 11, XFontStyle.Bold);
        var fontInfo = new XFont(FontFamily, 7, XFontStyle.Regular);
        var fontCombined = new XFont(FontFamily, 7, XFontStyle.BoldItalic);
        var fontPlaceholder = new XFont(FontFamily, 9, XFontStyle.Italic);

        // Draw cell border - use different color for combined transactions
        var borderPen = item.IsCombined
            ? new XPen(XColors.DodgerBlue, 1.5)
            : new XPen(XColors.LightGray, 0.5);
        gfx.DrawRectangle(borderPen, cellX, cellY, cellWidth, cellHeight);

        // Draw header with line reference(s)
        var headerY = cellY + 4;

        // Line reference badge(s) - shows "#1-3" or "#1, #2, #3" for combined
        var lineRefText = item.FormatLineRefs();
        var lineRefWidth = item.IsCombined ? 60.0 : 30.0;
        gfx.DrawString(
            lineRefText,
            fontRef,
            item.IsPlaceholder ? XBrushes.DarkOrange : XBrushes.DarkGreen,
            new XRect(cellX + 4, headerY, lineRefWidth, 14),
            XStringFormats.TopLeft);

        // For combined transactions, show combined indicator and total amount
        if (item.IsCombined)
        {
            // Show "COMBINED" badge
            gfx.DrawString(
                $"({item.LineRefs.Count} items)",
                fontCombined,
                XBrushes.DodgerBlue,
                new XRect(cellX + lineRefWidth + 4, headerY + 1, 50, 12),
                XStringFormats.TopLeft);

            // Show combined amount (total of all transactions)
            var combinedInfo = $"Total: {item.CombinedAmount.ToString("C", UsCulture)}";
            gfx.DrawString(
                combinedInfo,
                fontInfo,
                XBrushes.DarkGray,
                new XRect(cellX + 4, headerY + 12, cellWidth - 8, 12),
                XStringFormats.TopLeft);

            // Vendor name on third line for combined
            var vendorName = SanitizeForPdf(item.Line.VendorName, 30);
            if (!string.IsNullOrEmpty(vendorName))
            {
                gfx.DrawString(
                    vendorName,
                    fontInfo,
                    XBrushes.Black,
                    new XRect(cellX + 4, headerY + 22, cellWidth - 8, 12),
                    XStringFormats.TopLeft);
            }
        }
        else
        {
            // Standard single transaction display
            // Date and amount on the same line
            var infoText = $"{item.Line.ExpenseDate:MM/dd/yy} • {item.Line.Amount.ToString("C", UsCulture)}";
            gfx.DrawString(
                infoText,
                fontInfo,
                XBrushes.DarkGray,
                new XRect(cellX + 36, headerY + 2, cellWidth - 44, 12),
                XStringFormats.TopLeft);

            // Vendor name on second line
            var vendorName = SanitizeForPdf(item.Line.VendorName, 30);
            if (!string.IsNullOrEmpty(vendorName))
            {
                gfx.DrawString(
                    vendorName,
                    fontInfo,
                    XBrushes.Black,
                    new XRect(cellX + 4, headerY + 14, cellWidth - 8, 12),
                    XStringFormats.TopLeft);
            }
        }

        // Image area starts below header
        var imageAreaX = cellX + 4;
        var imageAreaY = cellY + headerHeight;

        if (item.IsPlaceholder || item.ImageData == null)
        {
            // Draw placeholder
            DrawPlaceholderCell(gfx, item, imageAreaX, imageAreaY, imageWidth, imageHeight, fontPlaceholder);
        }
        else
        {
            // Draw the actual receipt image
            try
            {
                using var imageSource = ImageSharp3ImageSource.FromBytes(item.ImageData);
                var xImage = XImage.FromImageSource(imageSource);

                // Calculate scaling to fit within cell while maintaining aspect ratio
                var scale = Math.Min(imageWidth / xImage.PixelWidth, imageHeight / xImage.PixelHeight);
                var scaledWidth = xImage.PixelWidth * scale;
                var scaledHeight = xImage.PixelHeight * scale;

                // Center the image in the cell
                var imgX = imageAreaX + (imageWidth - scaledWidth) / 2;
                var imgY = imageAreaY + (imageHeight - scaledHeight) / 2;

                gfx.DrawImage(xImage, imgX, imgY, scaledWidth, scaledHeight);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to render receipt image for lines {LineRefs}", item.FormatLineRefs());
                DrawPlaceholderCell(gfx, item, imageAreaX, imageAreaY, imageWidth, imageHeight, fontPlaceholder);
            }
        }
    }

    /// <summary>
    /// Draws a placeholder rectangle when receipt image is unavailable.
    /// </summary>
    private static void DrawPlaceholderCell(
        XGraphics gfx,
        ReceiptGridItem item,
        double x,
        double y,
        double width,
        double height,
        XFont font)
    {
        // Draw dashed border for placeholder
        var pen = new XPen(XColors.LightGray, 1) { DashStyle = XDashStyle.Dash };
        gfx.DrawRectangle(pen, x, y, width, height);

        // Draw placeholder text
        var placeholderText = item.PlaceholderReason ?? "Unavailable";
        var brush = item.PlaceholderReason == "Missing" ? XBrushes.DarkOrange : XBrushes.Gray;

        gfx.DrawString(
            placeholderText,
            font,
            brush,
            new XRect(x, y + (height / 2) - 8, width, 16),
            XStringFormats.TopCenter);

        // If missing, show justification if available
        if (item.PlaceholderReason == "Missing" && item.Line.MissingReceiptJustification.HasValue)
        {
            var justificationText = GetJustificationText(item.Line.MissingReceiptJustification);
            var smallFont = new XFont(FontFamily, 6, XFontStyle.Regular);
            gfx.DrawString(
                justificationText,
                smallFont,
                XBrushes.Gray,
                new XRect(x, y + (height / 2) + 6, width, 12),
                XStringFormats.TopCenter);
        }
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
        /// Quality of 95 ensures crisp display at 600px+ resolution in PDF viewers.
        /// Images with transparency are composited onto a white background since PDFs
        /// often render transparent areas as black.
        /// </summary>
        public static ImageSharp3ImageSource FromBytes(byte[] imageBytes, int quality = 95)
        {
            using var stream = new MemoryStream(imageBytes);
            var image = Image.Load<Rgba32>(stream);

            // Check if image has any transparent pixels
            var hasTransparency = false;
            if (image.PixelType.AlphaRepresentation != null)
            {
                hasTransparency = HasActualTransparency(image);
            }

            // If image has transparency, composite onto white background
            // This prevents black backgrounds in PDF viewers which don't handle transparency well
            if (hasTransparency)
            {
                // Alpha-blend each pixel onto white background
                image.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        var row = accessor.GetRowSpan(y);
                        for (int x = 0; x < row.Length; x++)
                        {
                            ref var pixel = ref row[x];
                            if (pixel.A < 255)
                            {
                                // Blend with white: result = fg * alpha + white * (1 - alpha)
                                float alpha = pixel.A / 255f;
                                float invAlpha = 1f - alpha;
                                pixel.R = (byte)(pixel.R * alpha + 255 * invAlpha);
                                pixel.G = (byte)(pixel.G * alpha + 255 * invAlpha);
                                pixel.B = (byte)(pixel.B * alpha + 255 * invAlpha);
                                pixel.A = 255; // Fully opaque
                            }
                        }
                    }
                });
            }

            return new ImageSharp3ImageSource(
                Guid.NewGuid().ToString(),
                image,
                quality,
                false); // No longer transparent after compositing
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
