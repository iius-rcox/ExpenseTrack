using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Configuration;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for PdfGenerationService.
/// Tests PDF generation including receipt image embedding and placeholder behavior.
/// </summary>
public class PdfGenerationServiceTests
{
    private readonly Mock<IExpenseReportRepository> _reportRepositoryMock;
    private readonly Mock<IBlobStorageService> _blobServiceMock;
    private readonly Mock<IUserPreferencesService> _preferencesServiceMock;
    private readonly Mock<ILogger<PdfGenerationService>> _loggerMock;
    private readonly IOptions<ExportOptions> _options;
    private readonly PdfGenerationService _service;

    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testReportId = Guid.NewGuid();

    public PdfGenerationServiceTests()
    {
        _reportRepositoryMock = new Mock<IExpenseReportRepository>();
        _blobServiceMock = new Mock<IBlobStorageService>();
        _preferencesServiceMock = new Mock<IUserPreferencesService>();
        _loggerMock = new Mock<ILogger<PdfGenerationService>>();

        _options = Options.Create(new ExportOptions
        {
            MaxReceiptsPerPdf = 100
        });

        _service = new PdfGenerationService(
            _reportRepositoryMock.Object,
            _blobServiceMock.Object,
            _preferencesServiceMock.Object,
            _options,
            _loggerMock.Object);
    }

    #region PDF Receipt Thumbnail Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GenerateReceiptPdfAsync_PdfReceiptWithThumbnail_EmbedsThumbnailImage()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var thumbnailUrl = "https://storage.example.com/thumbnails/test-thumbnail.jpg";

        var report = CreateReportWithPdfReceipt(receiptId, thumbnailUrl: thumbnailUrl);
        SetupRepositoryToReturn(report);
        SetupBlobServiceForThumbnail(thumbnailUrl);

        // Act
        var result = await _service.GenerateReceiptPdfAsync(_testReportId);

        // Assert
        result.Should().NotBeNull();
        result.PageCount.Should().Be(1);
        result.PlaceholderCount.Should().Be(0, "PDF receipt with thumbnail should not be a placeholder");

        // Verify thumbnail was downloaded (not the PDF blob)
        _blobServiceMock.Verify(
            x => x.DownloadAsync(thumbnailUrl),
            Times.Once,
            "Should download the thumbnail image for PDF receipt");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GenerateReceiptPdfAsync_PdfReceiptWithoutThumbnail_ShowsPlaceholder()
    {
        // Arrange
        var receiptId = Guid.NewGuid();

        var report = CreateReportWithPdfReceipt(receiptId, thumbnailUrl: null);
        SetupRepositoryToReturn(report);

        // Act
        var result = await _service.GenerateReceiptPdfAsync(_testReportId);

        // Assert
        result.Should().NotBeNull();
        result.PageCount.Should().Be(1);
        result.PlaceholderCount.Should().Be(0, "PDF with placeholder is not a 'missing' receipt");

        // Verify the PDF blob was NOT downloaded (placeholder shown instead)
        _blobServiceMock.Verify(
            x => x.DownloadAsync(It.IsAny<string>()),
            Times.Never,
            "Should not attempt to download the PDF when showing placeholder");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GenerateReceiptPdfAsync_HtmlReceiptWithThumbnail_EmbedsThumbnailImage()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var thumbnailUrl = "https://storage.example.com/thumbnails/html-thumbnail.jpg";

        var report = CreateReportWithHtmlReceipt(receiptId, thumbnailUrl: thumbnailUrl);
        SetupRepositoryToReturn(report);
        SetupBlobServiceForThumbnail(thumbnailUrl);

        // Act
        var result = await _service.GenerateReceiptPdfAsync(_testReportId);

        // Assert
        result.Should().NotBeNull();
        result.PageCount.Should().Be(1);
        result.PlaceholderCount.Should().Be(0, "HTML receipt with thumbnail should not be a placeholder");

        // Verify thumbnail was downloaded
        _blobServiceMock.Verify(
            x => x.DownloadAsync(thumbnailUrl),
            Times.Once,
            "Should download the thumbnail image for HTML receipt");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GenerateReceiptPdfAsync_HtmlReceiptWithoutThumbnail_ShowsPlaceholder()
    {
        // Arrange
        var receiptId = Guid.NewGuid();

        var report = CreateReportWithHtmlReceipt(receiptId, thumbnailUrl: null);
        SetupRepositoryToReturn(report);

        // Act
        var result = await _service.GenerateReceiptPdfAsync(_testReportId);

        // Assert
        result.Should().NotBeNull();
        result.PageCount.Should().Be(1);
        result.PlaceholderCount.Should().Be(0, "HTML with placeholder is not a 'missing' receipt");

        // Verify no blob download attempts
        _blobServiceMock.Verify(
            x => x.DownloadAsync(It.IsAny<string>()),
            Times.Never,
            "Should not attempt to download when showing placeholder");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GenerateReceiptPdfAsync_PdfReceiptThumbnailDownloadFails_ShowsPlaceholder()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var thumbnailUrl = "https://storage.example.com/thumbnails/missing.jpg";

        var report = CreateReportWithPdfReceipt(receiptId, thumbnailUrl: thumbnailUrl);
        SetupRepositoryToReturn(report);

        // Setup thumbnail download to fail
        _blobServiceMock
            .Setup(x => x.DownloadAsync(thumbnailUrl))
            .ThrowsAsync(new FileNotFoundException("Thumbnail not found"));

        // Act
        var result = await _service.GenerateReceiptPdfAsync(_testReportId);

        // Assert
        result.Should().NotBeNull();
        result.PageCount.Should().Be(1, "Should still generate a page even if thumbnail fails");

        // Verify thumbnail download was attempted
        _blobServiceMock.Verify(
            x => x.DownloadAsync(thumbnailUrl),
            Times.Once,
            "Should attempt to download the thumbnail");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GenerateCompleteReportPdfAsync_MixedReceiptTypes_HandlesAllCorrectly()
    {
        // Arrange
        var imageReceiptId = Guid.NewGuid();
        var pdfReceiptWithThumbId = Guid.NewGuid();
        var pdfReceiptNoThumbId = Guid.NewGuid();
        var htmlReceiptWithThumbId = Guid.NewGuid();
        var imageBlobUrl = "https://storage.example.com/receipts/image.jpg";
        var pdfThumbnailUrl = "https://storage.example.com/thumbnails/pdf-thumb.jpg";
        var htmlThumbnailUrl = "https://storage.example.com/thumbnails/html-thumb.jpg";

        var report = CreateReportWithMixedReceipts(
            imageReceiptId, imageBlobUrl,
            pdfReceiptWithThumbId, pdfThumbnailUrl,
            pdfReceiptNoThumbId,
            htmlReceiptWithThumbId, htmlThumbnailUrl);

        SetupRepositoryToReturn(report);
        SetupBlobServiceForImage(imageBlobUrl);
        SetupBlobServiceForThumbnail(pdfThumbnailUrl);
        SetupBlobServiceForThumbnail(htmlThumbnailUrl);

        // Act
        var result = await _service.GenerateCompleteReportPdfAsync(_testReportId);

        // Assert
        result.Should().NotBeNull();
        // 1 summary page + 4 receipt pages (image, pdf with thumb, pdf placeholder, html with thumb)
        result.PageCount.Should().BeGreaterOrEqualTo(5);

        // Verify correct downloads
        _blobServiceMock.Verify(x => x.DownloadAsync(imageBlobUrl), Times.Once);
        _blobServiceMock.Verify(x => x.DownloadAsync(pdfThumbnailUrl), Times.Once);
        _blobServiceMock.Verify(x => x.DownloadAsync(htmlThumbnailUrl), Times.Once);
    }

    #endregion

    #region Helper Methods

    private ExpenseReport CreateReportWithPdfReceipt(Guid receiptId, string? thumbnailUrl)
    {
        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = _testUserId,
            BlobUrl = "https://storage.example.com/receipts/document.pdf",
            ThumbnailUrl = thumbnailUrl,
            ContentType = "application/pdf",
            OriginalFilename = "invoice.pdf",
            Status = ReceiptStatus.Ready
        };

        var line = new ExpenseLine
        {
            Id = Guid.NewGuid(),
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = 99.99m,
            VendorName = "Test Vendor",
            NormalizedDescription = "Test expense",
            HasReceipt = true,
            ReceiptId = receiptId,
            Receipt = receipt,
            LineOrder = 1
        };

        return new ExpenseReport
        {
            Id = _testReportId,
            UserId = _testUserId,
            Period = "2025-01",
            Status = ReportStatus.Draft,
            User = new User { Id = _testUserId, DisplayName = "Test User", Email = "test@example.com" },
            Lines = new List<ExpenseLine> { line }
        };
    }

    private ExpenseReport CreateReportWithHtmlReceipt(Guid receiptId, string? thumbnailUrl)
    {
        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = _testUserId,
            BlobUrl = "https://storage.example.com/receipts/email.html",
            ThumbnailUrl = thumbnailUrl,
            ContentType = "text/html",
            OriginalFilename = "receipt.html",
            Status = ReceiptStatus.Ready
        };

        var line = new ExpenseLine
        {
            Id = Guid.NewGuid(),
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Amount = 49.99m,
            VendorName = "Email Vendor",
            NormalizedDescription = "Email receipt",
            HasReceipt = true,
            ReceiptId = receiptId,
            Receipt = receipt,
            LineOrder = 1
        };

        return new ExpenseReport
        {
            Id = _testReportId,
            UserId = _testUserId,
            Period = "2025-01",
            Status = ReportStatus.Draft,
            User = new User { Id = _testUserId, DisplayName = "Test User", Email = "test@example.com" },
            Lines = new List<ExpenseLine> { line }
        };
    }

    private ExpenseReport CreateReportWithMixedReceipts(
        Guid imageReceiptId, string imageBlobUrl,
        Guid pdfReceiptWithThumbId, string pdfThumbnailUrl,
        Guid pdfReceiptNoThumbId,
        Guid htmlReceiptWithThumbId, string htmlThumbnailUrl)
    {
        var imageReceipt = new Receipt
        {
            Id = imageReceiptId,
            UserId = _testUserId,
            BlobUrl = imageBlobUrl,
            ContentType = "image/jpeg",
            OriginalFilename = "receipt.jpg",
            Status = ReceiptStatus.Ready
        };

        var pdfReceiptWithThumb = new Receipt
        {
            Id = pdfReceiptWithThumbId,
            UserId = _testUserId,
            BlobUrl = "https://storage.example.com/receipts/invoice.pdf",
            ThumbnailUrl = pdfThumbnailUrl,
            ContentType = "application/pdf",
            OriginalFilename = "invoice.pdf",
            Status = ReceiptStatus.Ready
        };

        var pdfReceiptNoThumb = new Receipt
        {
            Id = pdfReceiptNoThumbId,
            UserId = _testUserId,
            BlobUrl = "https://storage.example.com/receipts/old-invoice.pdf",
            ThumbnailUrl = null,
            ContentType = "application/pdf",
            OriginalFilename = "old-invoice.pdf",
            Status = ReceiptStatus.Ready
        };

        var htmlReceiptWithThumb = new Receipt
        {
            Id = htmlReceiptWithThumbId,
            UserId = _testUserId,
            BlobUrl = "https://storage.example.com/receipts/email.html",
            ThumbnailUrl = htmlThumbnailUrl,
            ContentType = "text/html",
            OriginalFilename = "email.html",
            Status = ReceiptStatus.Ready
        };

        var lines = new List<ExpenseLine>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = 25.00m,
                VendorName = "Image Vendor",
                NormalizedDescription = "Image receipt",
                HasReceipt = true,
                ReceiptId = imageReceiptId,
                Receipt = imageReceipt,
                LineOrder = 1
            },
            new()
            {
                Id = Guid.NewGuid(),
                ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = 150.00m,
                VendorName = "PDF Vendor",
                NormalizedDescription = "PDF with thumbnail",
                HasReceipt = true,
                ReceiptId = pdfReceiptWithThumbId,
                Receipt = pdfReceiptWithThumb,
                LineOrder = 2
            },
            new()
            {
                Id = Guid.NewGuid(),
                ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = 75.00m,
                VendorName = "Old PDF Vendor",
                NormalizedDescription = "PDF without thumbnail",
                HasReceipt = true,
                ReceiptId = pdfReceiptNoThumbId,
                Receipt = pdfReceiptNoThumb,
                LineOrder = 3
            },
            new()
            {
                Id = Guid.NewGuid(),
                ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Amount = 50.00m,
                VendorName = "HTML Vendor",
                NormalizedDescription = "HTML receipt",
                HasReceipt = true,
                ReceiptId = htmlReceiptWithThumbId,
                Receipt = htmlReceiptWithThumb,
                LineOrder = 4
            }
        };

        return new ExpenseReport
        {
            Id = _testReportId,
            UserId = _testUserId,
            Period = "2025-01",
            Status = ReportStatus.Draft,
            User = new User { Id = _testUserId, DisplayName = "Test User", Email = "test@example.com" },
            Lines = lines
        };
    }

    private void SetupRepositoryToReturn(ExpenseReport report)
    {
        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(_testReportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
    }

    private void SetupBlobServiceForThumbnail(string thumbnailUrl)
    {
        var jpegBytes = CreateMinimalJpeg();
        _blobServiceMock
            .Setup(x => x.DownloadAsync(thumbnailUrl))
            .ReturnsAsync(() => new MemoryStream(jpegBytes));
    }

    private void SetupBlobServiceForImage(string blobUrl)
    {
        var jpegBytes = CreateMinimalJpeg();
        _blobServiceMock
            .Setup(x => x.DownloadAsync(blobUrl))
            .ReturnsAsync(() => new MemoryStream(jpegBytes));
    }

    /// <summary>
    /// Creates a minimal valid JPEG image (1x1 pixel).
    /// </summary>
    private static byte[] CreateMinimalJpeg()
    {
        // Minimal 1x1 red JPEG created with ImageMagick
        return new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
            0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
            0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
            0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
            0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
            0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
            0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
            0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00,
            0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03,
            0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00, 0x01, 0x7D,
            0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06,
            0x13, 0x51, 0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
            0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0, 0x24, 0x33, 0x62, 0x72,
            0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2A, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x43, 0x44, 0x45,
            0x46, 0x47, 0x48, 0x49, 0x4A, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59,
            0x5A, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6A, 0x73, 0x74, 0x75,
            0x76, 0x77, 0x78, 0x79, 0x7A, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89,
            0x8A, 0x92, 0x93, 0x94, 0x95, 0x96, 0x97, 0x98, 0x99, 0x9A, 0xA2, 0xA3,
            0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xB2, 0xB3, 0xB4, 0xB5, 0xB6,
            0xB7, 0xB8, 0xB9, 0xBA, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9,
            0xCA, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xE1, 0xE2,
            0xE3, 0xE4, 0xE5, 0xE6, 0xE7, 0xE8, 0xE9, 0xEA, 0xF1, 0xF2, 0xF3, 0xF4,
            0xF5, 0xF6, 0xF7, 0xF8, 0xF9, 0xFA, 0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01,
            0x00, 0x00, 0x3F, 0x00, 0xFB, 0xD5, 0xDB, 0x20, 0x20, 0x40, 0xFF, 0xD9
        };
    }

    #endregion
}
