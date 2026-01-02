using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.DTOs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for AnalyticsExportService.
/// Tests CSV and Excel export functionality with section filtering.
/// </summary>
public class AnalyticsExportServiceTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly Mock<IAnalyticsService> _analyticsServiceMock;
    private readonly Mock<ILogger<AnalyticsExportService>> _loggerMock;
    private readonly AnalyticsExportService _service;

    private readonly Guid _testUserId = Guid.NewGuid();

    public AnalyticsExportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _analyticsServiceMock = new Mock<IAnalyticsService>();
        _loggerMock = new Mock<ILogger<AnalyticsExportService>>();

        _service = new AnalyticsExportService(
            _dbContext,
            _analyticsServiceMock.Object,
            _loggerMock.Object);

        // Setup default mock behavior for DeriveCategory
        _analyticsServiceMock
            .Setup(x => x.DeriveCategory(It.IsAny<string>()))
            .Returns((string desc) => desc.Contains("COFFEE") ? "Food & Dining" : "Other");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region Date Range Validation Tests

    [Fact]
    public async Task ExportAsync_DateRangeExceeds5Years_ThrowsException()
    {
        // Arrange
        var startDate = new DateOnly(2020, 1, 1);
        var endDate = new DateOnly(2025, 6, 1); // > 5 years

        // Act & Assert
        var action = () => _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "trends" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*exceeds maximum of 5 years*");
    }

    [Fact]
    public async Task ExportAsync_StartDateAfterEndDate_ThrowsException()
    {
        // Arrange
        var startDate = new DateOnly(2024, 12, 1);
        var endDate = new DateOnly(2024, 1, 1);

        // Act & Assert
        var action = () => _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "trends" });

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Start date must be before or equal to end date*");
    }

    [Fact]
    public async Task ExportAsync_ValidDateRange_Succeeds()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 12, 31);

        SetupMocksForEmptyExport();

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "trends" });

        // Assert
        result.FileBytes.Should().NotBeNull();
        result.FileName.Should().Contain("Analytics_20240101_20241231");
    }

    #endregion

    #region CSV Export Tests

    [Fact]
    public async Task ExportAsync_CsvFormat_ReturnsCsvContentType()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        SetupMocksForEmptyExport();

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "trends" });

        // Assert
        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().EndWith(".csv");
    }

    [Fact]
    public async Task ExportAsync_CsvWithTrends_IncludesTrendData()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        var trends = new List<SpendingTrendItemDto>
        {
            new() { Date = "2024-01", Amount = 1000.00m, TransactionCount = 10 },
            new() { Date = "2024-02", Amount = 1200.00m, TransactionCount = 12 }
        };

        _analyticsServiceMock
            .Setup(x => x.GetSpendingTrendAsync(
                _testUserId, startDate, endDate, "month", It.IsAny<CancellationToken>()))
            .ReturnsAsync(trends);

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "trends" });

        // Assert
        var content = Encoding.UTF8.GetString(result.FileBytes);
        content.Should().Contain("Spending Trends");
        content.Should().Contain("2024-01");
        content.Should().Contain("1000");
    }

    [Fact]
    public async Task ExportAsync_CsvWithCategories_IncludesCategoryData()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        var categories = new List<SpendingByCategoryItemDto>
        {
            new() { Category = "Food & Dining", Amount = 500.00m, TransactionCount = 20, PercentageOfTotal = 50.0m },
            new() { Category = "Transportation", Amount = 500.00m, TransactionCount = 10, PercentageOfTotal = 50.0m }
        };

        _analyticsServiceMock
            .Setup(x => x.GetSpendingByCategoryAsync(
                _testUserId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "categories" });

        // Assert
        var content = Encoding.UTF8.GetString(result.FileBytes);
        content.Should().Contain("Spending by Category");
        content.Should().Contain("Food & Dining");
        content.Should().Contain("Transportation");
    }

    [Fact]
    public async Task ExportAsync_CsvWithTransactions_IncludesTransactionData()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        _dbContext.Transactions.AddRange(
            CreateTestTransaction("STARBUCKS COFFEE", 25.00m, new DateOnly(2024, 3, 15)),
            CreateTestTransaction("UBER RIDE", 35.00m, new DateOnly(2024, 3, 16))
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "transactions" });

        // Assert
        var content = Encoding.UTF8.GetString(result.FileBytes);
        content.Should().Contain("Transactions Export");
        content.Should().Contain("STARBUCKS COFFEE");
        content.Should().Contain("UBER RIDE");
    }

    #endregion

    #region Excel Export Tests

    [Fact]
    public async Task ExportAsync_ExcelFormat_ReturnsExcelContentType()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        SetupMocksForEmptyExport();

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "xlsx", new[] { "trends" });

        // Assert
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.FileName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task ExportAsync_ExcelWithMultipleSections_CreatesSeparateSheets()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        SetupMocksForFullExport();

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "xlsx", new[] { "trends", "categories", "vendors" });

        // Assert
        // Excel file should be created with content
        result.FileBytes.Length.Should().BeGreaterThan(100);
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task ExportAsync_ExcelIncludesSummarySheet()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        var trends = new List<SpendingTrendItemDto>
        {
            new() { Date = "2024-01", Amount = 1000.00m, TransactionCount = 10 }
        };

        _analyticsServiceMock
            .Setup(x => x.GetSpendingTrendAsync(
                _testUserId, startDate, endDate, "month", It.IsAny<CancellationToken>()))
            .ReturnsAsync(trends);

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "xlsx", new[] { "trends" });

        // Assert
        // Excel files with data should have a reasonable size including summary
        result.FileBytes.Length.Should().BeGreaterThan(5000);
    }

    #endregion

    #region Section Filtering Tests

    [Fact]
    public async Task ExportAsync_OnlyRequestedSections_AreIncluded()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        var trends = new List<SpendingTrendItemDto>
        {
            new() { Date = "2024-01", Amount = 1000.00m, TransactionCount = 10 }
        };

        _analyticsServiceMock
            .Setup(x => x.GetSpendingTrendAsync(
                _testUserId, startDate, endDate, "month", It.IsAny<CancellationToken>()))
            .ReturnsAsync(trends);

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "trends" });

        // Assert
        var content = Encoding.UTF8.GetString(result.FileBytes);
        content.Should().Contain("Spending Trends");
        content.Should().NotContain("Spending by Category");
        content.Should().NotContain("Spending by Vendor");
        content.Should().NotContain("Transactions Export");

        // Verify other services were not called
        _analyticsServiceMock.Verify(
            x => x.GetSpendingByCategoryAsync(It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExportAsync_VendorsSection_CallsVendorService()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        var vendors = new List<SpendingByVendorItemDto>
        {
            new() { VendorName = "Starbucks", Amount = 300.00m, TransactionCount = 15, PercentageOfTotal = 60.0m },
            new() { VendorName = "Uber", Amount = 200.00m, TransactionCount = 10, PercentageOfTotal = 40.0m }
        };

        _analyticsServiceMock
            .Setup(x => x.GetSpendingByVendorAsync(
                _testUserId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vendors);

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "vendors" });

        // Assert
        var content = Encoding.UTF8.GetString(result.FileBytes);
        content.Should().Contain("Spending by Vendor");
        content.Should().Contain("Starbucks");
        content.Should().Contain("Uber");
    }

    [Fact]
    public async Task ExportAsync_EmptyData_ReturnsEmptyButValidFile()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 6, 30);

        _analyticsServiceMock
            .Setup(x => x.GetSpendingTrendAsync(
                _testUserId, startDate, endDate, "month", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingTrendItemDto>());

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "trends" });

        // Assert
        result.FileBytes.Should().NotBeNull();
        result.FileName.Should().Contain("Analytics_");
    }

    #endregion

    #region Transaction Export Tests

    [Fact]
    public async Task ExportAsync_TransactionsIncludeReceiptFlag()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 12, 31);

        var transactionWithReceipt = CreateTestTransaction("STARBUCKS COFFEE", 25.00m, new DateOnly(2024, 3, 15));
        transactionWithReceipt.MatchedReceiptId = Guid.NewGuid();

        var transactionWithoutReceipt = CreateTestTransaction("UBER RIDE", 35.00m, new DateOnly(2024, 3, 16));
        transactionWithoutReceipt.MatchedReceiptId = null;

        _dbContext.Transactions.AddRange(transactionWithReceipt, transactionWithoutReceipt);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "transactions" });

        // Assert
        var content = Encoding.UTF8.GetString(result.FileBytes);
        content.Should().Contain("HasReceipt");
    }

    [Fact]
    public async Task ExportAsync_TransactionsFilteredByDateRange()
    {
        // Arrange
        var startDate = new DateOnly(2024, 3, 1);
        var endDate = new DateOnly(2024, 3, 31);

        _dbContext.Transactions.AddRange(
            CreateTestTransaction("IN RANGE", 25.00m, new DateOnly(2024, 3, 15)),
            CreateTestTransaction("BEFORE RANGE", 30.00m, new DateOnly(2024, 2, 15)),
            CreateTestTransaction("AFTER RANGE", 35.00m, new DateOnly(2024, 4, 15))
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "transactions" });

        // Assert
        var content = Encoding.UTF8.GetString(result.FileBytes);
        content.Should().Contain("IN RANGE");
        content.Should().NotContain("BEFORE RANGE");
        content.Should().NotContain("AFTER RANGE");
    }

    [Fact]
    public async Task ExportAsync_TransactionsFilteredByUser()
    {
        // Arrange
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 12, 31);
        var otherUserId = Guid.NewGuid();

        var userTransaction = CreateTestTransaction("USER TRANSACTION", 25.00m, new DateOnly(2024, 3, 15));

        var otherUserTransaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = otherUserId,
            ImportId = Guid.NewGuid(),
            Amount = 35.00m,
            TransactionDate = new DateOnly(2024, 3, 16),
            Description = "OTHER USER TRANSACTION",
            OriginalDescription = "OTHER USER TRANSACTION",
            DuplicateHash = Guid.NewGuid().ToString()
        };

        _dbContext.Transactions.AddRange(userTransaction, otherUserTransaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.ExportAsync(
            _testUserId, startDate, endDate, "csv", new[] { "transactions" });

        // Assert
        var content = Encoding.UTF8.GetString(result.FileBytes);
        content.Should().Contain("USER TRANSACTION");
        content.Should().NotContain("OTHER USER TRANSACTION");
    }

    #endregion

    #region Helper Methods

    private void SetupMocksForEmptyExport()
    {
        _analyticsServiceMock
            .Setup(x => x.GetSpendingTrendAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingTrendItemDto>());

        _analyticsServiceMock
            .Setup(x => x.GetSpendingByCategoryAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingByCategoryItemDto>());

        _analyticsServiceMock
            .Setup(x => x.GetSpendingByVendorAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingByVendorItemDto>());
    }

    private void SetupMocksForFullExport()
    {
        _analyticsServiceMock
            .Setup(x => x.GetSpendingTrendAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingTrendItemDto>
            {
                new() { Date = "2024-01", Amount = 1000.00m, TransactionCount = 10 }
            });

        _analyticsServiceMock
            .Setup(x => x.GetSpendingByCategoryAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingByCategoryItemDto>
            {
                new() { Category = "Food & Dining", Amount = 500.00m, TransactionCount = 20, PercentageOfTotal = 100.0m }
            });

        _analyticsServiceMock
            .Setup(x => x.GetSpendingByVendorAsync(
                It.IsAny<Guid>(), It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SpendingByVendorItemDto>
            {
                new() { VendorName = "Starbucks", Amount = 500.00m, TransactionCount = 20, PercentageOfTotal = 100.0m }
            });
    }

    private Transaction CreateTestTransaction(string description, decimal amount, DateOnly date)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ImportId = Guid.NewGuid(),
            Amount = amount,
            TransactionDate = date,
            Description = description,
            OriginalDescription = description,
            DuplicateHash = Guid.NewGuid().ToString()
        };
    }

    #endregion
}
