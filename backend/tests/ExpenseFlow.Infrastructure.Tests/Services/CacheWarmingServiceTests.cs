using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Services;
using FluentAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

public class CacheWarmingServiceTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<ILogger<CacheWarmingService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly CacheWarmingService _service;

    public CacheWarmingServiceTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _loggerMock = new Mock<ILogger<CacheWarmingService>>();

        var configData = new Dictionary<string, string?>
        {
            ["BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=test;EndpointSuffix=core.windows.net",
            ["BlobStorage:ImportsContainer"] = "test-imports"
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        _service = new CacheWarmingService(
            _dbContext,
            _configuration,
            _backgroundJobClientMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetImportJobAsync_ExistingJob_ReturnsJob()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var job = new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceFileName = "test.xlsx",
            BlobUrl = "https://test.blob.core.windows.net/imports/test.xlsx",
            Status = ImportJobStatus.Completed,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            TotalRecords = 100,
            ProcessedRecords = 100,
            CachedDescriptions = 50,
            CreatedAliases = 10,
            GeneratedEmbeddings = 45
        };

        _dbContext.ImportJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetImportJobAsync(job.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
        result.Status.Should().Be(ImportJobStatus.Completed);
        result.CachedDescriptions.Should().Be(50);
    }

    [Fact]
    public async Task GetImportJobAsync_NonExistentJob_ReturnsNull()
    {
        // Act
        var result = await _service.GetImportJobAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetImportJobsAsync_FiltersByUserAndStatus()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        var jobs = new[]
        {
            CreateJob(userId, ImportJobStatus.Completed),
            CreateJob(userId, ImportJobStatus.Processing),
            CreateJob(userId, ImportJobStatus.Completed),
            CreateJob(otherUserId, ImportJobStatus.Completed)
        };

        _dbContext.ImportJobs.AddRange(jobs);
        await _dbContext.SaveChangesAsync();

        // Act
        var (result, totalCount) = await _service.GetImportJobsAsync(
            userId, ImportJobStatus.Completed, 1, 10);

        // Assert
        totalCount.Should().Be(2);
        result.Should().HaveCount(2);
        result.Should().OnlyContain(j => j.UserId == userId && j.Status == ImportJobStatus.Completed);
    }

    [Fact]
    public async Task CancelImportJobAsync_PendingJob_CancelsSuccessfully()
    {
        // Arrange
        var job = CreateJob(Guid.NewGuid(), ImportJobStatus.Pending);
        _dbContext.ImportJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.CancelImportJobAsync(job.Id);

        // Assert
        result.Should().BeTrue();

        var updatedJob = await _dbContext.ImportJobs.FindAsync(job.Id);
        updatedJob!.Status.Should().Be(ImportJobStatus.Cancelled);
        updatedJob.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelImportJobAsync_CompletedJob_ReturnsFalse()
    {
        // Arrange
        var job = CreateJob(Guid.NewGuid(), ImportJobStatus.Completed);
        job.CompletedAt = DateTime.UtcNow;
        _dbContext.ImportJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.CancelImportJobAsync(job.Id);

        // Assert
        result.Should().BeFalse();

        var updatedJob = await _dbContext.ImportJobs.FindAsync(job.Id);
        updatedJob!.Status.Should().Be(ImportJobStatus.Completed);
    }

    [Fact]
    public async Task GetImportJobErrorsAsync_JobWithErrors_ReturnsPaginatedErrors()
    {
        // Arrange
        var job = CreateJob(Guid.NewGuid(), ImportJobStatus.Completed);
        job.ErrorLog = """
        [
            {"LineNumber":5,"ErrorMessage":"Invalid date","RawData":"bad row 1"},
            {"LineNumber":10,"ErrorMessage":"Missing description","RawData":"bad row 2"},
            {"LineNumber":15,"ErrorMessage":"Invalid amount","RawData":"bad row 3"}
        ]
        """;
        _dbContext.ImportJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        // Act
        var (errors, totalCount) = await _service.GetImportJobErrorsAsync(job.Id, 1, 2);

        // Assert
        totalCount.Should().Be(3);
        errors.Should().HaveCount(2);
        errors[0].LineNumber.Should().Be(5);
        errors[1].LineNumber.Should().Be(10);
    }

    [Fact]
    public async Task GetImportJobErrorsAsync_JobWithNoErrors_ReturnsEmpty()
    {
        // Arrange
        var job = CreateJob(Guid.NewGuid(), ImportJobStatus.Completed);
        job.ErrorLog = null;
        _dbContext.ImportJobs.Add(job);
        await _dbContext.SaveChangesAsync();

        // Act
        var (errors, totalCount) = await _service.GetImportJobErrorsAsync(job.Id, 1, 10);

        // Assert
        totalCount.Should().Be(0);
        errors.Should().BeEmpty();
    }

    private static ImportJob CreateJob(Guid userId, ImportJobStatus status)
    {
        return new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SourceFileName = $"test_{Guid.NewGuid():N}.xlsx",
            BlobUrl = "https://test.blob.core.windows.net/imports/test.xlsx",
            Status = status,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            TotalRecords = 100,
            ProcessedRecords = status == ImportJobStatus.Completed ? 100 : 50
        };
    }
}
