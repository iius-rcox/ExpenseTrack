using ExpenseFlow.Api.Controllers;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ExpenseFlow.Api.Tests.Controllers;

public class CacheWarmingControllerTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly Mock<ICacheWarmingService> _cacheWarmingServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<CacheWarmingController>> _loggerMock;
    private readonly CacheWarmingController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public CacheWarmingControllerTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _cacheWarmingServiceMock = new Mock<ICacheWarmingService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<CacheWarmingController>>();

        _controller = new CacheWarmingController(
            _cacheWarmingServiceMock.Object,
            _userServiceMock.Object,
            _dbContext,
            _loggerMock.Object);

        // Setup user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("oid", "test-oid"),
            new Claim("preferred_username", "test@example.com")
        }, "test"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        // Setup user service mock
        _userServiceMock
            .Setup(x => x.GetOrCreateUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new User
            {
                Id = _testUserId,
                EntraObjectId = "test-oid",
                Email = "test@example.com",
                DisplayName = "Test User"
            });
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task UploadHistoricalData_ValidFile_Returns202Accepted()
    {
        // Arrange
        var file = CreateMockFile("test.xlsx", 1024);
        var expectedJob = new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            SourceFileName = "test.xlsx",
            BlobUrl = "https://test.blob.core.windows.net/imports/test.xlsx",
            Status = ImportJobStatus.Pending,
            StartedAt = DateTime.UtcNow
        };

        _cacheWarmingServiceMock
            .Setup(x => x.ImportHistoricalDataAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<Guid>()))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _controller.UploadHistoricalData(file, CancellationToken.None);

        // Assert
        var acceptedResult = result.Result.Should().BeOfType<AcceptedAtActionResult>().Subject;
        acceptedResult.StatusCode.Should().Be(202);

        var response = acceptedResult.Value.Should().BeOfType<ImportJobResponse>().Subject;
        response.Id.Should().Be(expectedJob.Id);
        response.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task UploadHistoricalData_NullFile_Returns400BadRequest()
    {
        // Act
        var result = await _controller.UploadHistoricalData(null!, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UploadHistoricalData_InvalidFileExtension_Returns400BadRequest()
    {
        // Arrange
        var file = CreateMockFile("test.csv", 1024);

        // Act
        var result = await _controller.UploadHistoricalData(file, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetImportJob_ExistingJob_Returns200Ok()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var expectedJob = new ImportJob
        {
            Id = jobId,
            UserId = _testUserId,
            SourceFileName = "test.xlsx",
            BlobUrl = "https://test.blob.core.windows.net/imports/test.xlsx",
            Status = ImportJobStatus.Completed,
            StartedAt = DateTime.UtcNow.AddMinutes(-5),
            CompletedAt = DateTime.UtcNow,
            TotalRecords = 100,
            ProcessedRecords = 100,
            CachedDescriptions = 50
        };

        _cacheWarmingServiceMock
            .Setup(x => x.GetImportJobAsync(jobId))
            .ReturnsAsync(expectedJob);

        // Act
        var result = await _controller.GetImportJob(jobId, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImportJobResponse>().Subject;
        response.Id.Should().Be(jobId);
        response.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task GetImportJob_NonExistentJob_Returns404NotFound()
    {
        // Arrange
        _cacheWarmingServiceMock
            .Setup(x => x.GetImportJobAsync(It.IsAny<Guid>()))
            .ReturnsAsync((ImportJob?)null);

        // Act
        var result = await _controller.GetImportJob(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CancelImportJob_CancellableJob_Returns204NoContent()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new ImportJob
        {
            Id = jobId,
            Status = ImportJobStatus.Pending
        };

        _cacheWarmingServiceMock
            .Setup(x => x.GetImportJobAsync(jobId))
            .ReturnsAsync(job);

        _cacheWarmingServiceMock
            .Setup(x => x.CancelImportJobAsync(jobId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.CancelImportJob(jobId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task CancelImportJob_NonCancellableJob_Returns400BadRequest()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new ImportJob
        {
            Id = jobId,
            Status = ImportJobStatus.Completed
        };

        _cacheWarmingServiceMock
            .Setup(x => x.GetImportJobAsync(jobId))
            .ReturnsAsync(job);

        _cacheWarmingServiceMock
            .Setup(x => x.CancelImportJobAsync(jobId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.CancelImportJob(jobId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ListImportJobs_ReturnsPagedResults()
    {
        // Arrange
        var jobs = new List<ImportJob>
        {
            CreateTestJob(ImportJobStatus.Completed),
            CreateTestJob(ImportJobStatus.Processing)
        };

        _cacheWarmingServiceMock
            .Setup(x => x.GetImportJobsAsync(
                It.IsAny<Guid>(),
                It.IsAny<ImportJobStatus?>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ReturnsAsync((jobs, 5));

        // Act
        var result = await _controller.ListImportJobs(null, 1, 20, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ImportJobListResponse>().Subject;
        response.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(5);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(20);
    }

    private static IFormFile CreateMockFile(string fileName, int size)
    {
        var fileMock = new Mock<IFormFile>();
        var content = new byte[size];
        var stream = new MemoryStream(content);

        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(size);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);

        return fileMock.Object;
    }

    private ImportJob CreateTestJob(ImportJobStatus status)
    {
        return new ImportJob
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            SourceFileName = "test.xlsx",
            BlobUrl = "https://test.blob.core.windows.net/imports/test.xlsx",
            Status = status,
            StartedAt = DateTime.UtcNow,
            TotalRecords = 100,
            ProcessedRecords = status == ImportJobStatus.Completed ? 100 : 50
        };
    }
}
