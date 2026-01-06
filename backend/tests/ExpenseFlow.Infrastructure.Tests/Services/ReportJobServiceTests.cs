using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Repositories;
using ExpenseFlow.Infrastructure.Services;
using FluentAssertions;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for ReportJobService.
/// Tests job creation, status retrieval, pagination, cancellation, and duplicate prevention.
/// </summary>
public class ReportJobServiceTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ReportJobRepository _repository;
    private readonly Mock<IBackgroundJobClient> _backgroundJobClientMock;
    private readonly Mock<ILogger<ReportJobService>> _loggerMock;
    private readonly ReportJobService _service;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ReportJobServiceTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _repository = new ReportJobRepository(_dbContext);
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        _loggerMock = new Mock<ILogger<ReportJobService>>();

        // Setup Hangfire mock to return a job ID
        _backgroundJobClientMock
            .Setup(x => x.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<IState>()))
            .Returns("hangfire-job-123");

        _service = new ReportJobService(
            _repository,
            _backgroundJobClientMock.Object,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region CreateJobAsync Tests

    [Fact]
    public async Task CreateJobAsync_ValidRequest_CreatesJobAndEnqueuesBackgroundJob()
    {
        // Arrange
        var period = "2026-01";

        // Act
        var job = await _service.CreateJobAsync(_testUserId, period);

        // Assert
        job.Should().NotBeNull();
        job.UserId.Should().Be(_testUserId);
        job.Period.Should().Be(period);
        job.Status.Should().Be(ReportJobStatus.Pending);
        job.HangfireJobId.Should().Be("hangfire-job-123");
        job.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify Hangfire was called
        _backgroundJobClientMock.Verify(
            x => x.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<IState>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateJobAsync_DuplicateActiveJob_ThrowsInvalidOperationException()
    {
        // Arrange
        var period = "2026-01";
        await _service.CreateJobAsync(_testUserId, period);

        // Act
        var act = () => _service.CreateJobAsync(_testUserId, period);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateJobAsync_SamePeriodDifferentUser_AllowsCreation()
    {
        // Arrange
        var period = "2026-01";
        var otherUserId = Guid.NewGuid();
        await _service.CreateJobAsync(_testUserId, period);

        // Act
        var job = await _service.CreateJobAsync(otherUserId, period);

        // Assert
        job.Should().NotBeNull();
        job.UserId.Should().Be(otherUserId);
    }

    [Fact]
    public async Task CreateJobAsync_CompletedJobForSamePeriod_AllowsNewJob()
    {
        // Arrange
        var period = "2026-01";
        var existingJob = await _service.CreateJobAsync(_testUserId, period);

        // Simulate completion
        existingJob.Status = ReportJobStatus.Completed;
        existingJob.CompletedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(existingJob);

        // Act
        var newJob = await _service.CreateJobAsync(_testUserId, period);

        // Assert
        newJob.Should().NotBeNull();
        newJob.Id.Should().NotBe(existingJob.Id);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ExistingJob_ReturnsJob()
    {
        // Arrange
        var job = await _service.CreateJobAsync(_testUserId, "2026-01");

        // Act
        var result = await _service.GetByIdAsync(_testUserId, job.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentJob_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(_testUserId, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_JobBelongsToOtherUser_ReturnsNull()
    {
        // Arrange
        var job = await _service.CreateJobAsync(_testUserId, "2026-01");
        var otherUserId = Guid.NewGuid();

        // Act
        var result = await _service.GetByIdAsync(otherUserId, job.Id);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetListAsync Tests

    [Fact]
    public async Task GetListAsync_MultipleJobs_ReturnsPaginatedResults()
    {
        // Arrange
        await _service.CreateJobAsync(_testUserId, "2026-01");
        await CompleteJobForNextCreation("2026-01");
        await _service.CreateJobAsync(_testUserId, "2026-02");
        await CompleteJobForNextCreation("2026-02");
        await _service.CreateJobAsync(_testUserId, "2026-03");

        // Act
        var (jobs, totalCount) = await _service.GetListAsync(_testUserId, null, 1, 2);

        // Assert
        jobs.Should().HaveCount(2);
        totalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetListAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var pendingJob = await _service.CreateJobAsync(_testUserId, "2026-01");
        await CompleteJobForNextCreation("2026-01");
        var completedJob = await _service.CreateJobAsync(_testUserId, "2026-02");
        completedJob.Status = ReportJobStatus.Completed;
        await _repository.UpdateAsync(completedJob);

        // Act
        var (jobs, totalCount) = await _service.GetListAsync(_testUserId, ReportJobStatus.Pending, 1, 10);

        // Assert
        jobs.Should().HaveCount(1);
        jobs[0].Status.Should().Be(ReportJobStatus.Pending);
        totalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetListAsync_OtherUserJobs_NotReturned()
    {
        // Arrange
        await _service.CreateJobAsync(_testUserId, "2026-01");
        var otherUserId = Guid.NewGuid();

        // Act
        var (jobs, totalCount) = await _service.GetListAsync(otherUserId, null, 1, 10);

        // Assert
        jobs.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    #endregion

    #region CancelAsync Tests

    [Fact]
    public async Task CancelAsync_PendingJob_SetsCancellationRequested()
    {
        // Arrange
        var job = await _service.CreateJobAsync(_testUserId, "2026-01");

        // Act
        var result = await _service.CancelAsync(_testUserId, job.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ReportJobStatus.CancellationRequested);
    }

    [Fact]
    public async Task CancelAsync_ProcessingJob_SetsCancellationRequested()
    {
        // Arrange
        var job = await _service.CreateJobAsync(_testUserId, "2026-01");
        job.Status = ReportJobStatus.Processing;
        await _repository.UpdateAsync(job);

        // Act
        var result = await _service.CancelAsync(_testUserId, job.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ReportJobStatus.CancellationRequested);
    }

    [Fact]
    public async Task CancelAsync_CompletedJob_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = await _service.CreateJobAsync(_testUserId, "2026-01");
        job.Status = ReportJobStatus.Completed;
        await _repository.UpdateAsync(job);

        // Act
        var act = () => _service.CancelAsync(_testUserId, job.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel*");
    }

    [Fact]
    public async Task CancelAsync_FailedJob_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = await _service.CreateJobAsync(_testUserId, "2026-01");
        job.Status = ReportJobStatus.Failed;
        await _repository.UpdateAsync(job);

        // Act
        var act = () => _service.CancelAsync(_testUserId, job.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel*");
    }

    [Fact]
    public async Task CancelAsync_AlreadyCancelledJob_ThrowsInvalidOperationException()
    {
        // Arrange
        var job = await _service.CreateJobAsync(_testUserId, "2026-01");
        job.Status = ReportJobStatus.Cancelled;
        await _repository.UpdateAsync(job);

        // Act
        var act = () => _service.CancelAsync(_testUserId, job.Id);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot cancel*");
    }

    [Fact]
    public async Task CancelAsync_OtherUserJob_ReturnsNull()
    {
        // Arrange
        var job = await _service.CreateJobAsync(_testUserId, "2026-01");
        var otherUserId = Guid.NewGuid();

        // Act
        var result = await _service.CancelAsync(otherUserId, job.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CancelAsync_NonExistentJob_ReturnsNull()
    {
        // Act
        var result = await _service.CancelAsync(_testUserId, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetActiveJobAsync Tests

    [Fact]
    public async Task GetActiveJobAsync_ExistingActiveJob_ReturnsJob()
    {
        // Arrange
        var period = "2026-01";
        var job = await _service.CreateJobAsync(_testUserId, period);

        // Act
        var result = await _service.GetActiveJobAsync(_testUserId, period);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(job.Id);
    }

    [Fact]
    public async Task GetActiveJobAsync_NoActiveJob_ReturnsNull()
    {
        // Act
        var result = await _service.GetActiveJobAsync(_testUserId, "2026-01");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveJobAsync_CompletedJobExists_ReturnsNull()
    {
        // Arrange
        var period = "2026-01";
        var job = await _service.CreateJobAsync(_testUserId, period);
        job.Status = ReportJobStatus.Completed;
        await _repository.UpdateAsync(job);

        // Act
        var result = await _service.GetActiveJobAsync(_testUserId, period);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    private async Task CompleteJobForNextCreation(string period)
    {
        var job = await _repository.GetActiveByUserAndPeriodAsync(_testUserId, period);
        if (job != null)
        {
            job.Status = ReportJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(job);
        }
    }

    #endregion
}
