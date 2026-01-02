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

public class AnalyticsControllerTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly Mock<IComparisonService> _comparisonServiceMock;
    private readonly Mock<ICacheStatisticsService> _cacheStatisticsServiceMock;
    private readonly Mock<IAnalyticsService> _analyticsServiceMock;
    private readonly Mock<IAnalyticsExportService> _analyticsExportServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<AnalyticsController>> _loggerMock;
    private readonly AnalyticsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public AnalyticsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _comparisonServiceMock = new Mock<IComparisonService>();
        _cacheStatisticsServiceMock = new Mock<ICacheStatisticsService>();
        _analyticsServiceMock = new Mock<IAnalyticsService>();
        _analyticsExportServiceMock = new Mock<IAnalyticsExportService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<AnalyticsController>>();

        _controller = new AnalyticsController(
            _comparisonServiceMock.Object,
            _cacheStatisticsServiceMock.Object,
            _analyticsServiceMock.Object,
            _analyticsExportServiceMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object,
            _dbContext);

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

    #region GetCategories Tests

    [Fact]
    public async Task GetCategories_NoTransactions_ReturnsEmptyBreakdown()
    {
        // Act
        var result = await _controller.GetCategories(null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var breakdown = okResult.Value.Should().BeOfType<CategoryBreakdownDto>().Subject;
        breakdown.TotalSpending.Should().Be(0);
        breakdown.TransactionCount.Should().Be(0);
        breakdown.Categories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetCategories_WithTransactions_ReturnsCorrectBreakdown()
    {
        // Arrange
        var currentMonth = DateTime.UtcNow;
        var period = $"{currentMonth:yyyy-MM}";

        // Use descriptions that will be categorized by DeriveCategory
        _dbContext.Transactions.AddRange(
            CreateTransactionWithDescription("STARBUCKS #1234", 50m, currentMonth),
            CreateTransactionWithDescription("CHIPOTLE DALLAS", 30m, currentMonth),
            CreateTransactionWithDescription("UBER TRIP", 100m, currentMonth),
            CreateTransactionWithDescription("NETFLIX.COM", 25m, currentMonth)
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories(period, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var breakdown = okResult.Value.Should().BeOfType<CategoryBreakdownDto>().Subject;

        breakdown.Period.Should().Be(period);
        breakdown.TotalSpending.Should().Be(205m);
        breakdown.TransactionCount.Should().Be(4);
        breakdown.Categories.Should().HaveCount(3); // Transportation, Food & Dining, Entertainment

        // Verify categories are ordered by amount descending
        breakdown.Categories[0].Category.Should().Be("Transportation");
        breakdown.Categories[0].Amount.Should().Be(100m);

        breakdown.Categories[1].Category.Should().Be("Food & Dining");
        breakdown.Categories[1].Amount.Should().Be(80m);
        breakdown.Categories[1].TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task GetCategories_CalculatesPercentagesCorrectly()
    {
        // Arrange
        var currentMonth = DateTime.UtcNow;
        var period = $"{currentMonth:yyyy-MM}";

        _dbContext.Transactions.AddRange(
            CreateTransactionWithDescription("STARBUCKS COFFEE", 50m, currentMonth),
            CreateTransactionWithDescription("UBER RIDE", 50m, currentMonth)
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories(period, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var breakdown = okResult.Value.Should().BeOfType<CategoryBreakdownDto>().Subject;

        breakdown.Categories.Should().HaveCount(2);
        breakdown.Categories.Should().AllSatisfy(c => c.Percentage.Should().Be(50m));
    }

    [Fact]
    public async Task GetCategories_NullPeriod_DefaultsToCurrentMonth()
    {
        // Arrange
        var currentMonth = DateTime.UtcNow;
        var expectedPeriod = $"{currentMonth:yyyy-MM}";

        _dbContext.Transactions.Add(CreateTransactionWithDescription("STARBUCKS", 50m, currentMonth));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories(null, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var breakdown = okResult.Value.Should().BeOfType<CategoryBreakdownDto>().Subject;

        breakdown.Period.Should().Be(expectedPeriod);
        breakdown.TotalSpending.Should().Be(50m);
    }

    [Fact]
    public async Task GetCategories_InvalidPeriodFormat_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetCategories("invalid-period", CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetCategories_OnlyIncludesUserTransactions()
    {
        // Arrange
        var currentMonth = DateTime.UtcNow;
        var period = $"{currentMonth:yyyy-MM}";
        var otherUserId = Guid.NewGuid();

        // Add user's transaction
        _dbContext.Transactions.Add(CreateTransactionWithDescription("STARBUCKS", 50m, currentMonth));

        // Add another user's transaction (uses different user ID)
        var otherUserTransaction = CreateTransactionWithDescription("NETFLIX.COM", 200m, currentMonth);
        otherUserTransaction.UserId = otherUserId;
        _dbContext.Transactions.Add(otherUserTransaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories(period, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var breakdown = okResult.Value.Should().BeOfType<CategoryBreakdownDto>().Subject;

        breakdown.TotalSpending.Should().Be(50m);
        breakdown.Categories.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCategories_UnknownVendor_GroupsAsOther()
    {
        // Arrange
        var currentMonth = DateTime.UtcNow;
        var period = $"{currentMonth:yyyy-MM}";

        // Transaction with description that doesn't match any category patterns
        _dbContext.Transactions.Add(CreateTransactionWithDescription("UNKNOWN VENDOR XYZ", 50m, currentMonth));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories(period, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var breakdown = okResult.Value.Should().BeOfType<CategoryBreakdownDto>().Subject;

        breakdown.Categories.Should().HaveCount(1);
        breakdown.Categories[0].Category.Should().Be("Other");
    }

    [Fact]
    public async Task GetCategories_SpecificPeriod_FiltersCorrectly()
    {
        // Arrange
        var targetMonth = new DateTime(2025, 6, 15);
        var otherMonth = new DateTime(2025, 5, 15);
        var period = "2025-06";

        _dbContext.Transactions.AddRange(
            CreateTransactionWithDescription("STARBUCKS", 50m, targetMonth),
            CreateTransactionWithDescription("STARBUCKS", 100m, otherMonth)
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetCategories(period, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var breakdown = okResult.Value.Should().BeOfType<CategoryBreakdownDto>().Subject;

        breakdown.Period.Should().Be("2025-06");
        breakdown.TotalSpending.Should().Be(50m);
    }

    #endregion

    #region ExportAnalytics Tests

    [Fact]
    public async Task ExportAnalytics_CsvFormat_ReturnsCsvFile()
    {
        // Arrange
        var request = new AnalyticsExportRequestDto
        {
            StartDate = "2024-01-01",
            EndDate = "2024-06-30",
            Format = "csv",
            Sections = "trends,categories"
        };

        var csvContent = "Test CSV Content"u8.ToArray();
        _analyticsExportServiceMock
            .Setup(x => x.ExportAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                "csv",
                It.Is<IReadOnlyList<string>>(s => s.Contains("trends") && s.Contains("categories")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((csvContent, "text/csv", "Analytics_20240101_20240630.csv"));

        // Act
        var result = await _controller.ExportAnalytics(request, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("text/csv");
        fileResult.FileDownloadName.Should().Be("Analytics_20240101_20240630.csv");
    }

    [Fact]
    public async Task ExportAnalytics_ExcelFormat_ReturnsExcelFile()
    {
        // Arrange
        var request = new AnalyticsExportRequestDto
        {
            StartDate = "2024-01-01",
            EndDate = "2024-12-31",
            Format = "xlsx",
            Sections = "trends,categories,vendors"
        };

        var excelContent = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // XLSX magic bytes
        _analyticsExportServiceMock
            .Setup(x => x.ExportAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                "xlsx",
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((excelContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Analytics_20240101_20241231.xlsx"));

        // Act
        var result = await _controller.ExportAnalytics(request, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        fileResult.FileDownloadName.Should().EndWith(".xlsx");
    }

    [Fact]
    public async Task ExportAnalytics_DateRangeExceeds5Years_ReturnsBadRequest()
    {
        // Arrange
        var request = new AnalyticsExportRequestDto
        {
            StartDate = "2019-01-01",
            EndDate = "2025-06-01", // > 5 years
            Format = "csv",
            Sections = "trends"
        };

        _analyticsExportServiceMock
            .Setup(x => x.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Date range exceeds maximum of 5 years"));

        // Act
        var result = await _controller.ExportAnalytics(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExportAnalytics_StartDateAfterEndDate_ReturnsBadRequest()
    {
        // Arrange
        var request = new AnalyticsExportRequestDto
        {
            StartDate = "2024-12-01",
            EndDate = "2024-01-01", // Before start
            Format = "csv",
            Sections = "trends"
        };

        _analyticsExportServiceMock
            .Setup(x => x.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Start date must be before or equal to end date"));

        // Act
        var result = await _controller.ExportAnalytics(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ExportAnalytics_DefaultSections_UsesAllSections()
    {
        // Arrange
        var request = new AnalyticsExportRequestDto
        {
            StartDate = "2024-01-01",
            EndDate = "2024-06-30",
            Format = "csv",
            Sections = null // Default
        };

        _analyticsExportServiceMock
            .Setup(x => x.ExportAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                "csv",
                It.Is<IReadOnlyList<string>>(s => s.Count == 3), // Default sections: trends, categories, vendors
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new byte[10], "text/csv", "Analytics.csv"));

        // Act
        await _controller.ExportAnalytics(request, CancellationToken.None);

        // Assert
        _analyticsExportServiceMock.Verify(
            x => x.ExportAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                "csv",
                It.Is<IReadOnlyList<string>>(s => s.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExportAnalytics_IncludesContentDispositionHeader()
    {
        // Arrange
        var request = new AnalyticsExportRequestDto
        {
            StartDate = "2024-01-01",
            EndDate = "2024-06-30",
            Format = "csv",
            Sections = "trends"
        };

        _analyticsExportServiceMock
            .Setup(x => x.ExportAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new byte[10], "text/csv", "Analytics_20240101_20240630.csv"));

        // Act
        var result = await _controller.ExportAnalytics(request, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileDownloadName.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExportAnalytics_TransactionsSection_IncludesTransactionData()
    {
        // Arrange
        var request = new AnalyticsExportRequestDto
        {
            StartDate = "2024-01-01",
            EndDate = "2024-06-30",
            Format = "csv",
            Sections = "transactions"
        };

        _analyticsExportServiceMock
            .Setup(x => x.ExportAsync(
                _testUserId,
                It.IsAny<DateOnly>(),
                It.IsAny<DateOnly>(),
                "csv",
                It.Is<IReadOnlyList<string>>(s => s.Contains("transactions")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new byte[100], "text/csv", "Analytics_20240101_20240630.csv"));

        // Act
        var result = await _controller.ExportAnalytics(request, CancellationToken.None);

        // Assert
        var fileResult = result.Should().BeOfType<FileContentResult>().Subject;
        fileResult.FileContents.Length.Should().BeGreaterThan(0);
    }

    #endregion

    #region Helper Methods

    private Transaction CreateTransactionWithDescription(string description, decimal amount, DateTime date)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ImportId = Guid.NewGuid(),
            Amount = amount,
            TransactionDate = DateOnly.FromDateTime(date),
            Description = description,
            OriginalDescription = description,
            DuplicateHash = Guid.NewGuid().ToString()
        };
    }

    #endregion
}
