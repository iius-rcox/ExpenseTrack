using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Integration tests for ReportService with recurring allowances.
/// </summary>
public class ReportServiceAllowanceTests : IDisposable
{
    private readonly ExpenseFlowDbContext _context;
    private readonly Mock<IExpenseReportRepository> _reportRepoMock;
    private readonly Mock<IMatchRepository> _matchRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<ICategorizationService> _categorizationServiceMock;
    private readonly Mock<IDescriptionNormalizationService> _normalizationServiceMock;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<IDescriptionCacheService> _descriptionCacheServiceMock;
    private readonly Mock<IAllowanceService> _allowanceServiceMock;
    private readonly Mock<ILogger<ReportService>> _loggerMock;
    private readonly ReportService _service;

    private readonly Guid _testUserId = Guid.NewGuid();

    public ReportServiceAllowanceTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ExpenseFlowDbContext(options);

        _reportRepoMock = new Mock<IExpenseReportRepository>();
        _matchRepoMock = new Mock<IMatchRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _categorizationServiceMock = new Mock<ICategorizationService>();
        _normalizationServiceMock = new Mock<IDescriptionNormalizationService>();
        _vendorAliasServiceMock = new Mock<IVendorAliasService>();
        _descriptionCacheServiceMock = new Mock<IDescriptionCacheService>();
        _allowanceServiceMock = new Mock<IAllowanceService>();
        _loggerMock = new Mock<ILogger<ReportService>>();

        // Setup normalization to return input unchanged
        _normalizationServiceMock
            .Setup(s => s.NormalizeAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string desc, Guid userId, CancellationToken ct) => new NormalizationResultDto
            {
                RawDescription = desc,
                NormalizedDescription = desc
            });

        // Setup report repo to capture the added report
        _reportRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken ct) => report);

        _reportRepoMock
            .Setup(r => r.GetDraftByUserAndPeriodAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport?)null);

        _service = new ReportService(
            _reportRepoMock.Object,
            _matchRepoMock.Object,
            _transactionRepoMock.Object,
            _categorizationServiceMock.Object,
            _normalizationServiceMock.Object,
            _vendorAliasServiceMock.Object,
            _descriptionCacheServiceMock.Object,
            _loggerMock.Object,
            predictionService: null,
            allowanceService: _allowanceServiceMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateDraftAsync_WithActiveAllowances_IncludesAllowanceLines()
    {
        // Arrange
        var period = "2025-01";
        var allowances = new List<RecurringAllowance>
        {
            new RecurringAllowance
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                VendorName = "Verizon",
                Amount = 50.00m,
                Frequency = AllowanceFrequency.Monthly,
                GLCode = "66300",
                GLName = "Cell Phone Expense",
                DepartmentCode = "DEPT01",
                Description = "Monthly cell phone allowance",
                IsActive = true
            },
            new RecurringAllowance
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                VendorName = "Comcast",
                Amount = 75.00m,
                Frequency = AllowanceFrequency.Monthly,
                GLCode = "66400",
                GLName = "Internet Expense",
                DepartmentCode = "DEPT01",
                Description = "Monthly internet allowance",
                IsActive = true
            }
        };

        _allowanceServiceMock
            .Setup(s => s.GetActiveAllowancesForPeriodAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allowances);

        // No matches or unmatched transactions
        _matchRepoMock
            .Setup(m => m.GetConfirmedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ReceiptTransactionMatch>());

        _transactionRepoMock
            .Setup(t => t.GetUnmatchedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<Transaction>());

        // Capture the report that gets saved
        ExpenseReport? savedReport = null;
        _reportRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken ct) =>
            {
                savedReport = report;
                return report;
            });

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, period);

        // Assert
        savedReport.Should().NotBeNull();
        savedReport!.Lines.Should().HaveCount(2);
        savedReport.TotalAmount.Should().Be(125.00m); // 50 + 75

        var verizonLine = savedReport.Lines.FirstOrDefault(l => l.VendorName == "Verizon");
        verizonLine.Should().NotBeNull();
        verizonLine!.Amount.Should().Be(50.00m);
        verizonLine.GLCode.Should().Be("66300");
        verizonLine.AllowanceId.Should().NotBeNull();
        verizonLine.TransactionId.Should().BeNull();
        verizonLine.HasReceipt.Should().BeFalse();
        verizonLine.MissingReceiptJustification.Should().Be(MissingReceiptJustification.DigitalSubscription);

        var comcastLine = savedReport.Lines.FirstOrDefault(l => l.VendorName == "Comcast");
        comcastLine.Should().NotBeNull();
        comcastLine!.Amount.Should().Be(75.00m);
        comcastLine.GLCode.Should().Be("66400");
        comcastLine.AllowanceId.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateDraftAsync_AllowanceLines_HaveCorrectMetadata()
    {
        // Arrange
        var period = "2025-01";
        var allowanceId = Guid.NewGuid();
        var allowances = new List<RecurringAllowance>
        {
            new RecurringAllowance
            {
                Id = allowanceId,
                UserId = _testUserId,
                VendorName = "Test Vendor",
                Amount = 100.00m,
                Frequency = AllowanceFrequency.Monthly,
                GLCode = "12345",
                GLName = "Test GL",
                DepartmentCode = "TEST",
                Description = "Test Description",
                IsActive = true
            }
        };

        _allowanceServiceMock
            .Setup(s => s.GetActiveAllowancesForPeriodAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allowances);

        _matchRepoMock
            .Setup(m => m.GetConfirmedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ReceiptTransactionMatch>());

        _transactionRepoMock
            .Setup(t => t.GetUnmatchedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<Transaction>());

        ExpenseReport? savedReport = null;
        _reportRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken ct) =>
            {
                savedReport = report;
                return report;
            });

        // Act
        await _service.GenerateDraftAsync(_testUserId, period);

        // Assert
        savedReport.Should().NotBeNull();
        var line = savedReport!.Lines.Single();

        // Verify all metadata is correct
        line.AllowanceId.Should().Be(allowanceId);
        line.TransactionId.Should().BeNull();
        line.ReceiptId.Should().BeNull();
        line.VendorName.Should().Be("Test Vendor");
        line.Amount.Should().Be(100.00m);
        line.GLCode.Should().Be("12345");
        line.GLCodeSuggested.Should().Be("12345");
        line.GLCodeSource.Should().Be("RecurringAllowance");
        line.DepartmentCode.Should().Be("TEST");
        line.DepartmentSuggested.Should().Be("TEST");
        line.DepartmentSource.Should().Be("RecurringAllowance");
        line.NormalizedDescription.Should().Be("Test Description");
        line.HasReceipt.Should().BeFalse();
        line.MissingReceiptJustification.Should().Be(MissingReceiptJustification.DigitalSubscription);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateDraftAsync_AllowanceLines_HaveNoTransaction()
    {
        // Arrange
        var period = "2025-01";
        var allowances = new List<RecurringAllowance>
        {
            new RecurringAllowance
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                VendorName = "Verizon",
                Amount = 50.00m,
                Frequency = AllowanceFrequency.Monthly,
                IsActive = true
            }
        };

        _allowanceServiceMock
            .Setup(s => s.GetActiveAllowancesForPeriodAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allowances);

        _matchRepoMock
            .Setup(m => m.GetConfirmedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ReceiptTransactionMatch>());

        _transactionRepoMock
            .Setup(t => t.GetUnmatchedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<Transaction>());

        ExpenseReport? savedReport = null;
        _reportRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken ct) =>
            {
                savedReport = report;
                return report;
            });

        // Act
        await _service.GenerateDraftAsync(_testUserId, period);

        // Assert
        savedReport.Should().NotBeNull();
        var line = savedReport!.Lines.Single();
        line.TransactionId.Should().BeNull("allowance lines should not have a transaction");
        line.ReceiptId.Should().BeNull("allowance lines should not have a receipt");
        line.AllowanceId.Should().NotBeNull("allowance lines must have an allowance reference");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateDraftAsync_AllowanceServiceFailure_ContinuesWithoutAllowances()
    {
        // Arrange
        var period = "2025-01";

        _allowanceServiceMock
            .Setup(s => s.GetActiveAllowancesForPeriodAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        _matchRepoMock
            .Setup(m => m.GetConfirmedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ReceiptTransactionMatch>());

        _transactionRepoMock
            .Setup(t => t.GetUnmatchedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<Transaction>());

        ExpenseReport? savedReport = null;
        _reportRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken ct) =>
            {
                savedReport = report;
                return report;
            });

        // Act - Should not throw
        var result = await _service.GenerateDraftAsync(_testUserId, period);

        // Assert - Report should still be created without allowances
        savedReport.Should().NotBeNull();
        savedReport!.Lines.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GenerateDraftAsync_MixedTransactionsAndAllowances_IncludesBoth()
    {
        // Arrange
        var period = "2025-01";
        var transactionId = Guid.NewGuid();

        var allowances = new List<RecurringAllowance>
        {
            new RecurringAllowance
            {
                Id = Guid.NewGuid(),
                UserId = _testUserId,
                VendorName = "Verizon",
                Amount = 50.00m,
                Frequency = AllowanceFrequency.Monthly,
                IsActive = true
            }
        };

        var unmatchedTransactions = new List<Transaction>
        {
            new Transaction
            {
                Id = transactionId,
                UserId = _testUserId,
                TransactionDate = new DateOnly(2025, 1, 15),
                Amount = 100.00m,
                OriginalDescription = "UBER TRIP",
                Description = "Uber Trip"
            }
        };

        _allowanceServiceMock
            .Setup(s => s.GetActiveAllowancesForPeriodAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(allowances);

        _matchRepoMock
            .Setup(m => m.GetConfirmedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ReceiptTransactionMatch>());

        _transactionRepoMock
            .Setup(t => t.GetUnmatchedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(unmatchedTransactions);

        ExpenseReport? savedReport = null;
        _reportRepoMock
            .Setup(r => r.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken ct) =>
            {
                savedReport = report;
                return report;
            });

        // Act
        await _service.GenerateDraftAsync(_testUserId, period);

        // Assert
        savedReport.Should().NotBeNull();
        savedReport!.Lines.Should().HaveCount(2);
        savedReport.TotalAmount.Should().Be(150.00m); // 100 (transaction) + 50 (allowance)

        // Verify we have both types
        var transactionLine = savedReport.Lines.FirstOrDefault(l => l.TransactionId == transactionId);
        transactionLine.Should().NotBeNull();
        transactionLine!.AllowanceId.Should().BeNull();

        var allowanceLine = savedReport.Lines.FirstOrDefault(l => l.AllowanceId != null);
        allowanceLine.Should().NotBeNull();
        allowanceLine!.TransactionId.Should().BeNull();
    }
}
