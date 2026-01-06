using System.Security.Claims;
using ExpenseFlow.Api.Controllers;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Api.Tests.Controllers;

/// <summary>
/// Unit tests for ReportJobsController.
/// Tests async report generation endpoints: create, get, list, cancel.
/// </summary>
public class ReportJobsControllerTests
{
    private readonly Mock<IReportJobService> _reportJobServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<ReportJobsController>> _loggerMock;
    private readonly ReportJobsController _controller;
    private readonly User _testUser;

    public ReportJobsControllerTests()
    {
        _reportJobServiceMock = new Mock<IReportJobService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<ReportJobsController>>();

        _testUser = new User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = "test-object-id",
            Email = "test@example.com",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        _userServiceMock
            .Setup(s => s.GetOrCreateUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_testUser);

        _controller = new ReportJobsController(
            _reportJobServiceMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object);

        // Set up HttpContext with a user
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "test-object-id"),
            new("oid", "test-object-id"),
            new(ClaimTypes.Email, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #region Create Tests

    [Fact]
    public async Task Create_ValidRequest_Returns202Accepted()
    {
        // Arrange
        var request = new CreateReportJobRequest { Period = "2026-01" };
        var job = CreateTestJob("2026-01", ReportJobStatus.Pending);

        _reportJobServiceMock
            .Setup(s => s.CreateJobAsync(_testUser.Id, request.Period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var acceptedResult = result.Result.Should().BeOfType<AcceptedAtActionResult>().Subject;
        acceptedResult.ActionName.Should().Be(nameof(ReportJobsController.GetById));
        acceptedResult.StatusCode.Should().Be(StatusCodes.Status202Accepted);

        var dto = acceptedResult.Value.Should().BeOfType<ReportJobDto>().Subject;
        dto.Id.Should().Be(job.Id);
        dto.Period.Should().Be("2026-01");
        dto.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Create_DuplicateJob_Returns409Conflict()
    {
        // Arrange
        var request = new CreateReportJobRequest { Period = "2026-01" };

        _reportJobServiceMock
            .Setup(s => s.CreateJobAsync(_testUser.Id, request.Period, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("An active report generation job already exists for period 2026-01"));

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var conflictResult = result.Result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ExistingJob_Returns200WithJob()
    {
        // Arrange
        var job = CreateTestJob("2026-01", ReportJobStatus.Processing);
        job.ProcessedLines = 50;
        job.TotalLines = 100;

        _reportJobServiceMock
            .Setup(s => s.GetByIdAsync(_testUser.Id, job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Act
        var result = await _controller.GetById(job.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<ReportJobDto>().Subject;
        dto.Id.Should().Be(job.Id);
        dto.Status.Should().Be("Processing");
        dto.ProcessedLines.Should().Be(50);
        dto.TotalLines.Should().Be(100);
        dto.ProgressPercent.Should().Be(50); // 50/100 = 50%
    }

    [Fact]
    public async Task GetById_NonExistentJob_Returns404()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        _reportJobServiceMock
            .Setup(s => s.GetByIdAsync(_testUser.Id, jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportGenerationJob?)null);

        // Act
        var result = await _controller.GetById(jobId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetList Tests

    [Fact]
    public async Task GetList_WithJobs_ReturnsPaginatedList()
    {
        // Arrange
        var jobs = new List<ReportGenerationJob>
        {
            CreateTestJob("2026-01", ReportJobStatus.Completed),
            CreateTestJob("2026-02", ReportJobStatus.Processing)
        };

        _reportJobServiceMock
            .Setup(s => s.GetListAsync(_testUser.Id, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((jobs, 2));

        // Act
        var result = await _controller.GetList(null, 1, 20, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ReportJobListResponse>().Subject;
        response.Items.Should().HaveCount(2);
        response.TotalCount.Should().Be(2);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetList_WithStatusFilter_PassesFilterToService()
    {
        // Arrange
        var jobs = new List<ReportGenerationJob>
        {
            CreateTestJob("2026-01", ReportJobStatus.Completed)
        };

        _reportJobServiceMock
            .Setup(s => s.GetListAsync(_testUser.Id, ReportJobStatus.Completed, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((jobs, 1));

        // Act
        await _controller.GetList(ReportJobStatus.Completed, 1, 20, CancellationToken.None);

        // Assert
        _reportJobServiceMock.Verify(
            s => s.GetListAsync(_testUser.Id, ReportJobStatus.Completed, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetList_InvalidPageSize_ClampedToValid()
    {
        // Arrange
        _reportJobServiceMock
            .Setup(s => s.GetListAsync(_testUser.Id, null, 1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<ReportGenerationJob>(), 0));

        // Act - request page size of 200 (exceeds max of 100)
        await _controller.GetList(null, 1, 200, CancellationToken.None);

        // Assert - should be clamped to 100
        _reportJobServiceMock.Verify(
            s => s.GetListAsync(_testUser.Id, null, 1, 100, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_ActiveJob_Returns200WithUpdatedJob()
    {
        // Arrange
        var job = CreateTestJob("2026-01", ReportJobStatus.CancellationRequested);

        _reportJobServiceMock
            .Setup(s => s.CancelAsync(_testUser.Id, job.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Act
        var result = await _controller.Cancel(job.Id, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = okResult.Value.Should().BeOfType<ReportJobDto>().Subject;
        dto.Status.Should().Be("CancellationRequested");
    }

    [Fact]
    public async Task Cancel_NonExistentJob_Returns404()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        _reportJobServiceMock
            .Setup(s => s.CancelAsync(_testUser.Id, jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportGenerationJob?)null);

        // Act
        var result = await _controller.Cancel(jobId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Cancel_CompletedJob_Returns400BadRequest()
    {
        // Arrange
        var jobId = Guid.NewGuid();

        _reportJobServiceMock
            .Setup(s => s.CancelAsync(_testUser.Id, jobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot cancel job in Completed status"));

        // Act
        var result = await _controller.Cancel(jobId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region GetActiveJob Tests

    [Fact]
    public async Task GetActiveJob_ExistingJob_ReturnsJobDetails()
    {
        // Arrange
        var period = "2026-01";
        var job = CreateTestJob(period, ReportJobStatus.Processing);

        _reportJobServiceMock
            .Setup(s => s.GetActiveJobAsync(_testUser.Id, period, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        // Act
        var result = await _controller.GetActiveJob(period, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ActiveJobResponse>().Subject;
        response.HasActiveJob.Should().BeTrue();
        response.Job.Should().NotBeNull();
        response.Job!.Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task GetActiveJob_NoActiveJob_ReturnsEmptyResponse()
    {
        // Arrange
        var period = "2026-01";

        _reportJobServiceMock
            .Setup(s => s.GetActiveJobAsync(_testUser.Id, period, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReportGenerationJob?)null);

        // Act
        var result = await _controller.GetActiveJob(period, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ActiveJobResponse>().Subject;
        response.HasActiveJob.Should().BeFalse();
        response.Job.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private ReportGenerationJob CreateTestJob(string period, ReportJobStatus status)
    {
        return new ReportGenerationJob
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            Period = period,
            Status = status,
            TotalLines = 100,
            ProcessedLines = 0,
            FailedLines = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
