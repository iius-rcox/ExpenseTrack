using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Jobs;
using ExpenseFlow.Infrastructure.Repositories;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Jobs;

/// <summary>
/// Unit tests for ReportGenerationBackgroundJob.
/// Tests job execution, progress tracking, cancellation handling, and error scenarios.
/// </summary>
public class ReportGenerationBackgroundJobTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ReportJobRepository _repository;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<ICategorizationService> _categorizationServiceMock;
    private readonly Mock<IDescriptionNormalizationService> _normalizationServiceMock;
    private readonly Mock<IExpenseReportRepository> _reportRepositoryMock;
    private readonly Mock<ILogger<ReportGenerationBackgroundJob>> _loggerMock;
    private readonly ReportGenerationBackgroundJob _job;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ReportGenerationBackgroundJobTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _repository = new ReportJobRepository(_dbContext);
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _categorizationServiceMock = new Mock<ICategorizationService>();
        _normalizationServiceMock = new Mock<IDescriptionNormalizationService>();
        _reportRepositoryMock = new Mock<IExpenseReportRepository>();
        _loggerMock = new Mock<ILogger<ReportGenerationBackgroundJob>>();

        // Default mock setups
        _matchRepositoryMock
            .Setup(r => r.GetConfirmedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ReceiptTransactionMatch>());

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<Transaction>());

        _normalizationServiceMock
            .Setup(s => s.NormalizeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string desc, Guid _, CancellationToken _) => new NormalizationResultDto
            {
                RawDescription = desc,
                NormalizedDescription = desc,
                ExtractedVendor = "Test Vendor",
                Tier = 1,
                CacheHit = true,
                Confidence = 1.0m
            });

        _job = new ReportGenerationBackgroundJob(
            _dbContext,
            _repository,
            _matchRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _categorizationServiceMock.Object,
            _normalizationServiceMock.Object,
            _reportRepositoryMock.Object,
            _loggerMock.Object,
            null); // No prediction service
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_NonExistentJob_ReturnsWithoutError()
    {
        // Arrange
        var nonExistentJobId = Guid.NewGuid();

        // Act & Assert - should not throw
        await _job.ExecuteAsync(nonExistentJobId, CancellationToken.None);

        // Verify no report was created
        _reportRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyCancelledJob_SkipsProcessing()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.CancellationRequested);

        // Act
        await _job.ExecuteAsync(reportJob.Id, CancellationToken.None);

        // Assert
        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.Status.Should().Be(ReportJobStatus.Cancelled);
        updatedJob.ErrorMessage.Should().Be("Cancelled by user");
    }

    [Fact]
    public async Task ExecuteAsync_NoTransactions_FailsWithMessage()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);

        _matchRepositoryMock
            .Setup(r => r.GetConfirmedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ReceiptTransactionMatch>());

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<Transaction>());

        // Act
        await _job.ExecuteAsync(reportJob.Id, CancellationToken.None);

        // Assert
        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.Status.Should().Be(ReportJobStatus.Failed);
        updatedJob.ErrorMessage.Should().Contain("No transactions found");
    }

    [Fact]
    public async Task ExecuteAsync_WithTransactions_UpdatesStatusToProcessing()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);
        var transactions = CreateTestTransactions(5);

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(transactions);

        _reportRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken _) => report);

        // Act
        await _job.ExecuteAsync(reportJob.Id, CancellationToken.None);

        // Assert - job should have been set to Processing during execution
        // and then Completed after
        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.Status.Should().Be(ReportJobStatus.Completed);
        updatedJob.StartedAt.Should().NotBeNull();
        updatedJob.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithTransactions_CreatesReport()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);
        var transactions = CreateTestTransactions(3);

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(transactions);

        ExpenseReport? createdReport = null;
        _reportRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken _) =>
            {
                createdReport = report;
                return report;
            });

        // Act
        await _job.ExecuteAsync(reportJob.Id, CancellationToken.None);

        // Assert
        createdReport.Should().NotBeNull();
        createdReport!.Lines.Should().HaveCount(3);
        createdReport.UserId.Should().Be(_testUserId);
        createdReport.Period.Should().Be("2026-01");
        createdReport.Status.Should().Be(ReportStatus.Draft);
    }

    [Fact]
    public async Task ExecuteAsync_WithTransactions_TracksProgress()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);
        var transactions = CreateTestTransactions(15); // More than progress update interval (10)

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(transactions);

        _reportRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken _) => report);

        // Act
        await _job.ExecuteAsync(reportJob.Id, CancellationToken.None);

        // Assert
        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.TotalLines.Should().Be(15);
        updatedJob.ProcessedLines.Should().Be(15);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_CancellationRequestedDuringProcessing_StopsAndMarksCancelled()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);
        var transactions = CreateTestTransactions(20);

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(transactions);

        // Simulate cancellation being requested after some processing
        var callCount = 0;
        _normalizationServiceMock
            .Setup(s => s.NormalizeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(async (string desc, Guid userId, CancellationToken ct) =>
            {
                callCount++;
                if (callCount == 5)
                {
                    // Simulate cancellation request
                    var job = await _repository.GetByIdAsync(reportJob.Id, ct);
                    job!.Status = ReportJobStatus.CancellationRequested;
                    await _repository.UpdateAsync(job, ct);
                }
                return new NormalizationResultDto
                {
                    RawDescription = desc,
                    NormalizedDescription = desc,
                    ExtractedVendor = "Test Vendor",
                    Tier = 1,
                    CacheHit = true,
                    Confidence = 1.0m
                };
            });

        // Act
        await _job.ExecuteAsync(reportJob.Id, CancellationToken.None);

        // Assert
        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.Status.Should().Be(ReportJobStatus.Cancelled);
    }

    [Fact]
    public async Task ExecuteAsync_CancellationTokenCancelled_MarksCancelled()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);
        var transactions = CreateTestTransactions(10);

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(transactions);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        await _job.ExecuteAsync(reportJob.Id, cts.Token);

        // Assert
        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.Status.Should().Be(ReportJobStatus.Cancelled);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_CategorizationFails_ContinuesWithFailedLineCount()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);
        var transactions = CreateTestTransactions(5);

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(transactions);

        // Make categorization fail for some transactions
        var callCount = 0;
        _categorizationServiceMock
            .Setup(s => s.GetCategorizationAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount % 2 == 0)
                    throw new Exception("Categorization failed");
                return null;
            });

        _reportRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken _) => report);

        // Act
        await _job.ExecuteAsync(reportJob.Id, CancellationToken.None);

        // Assert - job should complete despite categorization failures
        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.Status.Should().Be(ReportJobStatus.Completed);
        updatedJob.FailedLines.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteAsync_ReportSaveFails_MarksFailed()
    {
        // Arrange
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);
        var transactions = CreateTestTransactions(3);

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(transactions);

        _reportRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _job.ExecuteAsync(reportJob.Id, CancellationToken.None));

        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.Status.Should().Be(ReportJobStatus.Failed);
        updatedJob.ErrorMessage.Should().Contain("Database error");
    }

    [Fact]
    public async Task ExecuteAsync_CancellationDuringExceptionHandling_PreservesCancelledStatus()
    {
        // Arrange - Test for race condition fix: when cancellation and exception occur simultaneously,
        // the cancellation status should be preserved instead of being overwritten with Failed
        var reportJob = await CreateTestJobAsync(ReportJobStatus.Pending);
        var transactions = CreateTestTransactions(3);

        _transactionRepositoryMock
            .Setup(r => r.GetUnmatchedByPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(transactions);

        // Simulate cancellation request arriving during report save failure
        _reportRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .Returns(async (ExpenseReport _, CancellationToken ct) =>
            {
                // Simulate cancellation request arriving just before the exception
                var job = await _repository.GetByIdAsync(reportJob.Id, ct);
                job!.Status = ReportJobStatus.CancellationRequested;
                await _repository.UpdateAsync(job, ct);

                throw new Exception("Database error during save");
            });

        // Act - should NOT throw because cancellation is detected and handled gracefully
        await _job.ExecuteAsync(reportJob.Id, CancellationToken.None);

        // Assert - status should be Cancelled, NOT Failed
        var updatedJob = await _repository.GetByIdAsync(reportJob.Id);
        updatedJob!.Status.Should().Be(ReportJobStatus.Cancelled);
        updatedJob.ErrorMessage.Should().Be("Cancelled by user");
    }

    #endregion

    #region Helper Methods

    private async Task<ReportGenerationJob> CreateTestJobAsync(ReportJobStatus status)
    {
        var job = new ReportGenerationJob
        {
            UserId = _testUserId,
            Period = "2026-01",
            Status = status,
            CreatedAt = DateTime.UtcNow
        };

        return await _repository.AddAsync(job);
    }

    private List<Transaction> CreateTestTransactions(int count)
    {
        var transactions = new List<Transaction>();
        for (var i = 0; i < count; i++)
        {
            transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                OriginalDescription = $"Test Transaction {i + 1}",
                Amount = 100m + i,
                TransactionDate = new DateOnly(2026, 1, 15),
                CreatedAt = DateTime.UtcNow
            });
        }
        return transactions;
    }

    #endregion
}
