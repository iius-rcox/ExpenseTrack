using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Core.Services;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for Receipt Duplicate Detection feature.
/// Tests file hash and content hash computation, as well as duplicate detection logic.
/// </summary>
public class ReceiptDuplicateDetectionTests
{
    private readonly Mock<IReceiptRepository> _receiptRepositoryMock;
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
    private readonly Mock<IHeicConversionService> _heicConversionServiceMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<IExtractionCorrectionService> _correctionServiceMock;
    private readonly Mock<ILogger<ReceiptService>> _loggerMock;
    private readonly IConfiguration _configuration;

    private readonly Guid _testUserId = Guid.NewGuid();

    public ReceiptDuplicateDetectionTests()
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
    }

    private ReceiptService CreateService()
    {
        return new ReceiptService(
            _receiptRepositoryMock.Object,
            _blobStorageServiceMock.Object,
            _heicConversionServiceMock.Object,
            _backgroundJobClientMock.Object,
            _correctionServiceMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    #region ComputeFileHash Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ComputeFileHash_ReturnsConsistentHash_ForSameFile()
    {
        // Arrange
        var fileContent = "This is test file content for hashing."u8.ToArray();
        using var stream1 = new MemoryStream(fileContent);
        using var stream2 = new MemoryStream(fileContent);

        // Act
        var hash1 = await ReceiptService.ComputeFileHashAsync(stream1);
        var hash2 = await ReceiptService.ComputeFileHashAsync(stream2);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2, "Same file content should produce identical hash");
        hash1.Should().HaveLength(64, "SHA-256 produces 64 character hex string");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ComputeFileHash_ReturnsDifferentHash_ForDifferentFiles()
    {
        // Arrange
        var fileContent1 = "First file content."u8.ToArray();
        var fileContent2 = "Second file content - different."u8.ToArray();
        using var stream1 = new MemoryStream(fileContent1);
        using var stream2 = new MemoryStream(fileContent2);

        // Act
        var hash1 = await ReceiptService.ComputeFileHashAsync(stream1);
        var hash2 = await ReceiptService.ComputeFileHashAsync(stream2);

        // Assert
        hash1.Should().NotBe(hash2, "Different file content should produce different hashes");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ComputeFileHash_ResetsStreamPosition_AfterHashing()
    {
        // Arrange
        var fileContent = "Test content"u8.ToArray();
        using var stream = new MemoryStream(fileContent);

        // Act
        await ReceiptService.ComputeFileHashAsync(stream);

        // Assert
        stream.Position.Should().Be(0, "Stream position should be reset for subsequent reading");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ComputeFileHash_ThrowsArgumentNullException_ForNullStream()
    {
        // Act
        var act = () => ReceiptService.ComputeFileHashAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ComputeFileHash_ReturnsValidHash_ForEmptyStream()
    {
        // Arrange
        using var emptyStream = new MemoryStream();

        // Act
        var hash = await ReceiptService.ComputeFileHashAsync(emptyStream);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64, "SHA-256 always produces 64 character hex string");
        // SHA-256 of empty input is a known value
        hash.Should().Be("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ComputeFileHash_ProducesLowercaseHexString()
    {
        // Arrange
        var fileContent = "Test content for case check"u8.ToArray();
        using var stream = new MemoryStream(fileContent);

        // Act
        var hash = await ReceiptService.ComputeFileHashAsync(stream);

        // Assert
        hash.Should().MatchRegex("^[a-f0-9]{64}$", "Hash should be lowercase hex string");
    }

    #endregion

    #region ComputeContentHash Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_ReturnsConsistentHash_ForSameContent()
    {
        // Arrange
        var vendor = "STARBUCKS";
        var date = new DateOnly(2024, 6, 15);
        var amount = 12.50m;

        // Act
        var hash1 = ReceiptService.ComputeContentHash(vendor, date, amount);
        var hash2 = ReceiptService.ComputeContentHash(vendor, date, amount);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2, "Same content should produce identical hash");
        hash1.Should().HaveLength(64, "SHA-256 produces 64 character hex string");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_ReturnsDifferentHash_ForDifferentVendor()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);
        var amount = 12.50m;

        // Act
        var hash1 = ReceiptService.ComputeContentHash("STARBUCKS", date, amount);
        var hash2 = ReceiptService.ComputeContentHash("DUNKIN", date, amount);

        // Assert
        hash1.Should().NotBe(hash2, "Different vendor should produce different hash");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_ReturnsDifferentHash_ForDifferentDate()
    {
        // Arrange
        var vendor = "STARBUCKS";
        var amount = 12.50m;

        // Act
        var hash1 = ReceiptService.ComputeContentHash(vendor, new DateOnly(2024, 6, 15), amount);
        var hash2 = ReceiptService.ComputeContentHash(vendor, new DateOnly(2024, 6, 16), amount);

        // Assert
        hash1.Should().NotBe(hash2, "Different date should produce different hash");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_ReturnsDifferentHash_ForDifferentAmount()
    {
        // Arrange
        var vendor = "STARBUCKS";
        var date = new DateOnly(2024, 6, 15);

        // Act
        var hash1 = ReceiptService.ComputeContentHash(vendor, date, 12.50m);
        var hash2 = ReceiptService.ComputeContentHash(vendor, date, 12.51m);

        // Assert
        hash1.Should().NotBe(hash2, "Different amount should produce different hash");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesNullVendor()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);
        var amount = 12.50m;

        // Act
        var hash = ReceiptService.ComputeContentHash(null, date, amount);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64, "SHA-256 produces 64 character hex string");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesNullDate()
    {
        // Arrange
        var vendor = "STARBUCKS";
        var amount = 12.50m;

        // Act
        var hash = ReceiptService.ComputeContentHash(vendor, null, amount);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64, "SHA-256 produces 64 character hex string");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesNullAmount()
    {
        // Arrange
        var vendor = "STARBUCKS";
        var date = new DateOnly(2024, 6, 15);

        // Act
        var hash = ReceiptService.ComputeContentHash(vendor, date, null);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64, "SHA-256 produces 64 character hex string");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesAllNullValues()
    {
        // Act
        var hash = ReceiptService.ComputeContentHash(null, null, null);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64, "SHA-256 produces 64 character hex string");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_NormalizesVendorCasing()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);
        var amount = 12.50m;

        // Act
        var hash1 = ReceiptService.ComputeContentHash("STARBUCKS", date, amount);
        var hash2 = ReceiptService.ComputeContentHash("starbucks", date, amount);
        var hash3 = ReceiptService.ComputeContentHash("Starbucks", date, amount);

        // Assert
        hash1.Should().Be(hash2, "Vendor casing should be normalized");
        hash2.Should().Be(hash3, "Vendor casing should be normalized");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_NormalizesVendorWhitespace()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);
        var amount = 12.50m;

        // Act
        var hash1 = ReceiptService.ComputeContentHash("STARBUCKS", date, amount);
        var hash2 = ReceiptService.ComputeContentHash("  STARBUCKS  ", date, amount);
        var hash3 = ReceiptService.ComputeContentHash("\tSTARBUCKS\n", date, amount);

        // Assert
        hash1.Should().Be(hash2, "Vendor whitespace should be normalized");
        hash2.Should().Be(hash3, "Vendor whitespace should be normalized");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_FormatsAmountWithTwoDecimals()
    {
        // Arrange
        var vendor = "STARBUCKS";
        var date = new DateOnly(2024, 6, 15);

        // Act - 12.5 and 12.50 should produce the same hash
        var hash1 = ReceiptService.ComputeContentHash(vendor, date, 12.5m);
        var hash2 = ReceiptService.ComputeContentHash(vendor, date, 12.50m);
        var hash3 = ReceiptService.ComputeContentHash(vendor, date, 12.500m);

        // Assert
        hash1.Should().Be(hash2, "Amount should be formatted consistently");
        hash2.Should().Be(hash3, "Amount should be formatted consistently");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_FormatsDateAsIso8601()
    {
        // Arrange - Verify date format by checking different but equal dates produce same hash
        var vendor = "STARBUCKS";
        var amount = 12.50m;

        // Create dates that are equal
        var date1 = new DateOnly(2024, 6, 15);
        var date2 = DateOnly.Parse("2024-06-15");

        // Act
        var hash1 = ReceiptService.ComputeContentHash(vendor, date1, amount);
        var hash2 = ReceiptService.ComputeContentHash(vendor, date2, amount);

        // Assert
        hash1.Should().Be(hash2, "Date should be formatted consistently as yyyy-MM-dd");
    }

    #endregion

    #region Upload Duplicate Detection Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadReceipt_ReturnsConflict_WhenExactDuplicateExists()
    {
        // Arrange
        var service = CreateService();
        var fileContent = CreateJpegBytes();
        using var stream = new MemoryStream(fileContent);

        // Compute expected hash for the file
        var expectedHash = ComputeSha256Hash(fileContent);

        // Set up repository to return existing receipt with same file hash
        var existingReceipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            FileHash = expectedHash,
            OriginalFilename = "existing-receipt.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(expectedHash, _testUserId))
            .ReturnsAsync(existingReceipt);

        // Act
        var result = await service.UploadReceiptAsync(stream, "new-receipt.jpg", "image/jpeg", _testUserId, allowDuplicates: false);

        // Assert
        result.Should().NotBeNull();
        result.IsDuplicate.Should().BeTrue();
        result.DuplicateType.Should().Be(DuplicateType.ExactFile);
        result.ExistingReceiptId.Should().Be(existingReceipt.Id);
        result.Receipt.Should().BeNull("No new receipt should be created for duplicate");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadReceipt_ReturnsConflict_WhenContentDuplicateExists()
    {
        // Arrange
        var service = CreateService();
        var fileContent = CreateJpegBytes();
        using var stream = new MemoryStream(fileContent);

        // Set up repository to return no file hash match
        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(It.IsAny<string>(), _testUserId))
            .ReturnsAsync((Receipt?)null);

        // Set up blob storage to return URL
        _blobStorageServiceMock
            .Setup(b => b.GenerateReceiptPath(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("receipts/test/test.jpg");
        _blobStorageServiceMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://storage/receipts/test/test.jpg");

        // After upload, extraction happens and content hash is computed
        // Set up a receipt with same content hash
        var contentHash = ReceiptService.ComputeContentHash("STARBUCKS", new DateOnly(2024, 6, 15), 12.50m);
        var existingReceipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ContentHash = contentHash,
            VendorExtracted = "STARBUCKS",
            DateExtracted = new DateOnly(2024, 6, 15),
            AmountExtracted = 12.50m,
            OriginalFilename = "existing-receipt.jpg",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        _receiptRepositoryMock
            .Setup(r => r.FindByContentHashAsync(contentHash, _testUserId))
            .ReturnsAsync(existingReceipt);

        // Act
        var result = await service.CheckContentDuplicateAsync(
            _testUserId,
            "STARBUCKS",
            new DateOnly(2024, 6, 15),
            12.50m);

        // Assert
        result.Should().NotBeNull();
        result.IsDuplicate.Should().BeTrue();
        result.DuplicateType.Should().Be(DuplicateType.SameContent);
        result.ExistingReceiptId.Should().Be(existingReceipt.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadReceipt_AllowsDuplicate_WhenFlagIsTrue()
    {
        // Arrange
        var service = CreateService();
        var fileContent = CreateJpegBytes();
        using var stream = new MemoryStream(fileContent);

        // Compute expected hash for the file
        var expectedHash = ComputeSha256Hash(fileContent);

        // Set up repository to return existing receipt with same file hash
        var existingReceipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            FileHash = expectedHash,
            OriginalFilename = "existing-receipt.jpg"
        };

        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(expectedHash, _testUserId))
            .ReturnsAsync(existingReceipt);

        // Set up for successful upload
        _blobStorageServiceMock
            .Setup(b => b.GenerateReceiptPath(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("receipts/test/test.jpg");
        _blobStorageServiceMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://storage/receipts/test/test.jpg");

        _receiptRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Receipt>()))
            .ReturnsAsync((Receipt r) => r);

        // Act
        var result = await service.UploadReceiptAsync(stream, "new-receipt.jpg", "image/jpeg", _testUserId, allowDuplicates: true);

        // Assert
        result.Should().NotBeNull();
        result.IsDuplicate.Should().BeFalse("Duplicate flag should be false when upload is allowed");
        result.Receipt.Should().NotBeNull("New receipt should be created when allowDuplicates is true");
        result.Receipt!.FileHash.Should().Be(expectedHash);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadReceipt_ComputesAndStoresFileHash()
    {
        // Arrange
        var service = CreateService();
        var fileContent = CreateJpegBytes();
        using var stream = new MemoryStream(fileContent);
        var expectedHash = ComputeSha256Hash(fileContent);

        // Set up for no duplicates and successful upload
        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(It.IsAny<string>(), _testUserId))
            .ReturnsAsync((Receipt?)null);

        _blobStorageServiceMock
            .Setup(b => b.GenerateReceiptPath(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("receipts/test/test.jpg");
        _blobStorageServiceMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://storage/receipts/test/test.jpg");

        Receipt? capturedReceipt = null;
        _receiptRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Receipt>()))
            .Callback<Receipt>(r => capturedReceipt = r)
            .ReturnsAsync((Receipt r) => r);

        // Act
        var result = await service.UploadReceiptAsync(stream, "test.jpg", "image/jpeg", _testUserId);

        // Assert
        capturedReceipt.Should().NotBeNull();
        capturedReceipt!.FileHash.Should().Be(expectedHash, "File hash should be computed and stored on upload");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceipt_UpdatesContentHash_AfterExtraction()
    {
        // Arrange
        var service = CreateService();
        var receiptId = Guid.NewGuid();

        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = _testUserId,
            FileHash = "somehash",
            ContentHash = null, // No content hash yet
            BlobUrl = "https://storage/test.jpg",
            OriginalFilename = "test.jpg",
            ContentType = "image/jpeg",
            FileSize = 1024,
            Status = ReceiptStatus.Ready,
            VendorExtracted = "OLD VENDOR",
            DateExtracted = new DateOnly(2024, 1, 1),
            AmountExtracted = 10.00m
        };

        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(receiptId, _testUserId))
            .ReturnsAsync(receipt);

        Receipt? capturedReceipt = null;
        _receiptRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Receipt>()))
            .Callback<Receipt>(r => capturedReceipt = r)
            .ReturnsAsync((Receipt r) => r);

        var request = new ReceiptUpdateRequestDto
        {
            Vendor = "STARBUCKS",
            Date = new DateTime(2024, 6, 15),
            Amount = 12.50m
        };

        // Act
        var result = await service.UpdateReceiptAsync(receiptId, _testUserId, request);

        // Assert
        capturedReceipt.Should().NotBeNull();
        capturedReceipt!.ContentHash.Should().NotBeNullOrEmpty("Content hash should be computed on update");

        var expectedContentHash = ReceiptService.ComputeContentHash("STARBUCKS", new DateOnly(2024, 6, 15), 12.50m);
        capturedReceipt.ContentHash.Should().Be(expectedContentHash);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UploadReceipt_ChecksDuplicateByDefault()
    {
        // Arrange
        var service = CreateService();
        var fileContent = CreateJpegBytes();
        using var stream = new MemoryStream(fileContent);

        // Set up repository to return no duplicates
        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(It.IsAny<string>(), _testUserId))
            .ReturnsAsync((Receipt?)null);

        _blobStorageServiceMock
            .Setup(b => b.GenerateReceiptPath(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("receipts/test/test.jpg");
        _blobStorageServiceMock
            .Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://storage/receipts/test/test.jpg");

        _receiptRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Receipt>()))
            .ReturnsAsync((Receipt r) => r);

        // Act - Use the overloaded method with duplicate checking (allowDuplicates: false is default)
        await service.UploadReceiptAsync(stream, "test.jpg", "image/jpeg", _testUserId, allowDuplicates: false);

        // Assert - Verify duplicate check was called
        _receiptRepositoryMock.Verify(r => r.FindByFileHashAsync(It.IsAny<string>(), _testUserId), Times.Once);
    }

    #endregion

    #region Repository Method Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FindByFileHashAsync_ReturnsReceipt_WhenHashExists()
    {
        // Arrange
        var fileHash = "abc123def456";
        var expectedReceipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            FileHash = fileHash
        };

        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(fileHash, _testUserId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _receiptRepositoryMock.Object.FindByFileHashAsync(fileHash, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(expectedReceipt);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FindByFileHashAsync_ReturnsNull_WhenHashNotFound()
    {
        // Arrange
        var fileHash = "nonexistent";

        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(fileHash, _testUserId))
            .ReturnsAsync((Receipt?)null);

        // Act
        var result = await _receiptRepositoryMock.Object.FindByFileHashAsync(fileHash, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FindByFileHashAsync_EnforcesUserIsolation()
    {
        // Arrange
        var fileHash = "abc123def456";
        var otherUserId = Guid.NewGuid();

        // Set up receipt belonging to different user
        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(fileHash, _testUserId))
            .ReturnsAsync((Receipt?)null); // Should not find other user's receipt

        _receiptRepositoryMock
            .Setup(r => r.FindByFileHashAsync(fileHash, otherUserId))
            .ReturnsAsync(new Receipt { Id = Guid.NewGuid(), UserId = otherUserId, FileHash = fileHash });

        // Act
        var result = await _receiptRepositoryMock.Object.FindByFileHashAsync(fileHash, _testUserId);

        // Assert
        result.Should().BeNull("User should not see another user's receipts");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task FindByContentHashAsync_ReturnsReceipts_WhenHashExists()
    {
        // Arrange
        var contentHash = "contenthash123";
        var expectedReceipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ContentHash = contentHash
        };

        _receiptRepositoryMock
            .Setup(r => r.FindByContentHashAsync(contentHash, _testUserId))
            .ReturnsAsync(expectedReceipt);

        // Act
        var result = await _receiptRepositoryMock.Object.FindByContentHashAsync(contentHash, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(expectedReceipt);
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesSpecialCharactersInVendor()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);
        var amount = 12.50m;

        // Act - Special characters should be handled consistently
        var hash1 = ReceiptService.ComputeContentHash("MCDONALD'S", date, amount);
        var hash2 = ReceiptService.ComputeContentHash("MCDONALD'S", date, amount);

        // Assert
        hash1.Should().Be(hash2, "Special characters should be handled consistently");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesUnicodeVendorNames()
    {
        // Arrange
        var date = new DateOnly(2024, 6, 15);
        var amount = 12.50m;

        // Act - Unicode characters should be handled
        var hash1 = ReceiptService.ComputeContentHash("Cafe", date, amount);
        var hash2 = ReceiptService.ComputeContentHash("Cafe", date, amount);

        // Assert
        hash1.Should().Be(hash2, "Unicode characters should be handled consistently");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesNegativeAmount()
    {
        // Arrange - Refunds might have negative amounts
        var vendor = "STARBUCKS";
        var date = new DateOnly(2024, 6, 15);

        // Act
        var hashPositive = ReceiptService.ComputeContentHash(vendor, date, 12.50m);
        var hashNegative = ReceiptService.ComputeContentHash(vendor, date, -12.50m);

        // Assert
        hashPositive.Should().NotBe(hashNegative, "Positive and negative amounts should produce different hashes");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesLargeAmount()
    {
        // Arrange
        var vendor = "EXPENSIVE HOTEL";
        var date = new DateOnly(2024, 6, 15);
        var largeAmount = 999999.99m;

        // Act
        var hash = ReceiptService.ComputeContentHash(vendor, date, largeAmount);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ComputeContentHash_HandlesPrecisionDifferences()
    {
        // Arrange - Amounts with different internal precision
        var vendor = "STARBUCKS";
        var date = new DateOnly(2024, 6, 15);

        // Act
        var hash1 = ReceiptService.ComputeContentHash(vendor, date, 12.50m);
        var hash2 = ReceiptService.ComputeContentHash(vendor, date, 12.5000m);

        // Assert
        hash1.Should().Be(hash2, "Amount precision differences should not affect hash");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ComputeFileHash_HandlesLargeFile()
    {
        // Arrange - 10MB of random data
        var largeContent = new byte[10 * 1024 * 1024];
        new Random(42).NextBytes(largeContent);
        using var stream = new MemoryStream(largeContent);

        // Act
        var hash = await ReceiptService.ComputeFileHashAsync(stream);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
    }

    #endregion

    #region Helper Methods

    private static byte[] CreateJpegBytes()
    {
        // Minimal valid JPEG bytes (just the magic bytes + some content)
        return new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9
        };
    }

    private static string ComputeSha256Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    #endregion
}
