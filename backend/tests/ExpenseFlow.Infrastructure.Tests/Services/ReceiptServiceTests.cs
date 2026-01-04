using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Core.Services;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for ReceiptService.
/// Tests receipt update operations including optimistic concurrency.
/// </summary>
public class ReceiptServiceTests
{
    private readonly Mock<IReceiptRepository> _receiptRepositoryMock;
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
    private readonly Mock<IHeicConversionService> _heicConversionServiceMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<IExtractionCorrectionService> _correctionServiceMock;
    private readonly Mock<ILogger<ReceiptService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly ReceiptService _service;

    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testReceiptId = Guid.NewGuid();

    public ReceiptServiceTests()
    {
        _receiptRepositoryMock = new Mock<IReceiptRepository>();
        _blobStorageServiceMock = new Mock<IBlobStorageService>();
        _heicConversionServiceMock = new Mock<IHeicConversionService>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _correctionServiceMock = new Mock<IExtractionCorrectionService>();
        _loggerMock = new Mock<ILogger<ReceiptService>>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ReceiptProcessing:MaxFileSizeMB", "25" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _service = new ReceiptService(
            _receiptRepositoryMock.Object,
            _blobStorageServiceMock.Object,
            _heicConversionServiceMock.Object,
            _backgroundJobClientMock.Object,
            _correctionServiceMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    #region UpdateReceiptAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptAsync_WithValidRowVersion_UpdatesReceipt()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        receipt.RowVersion = 1; // Current version

        var request = new ReceiptUpdateRequestDto
        {
            Vendor = "Updated Vendor",
            Amount = 99.99m,
            RowVersion = 1 // Matches current
        };

        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(_testReceiptId, _testUserId))
            .ReturnsAsync(receipt);

        _receiptRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Receipt>()))
            .ReturnsAsync((Receipt r) => r);

        // Act
        var result = await _service.UpdateReceiptAsync(_testReceiptId, _testUserId, request);

        // Assert
        result.Should().NotBeNull();
        result!.VendorExtracted.Should().Be("Updated Vendor");
        result.AmountExtracted.Should().Be(99.99m);

        _receiptRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Receipt>()), Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptAsync_WithStaleRowVersion_ThrowsDbUpdateConcurrencyException()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        receipt.RowVersion = 2; // Current version is 2

        var request = new ReceiptUpdateRequestDto
        {
            Vendor = "Updated Vendor",
            RowVersion = 1 // Client has stale version 1
        };

        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(_testReceiptId, _testUserId))
            .ReturnsAsync(receipt);

        // Act
        var act = () => _service.UpdateReceiptAsync(_testReceiptId, _testUserId, request);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>()
            .WithMessage("*modified by another user*");

        _receiptRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Receipt>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptAsync_WithNullRowVersion_SkipsConcurrencyCheck()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        receipt.RowVersion = 5;

        var request = new ReceiptUpdateRequestDto
        {
            Vendor = "Updated Vendor",
            RowVersion = null // No concurrency check requested
        };

        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(_testReceiptId, _testUserId))
            .ReturnsAsync(receipt);

        _receiptRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Receipt>()))
            .ReturnsAsync((Receipt r) => r);

        // Act
        var result = await _service.UpdateReceiptAsync(_testReceiptId, _testUserId, request);

        // Assert
        result.Should().NotBeNull();
        result!.VendorExtracted.Should().Be("Updated Vendor");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptAsync_WithCorrections_RecordsTrainingFeedback()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        receipt.VendorExtracted = "Original Vendor";
        receipt.AmountExtracted = 50.00m;
        receipt.RowVersion = 1;

        var request = new ReceiptUpdateRequestDto
        {
            Vendor = "Corrected Vendor",
            Amount = 75.00m,
            RowVersion = 1,
            Corrections = new List<CorrectionMetadataDto>
            {
                new() { FieldName = "vendor", OriginalValue = "Original Vendor" },
                new() { FieldName = "amount", OriginalValue = "50.00" }
            }
        };

        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(_testReceiptId, _testUserId))
            .ReturnsAsync(receipt);

        _receiptRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Receipt>()))
            .ReturnsAsync((Receipt r) => r);

        // Act
        var result = await _service.UpdateReceiptAsync(_testReceiptId, _testUserId, request);

        // Assert
        result.Should().NotBeNull();

        _correctionServiceMock.Verify(
            c => c.RecordCorrectionsAsync(
                _testReceiptId,
                _testUserId,
                It.Is<IEnumerable<CorrectionMetadataDto>>(corrections => corrections.Count() == 2),
                It.IsAny<Dictionary<string, string?>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptAsync_PreventEditDuringProcessing_ReturnsNull()
    {
        // Arrange
        var receipt = CreateTestReceipt();
        receipt.Status = ReceiptStatus.Processing;

        var request = new ReceiptUpdateRequestDto
        {
            Vendor = "Should Not Update"
        };

        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(_testReceiptId, _testUserId))
            .ReturnsAsync(receipt);

        // Act
        var result = await _service.UpdateReceiptAsync(_testReceiptId, _testUserId, request);

        // Assert
        result.Should().BeNull();

        _receiptRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Receipt>()), Times.Never);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptAsync_NotFound_ReturnsNull()
    {
        // Arrange
        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(_testReceiptId, _testUserId))
            .ReturnsAsync((Receipt?)null);

        var request = new ReceiptUpdateRequestDto { Vendor = "Test" };

        // Act
        var result = await _service.UpdateReceiptAsync(_testReceiptId, _testUserId, request);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private Receipt CreateTestReceipt()
    {
        return new Receipt
        {
            Id = _testReceiptId,
            UserId = _testUserId,
            BlobUrl = "https://storage.blob.core.windows.net/receipts/test.jpg",
            OriginalFilename = "test.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            Status = ReceiptStatus.Processed,
            VendorExtracted = "Test Vendor",
            AmountExtracted = 100.00m,
            DateExtracted = DateOnly.FromDateTime(DateTime.Today),
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
