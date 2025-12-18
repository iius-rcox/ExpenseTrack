using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Jobs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Pgvector;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Jobs;

public class CacheWarmingJobTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<ILogger<CacheWarmingJob>> _loggerMock;
    private readonly IConfiguration _configuration;

    public CacheWarmingJobTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _loggerMock = new Mock<ILogger<CacheWarmingJob>>();

        var configData = new Dictionary<string, string?>
        {
            ["BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
            ["CacheWarming:BatchSize"] = "100",
            ["CacheWarming:SimilarityThreshold"] = "0.98"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Setup embedding service mock
        _embeddingServiceMock
            .Setup(x => x.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Vector(new float[1536]));
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public void CacheWarmingJob_CanBeInstantiated()
    {
        // Act
        var job = new CacheWarmingJob(
            _dbContext,
            _embeddingServiceMock.Object,
            _configuration,
            _loggerMock.Object);

        // Assert
        job.Should().NotBeNull();
    }

    [Fact]
    public async Task ProcessImportAsync_NonExistentJob_LogsError()
    {
        // Arrange
        var job = new CacheWarmingJob(
            _dbContext,
            _embeddingServiceMock.Object,
            _configuration,
            _loggerMock.Object);

        // Act
        await job.ProcessImportAsync(Guid.NewGuid(), CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessImportAsync_CancelledJob_SkipsProcessing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importJob = new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceFileName = "test.xlsx",
            BlobUrl = "https://test.blob.core.windows.net/imports/test.xlsx",
            Status = ImportJobStatus.Cancelled,
            StartedAt = DateTime.UtcNow
        };

        _dbContext.ImportJobs.Add(importJob);
        await _dbContext.SaveChangesAsync();

        var job = new CacheWarmingJob(
            _dbContext,
            _embeddingServiceMock.Object,
            _configuration,
            _loggerMock.Object);

        // Act
        await job.ProcessImportAsync(importJob.Id, CancellationToken.None);

        // Assert
        var updatedJob = await _dbContext.ImportJobs.FindAsync(importJob.Id);
        updatedJob!.Status.Should().Be(ImportJobStatus.Cancelled);
        updatedJob.ProcessedRecords.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteAsync_ThrowsNotImplementedException()
    {
        // Arrange
        var job = new CacheWarmingJob(
            _dbContext,
            _embeddingServiceMock.Object,
            _configuration,
            _loggerMock.Object);

        // Act & Assert
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            job.ExecuteAsync(CancellationToken.None));
    }
}
