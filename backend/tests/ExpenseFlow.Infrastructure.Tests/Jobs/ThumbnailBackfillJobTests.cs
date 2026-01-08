using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Jobs;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Jobs;

/// <summary>
/// Unit tests for ThumbnailBackfillJob.
/// Tests thumbnail backfill job initialization and status management.
/// </summary>
public class ThumbnailBackfillJobTests
{
    private readonly Mock<IReceiptRepository> _receiptRepositoryMock;
    private readonly Mock<IBlobStorageService> _blobStorageServiceMock;
    private readonly Mock<IThumbnailService> _thumbnailServiceMock;
    private readonly Mock<IHtmlThumbnailService> _htmlThumbnailServiceMock;
    private readonly Mock<IHtmlSanitizationService> _htmlSanitizationServiceMock;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<ILogger<ThumbnailBackfillJob>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly ThumbnailBackfillJob _job;

    public ThumbnailBackfillJobTests()
    {
        _receiptRepositoryMock = new Mock<IReceiptRepository>();
        _blobStorageServiceMock = new Mock<IBlobStorageService>();
        _thumbnailServiceMock = new Mock<IThumbnailService>();
        _htmlThumbnailServiceMock = new Mock<IHtmlThumbnailService>();
        _htmlSanitizationServiceMock = new Mock<IHtmlSanitizationService>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _loggerMock = new Mock<ILogger<ThumbnailBackfillJob>>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ReceiptProcessing:Thumbnail:BackfillBatchSize", "50" }
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _job = new ThumbnailBackfillJob(
            _receiptRepositoryMock.Object,
            _blobStorageServiceMock.Object,
            _thumbnailServiceMock.Object,
            _htmlThumbnailServiceMock.Object,
            _htmlSanitizationServiceMock.Object,
            _backgroundJobClientMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    [Fact]
    public async Task StartBackfillAsync_NoReceiptsToProcess_ReturnsEmptyJobId()
    {
        // Arrange
        _receiptRepositoryMock
            .Setup(r => r.GetReceiptsWithoutThumbnailsCountAsync(It.IsAny<IEnumerable<string>?>()))
            .ReturnsAsync(0);

        var request = new ThumbnailBackfillRequest();

        // Act
        var response = await _job.StartBackfillAsync(request);

        // Assert
        response.JobId.Should().BeEmpty();
        response.EstimatedCount.Should().Be(0);
        response.Message.Should().Contain("No receipts");
    }

    [Fact]
    public async Task StartBackfillAsync_WithReceiptsToProcess_EnqueuesJob()
    {
        // Arrange
        _receiptRepositoryMock
            .Setup(r => r.GetReceiptsWithoutThumbnailsCountAsync(It.IsAny<IEnumerable<string>?>()))
            .ReturnsAsync(100);

        _backgroundJobClientMock
            .Setup(c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<IState>()))
            .Returns("test-job-id");

        var request = new ThumbnailBackfillRequest { BatchSize = 25 };

        // Act
        var response = await _job.StartBackfillAsync(request);

        // Assert
        response.JobId.Should().NotBeEmpty();
        response.EstimatedCount.Should().Be(100);
        response.Message.Should().Contain("started");
    }

    [Fact]
    public async Task StartBackfillAsync_ClampsBatchSizeToValidRange()
    {
        // Arrange
        _receiptRepositoryMock
            .Setup(r => r.GetReceiptsWithoutThumbnailsCountAsync(It.IsAny<IEnumerable<string>?>()))
            .ReturnsAsync(10);

        _backgroundJobClientMock
            .Setup(c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<IState>()))
            .Returns("test-job-id");

        // Request with batch size above max (500)
        var request = new ThumbnailBackfillRequest { BatchSize = 1000 };

        // Act
        var response = await _job.StartBackfillAsync(request);

        // Assert - should succeed (batch size clamped internally)
        response.JobId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetStatusAsync_ReturnsCurrentStatus()
    {
        // Act
        var status = await _job.GetStatusAsync();

        // Assert
        status.Should().NotBeNull();
        status.Status.Should().Be(BackfillJobStatus.Idle); // Default state
    }

    [Fact]
    public async Task RegenerateThumbnailAsync_ReceiptNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(receiptId, userId))
            .ReturnsAsync((Receipt?)null);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _job.RegenerateThumbnailAsync(receiptId, userId));
    }

    [Fact]
    public async Task RegenerateThumbnailAsync_ValidReceipt_EnqueuesJob()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = userId,
            BlobUrl = "https://storage.blob.core.windows.net/receipts/test.jpg",
            OriginalFilename = "test.jpg",
            ContentType = "image/jpeg",
            Status = ReceiptStatus.Ready
        };

        _receiptRepositoryMock
            .Setup(r => r.GetByIdAsync(receiptId, userId))
            .ReturnsAsync(receipt);

        _backgroundJobClientMock
            .Setup(c => c.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<IState>()))
            .Returns("regen-job-id");

        // Act
        var response = await _job.RegenerateThumbnailAsync(receiptId, userId);

        // Assert
        response.ReceiptId.Should().Be(receiptId);
        response.JobId.Should().Be("regen-job-id");
        response.Message.Should().Contain("queued");
    }
}
