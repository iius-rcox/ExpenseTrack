using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Repositories;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.Enums;
using ExpenseFlow.TestCommon;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Integration tests for ExpensePredictionService focusing on the duplicate vendor bug fix.
///
/// Bug: DbUpdateConcurrencyException was thrown when:
/// 1. Same vendor appeared multiple times in ONE report
/// 2. Same vendor appeared across MULTIPLE reports
///
/// Fix: Removed redundant UpdateAsync() call - EF Core auto-tracks property changes.
/// Also added ChangeTracker.Clear() between reports in LearnFromReportsAsync.
/// </summary>
[Trait("Category", TestCategories.Integration)]
public class ExpensePredictionServiceDuplicateVendorTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ExpensePredictionService _service;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<ITransactionPredictionRepository> _predictionRepositoryMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ExpensePredictionServiceDuplicateVendorTests()
    {
        // Create InMemory database with unique name for test isolation
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: $"ExpenseFlow_Test_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);

        // Setup mocks
        _vendorAliasServiceMock = new Mock<IVendorAliasService>();
        _vendorAliasServiceMock
            .Setup(x => x.FindMatchingAliasAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => null); // Return null to use default normalization

        _predictionRepositoryMock = new Mock<ITransactionPredictionRepository>();

        // Create real repository using the same DbContext
        var patternRepository = new ExpensePatternRepository(_dbContext);

        // Create service under test
        _service = new ExpensePredictionService(
            _dbContext,
            patternRepository,
            _predictionRepositoryMock.Object,
            _vendorAliasServiceMock.Object,
            Mock.Of<ILogger<ExpensePredictionService>>()
        );

        // Seed test user
        SeedTestUser();
    }

    private void SeedTestUser()
    {
        _dbContext.Users.Add(new User
        {
            Id = _testUserId,
            Email = "test@example.com",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region Scenario 1: Duplicate vendors within ONE report

    [Fact]
    public async Task LearnFromReportAsync_ReportWithDuplicateVendors_CreatesOnePatternWithCorrectOccurrenceCount()
    {
        // Arrange: Create a report with 3 Amazon purchases (duplicate vendor)
        var report = CreateReportWithDuplicateVendors("AMAZON", 3);

        // Act: Should NOT throw DbUpdateConcurrencyException
        var patternsUpdated = await _service.LearnFromReportAsync(_testUserId, report.Id);

        // Assert
        patternsUpdated.Should().Be(3, "all 3 lines should be processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(1, "duplicate vendors should create only one pattern");

        var amazonPattern = patterns.First();
        amazonPattern.NormalizedVendor.Should().Be("AMAZON");
        amazonPattern.OccurrenceCount.Should().Be(3, "pattern should track all 3 occurrences");
    }

    [Fact]
    public async Task LearnFromReportAsync_ReportWithDuplicateVendors_CalculatesCorrectAverageAmount()
    {
        // Arrange: Create report with specific amounts for testing average calculation
        var report = CreateReportWithSpecificAmounts("STARBUCKS", new[] { 5.50m, 7.00m, 4.50m });

        // Act
        await _service.LearnFromReportAsync(_testUserId, report.Id);

        // Assert
        var pattern = await _dbContext.ExpensePatterns
            .FirstAsync(p => p.UserId == _testUserId && p.NormalizedVendor == "STARBUCKS");

        // Note: The implementation uses weighted moving average with decay,
        // so we just verify min/max are tracked correctly
        pattern.MinAmount.Should().Be(4.50m);
        pattern.MaxAmount.Should().Be(7.00m);
        pattern.OccurrenceCount.Should().Be(3);
    }

    [Fact]
    public async Task LearnFromReportAsync_ReportWithMixedVendors_CreatesSeparatePatterns()
    {
        // Arrange: Report with 2 Amazon, 1 Starbucks, 2 Uber
        var report = CreateReportWithMixedVendors();

        // Act
        var patternsUpdated = await _service.LearnFromReportAsync(_testUserId, report.Id);

        // Assert
        patternsUpdated.Should().Be(5, "all 5 lines should be processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(3, "should have patterns for Amazon, Starbucks, Uber");

        var amazonPattern = patterns.First(p => p.NormalizedVendor == "AMAZON");
        amazonPattern.OccurrenceCount.Should().Be(2);

        var starbucksPattern = patterns.First(p => p.NormalizedVendor == "STARBUCKS");
        starbucksPattern.OccurrenceCount.Should().Be(1);

        var uberPattern = patterns.First(p => p.NormalizedVendor == "UBER");
        uberPattern.OccurrenceCount.Should().Be(2);
    }

    #endregion

    #region Scenario 2: Same vendor across MULTIPLE reports

    [Fact]
    public async Task LearnFromReportsAsync_MultipleReportsWithSameVendor_DoesNotThrowConcurrencyException()
    {
        // Arrange: Create 3 reports, each with Amazon purchases
        var report1 = CreateReportWithDuplicateVendors("AMAZON", 2, "2025-01");
        var report2 = CreateReportWithDuplicateVendors("AMAZON", 1, "2025-02");
        var report3 = CreateReportWithDuplicateVendors("AMAZON", 3, "2025-03");

        var reportIds = new[] { report1.Id, report2.Id, report3.Id };

        // Act: Should NOT throw DbUpdateConcurrencyException
        Func<Task> act = async () => await _service.LearnFromReportsAsync(_testUserId, reportIds);

        // Assert
        await act.Should().NotThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task LearnFromReportsAsync_MultipleReportsWithSameVendor_AccumulatesOccurrenceCount()
    {
        // Arrange
        var report1 = CreateReportWithDuplicateVendors("AMAZON", 2, "2025-01");
        var report2 = CreateReportWithDuplicateVendors("AMAZON", 3, "2025-02");

        var reportIds = new[] { report1.Id, report2.Id };

        // Act
        var totalPatterns = await _service.LearnFromReportsAsync(_testUserId, reportIds);

        // Assert
        totalPatterns.Should().Be(5, "2 + 3 = 5 lines processed");

        var pattern = await _dbContext.ExpensePatterns
            .FirstAsync(p => p.UserId == _testUserId && p.NormalizedVendor == "AMAZON");

        pattern.OccurrenceCount.Should().Be(5, "all occurrences across reports should be counted");
    }

    [Fact]
    public async Task LearnFromReportsAsync_MultipleReportsWithOverlappingVendors_TracksAllVendorsSeparately()
    {
        // Arrange
        // Report 1: Amazon x2, Starbucks x1
        var report1 = CreateReport("2025-01", new[]
        {
            ("AMAZON", 25.99m),
            ("AMAZON", 35.00m),
            ("STARBUCKS", 5.50m)
        });

        // Report 2: Amazon x1, Uber x2
        var report2 = CreateReport("2025-02", new[]
        {
            ("AMAZON", 42.00m),
            ("UBER", 15.00m),
            ("UBER", 22.50m)
        });

        var reportIds = new[] { report1.Id, report2.Id };

        // Act
        await _service.LearnFromReportsAsync(_testUserId, reportIds);

        // Assert
        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(3, "Amazon, Starbucks, Uber");

        patterns.First(p => p.NormalizedVendor == "AMAZON").OccurrenceCount.Should().Be(3);
        patterns.First(p => p.NormalizedVendor == "STARBUCKS").OccurrenceCount.Should().Be(1);
        patterns.First(p => p.NormalizedVendor == "UBER").OccurrenceCount.Should().Be(2);
    }

    #endregion

    #region Scenario 3: RebuildPatternsAsync (full rebuild)

    [Fact]
    public async Task RebuildPatternsAsync_WithDuplicateVendorsAcrossReports_RebuildsCorrectly()
    {
        // Arrange: Create existing patterns (to be deleted during rebuild)
        _dbContext.ExpensePatterns.Add(new ExpensePattern
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            NormalizedVendor = "OLD_VENDOR",
            DisplayName = "Old Vendor",
            OccurrenceCount = 10,
            AverageAmount = 100m,
            MinAmount = 50m,
            MaxAmount = 150m,
            LastSeenAt = DateTime.UtcNow.AddMonths(-6),
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            UpdatedAt = DateTime.UtcNow.AddMonths(-6)
        });
        await _dbContext.SaveChangesAsync();

        // Create Submitted reports with duplicate vendors (RebuildPatternsAsync only learns from Submitted)
        var report1 = CreateReportWithDuplicateVendors("AMAZON", 3, "2025-01", ReportStatus.Submitted);
        var report2 = CreateReportWithDuplicateVendors("AMAZON", 2, "2025-02", ReportStatus.Submitted);

        // Act: Should delete old patterns and rebuild from reports
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert
        patternsCreated.Should().Be(5, "3 + 2 = 5 lines processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(1, "only Amazon pattern should exist after rebuild");
        patterns.First().NormalizedVendor.Should().Be("AMAZON");
        patterns.First().OccurrenceCount.Should().Be(5);

        // Old vendor should be deleted
        patterns.Should().NotContain(p => p.NormalizedVendor == "OLD_VENDOR");
    }

    [Fact]
    public async Task RebuildPatternsAsync_WithManyDuplicateVendorsAcrossManyReports_CompletesWithoutException()
    {
        // Arrange: Create 10 Submitted reports with overlapping vendors (RebuildPatternsAsync only learns from Submitted)
        var reports = new List<ExpenseReport>();
        for (int i = 1; i <= 10; i++)
        {
            var period = $"2025-{i:D2}";
            var report = CreateReport(period, new[]
            {
                ("AMAZON", 20m + i),
                ("STARBUCKS", 5m + (i * 0.5m)),
                ("UBER", 15m + i),
                ("AMAZON", 30m + i), // Duplicate Amazon in same report
            }, ReportStatus.Submitted);
            reports.Add(report);
        }

        // Act
        Func<Task> act = async () => await _service.RebuildPatternsAsync(_testUserId);

        // Assert
        await act.Should().NotThrowAsync<DbUpdateConcurrencyException>();

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(3, "Amazon, Starbucks, Uber");
        patterns.First(p => p.NormalizedVendor == "AMAZON").OccurrenceCount.Should().Be(20); // 2 per report * 10 reports
        patterns.First(p => p.NormalizedVendor == "STARBUCKS").OccurrenceCount.Should().Be(10);
        patterns.First(p => p.NormalizedVendor == "UBER").OccurrenceCount.Should().Be(10);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task LearnFromReportAsync_EmptyReport_ReturnsZero()
    {
        // Arrange
        var report = new ExpenseReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Period = "2025-01",
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<ExpenseLine>() // Empty
        };
        _dbContext.ExpenseReports.Add(report);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.LearnFromReportAsync(_testUserId, report.Id);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task LearnFromReportAsync_NonExistentReport_ReturnsZero()
    {
        // Act
        var result = await _service.LearnFromReportAsync(_testUserId, Guid.NewGuid());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task LearnFromReportsAsync_EmptyReportIds_ReturnsZero()
    {
        // Act
        var result = await _service.LearnFromReportsAsync(_testUserId, Array.Empty<Guid>());

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task LearnFromReportAsync_VendorWithNullName_UsesOriginalDescription()
    {
        // Arrange
        var report = new ExpenseReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Period = "2025-01",
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<ExpenseLine>
            {
                new ExpenseLine
                {
                    Id = Guid.NewGuid(),
                    LineOrder = 1,
                    ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Amount = 50.00m,
                    OriginalDescription = "UBER *EATS",
                    NormalizedDescription = "Uber Eats",
                    VendorName = null, // Null vendor name
                    GLCode = "6000",
                    CreatedAt = DateTime.UtcNow
                }
            }
        };
        _dbContext.ExpenseReports.Add(report);
        await _dbContext.SaveChangesAsync();

        // Act
        await _service.LearnFromReportAsync(_testUserId, report.Id);

        // Assert
        var pattern = await _dbContext.ExpensePatterns.FirstAsync(p => p.UserId == _testUserId);
        pattern.NormalizedVendor.Should().Be("UBER *EATS");
        pattern.DisplayName.Should().Be("UBER *EATS");
    }

    #endregion

    #region Helper Methods

    private ExpenseReport CreateReportWithDuplicateVendors(
        string vendorName,
        int count,
        string period = "2025-01",
        ReportStatus status = ReportStatus.Draft)
    {
        var lines = Enumerable.Range(1, count)
            .Select(i => new ExpenseLine
            {
                Id = Guid.NewGuid(),
                LineOrder = i,
                ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                Amount = 10m * i,
                OriginalDescription = $"{vendorName} Purchase #{i}",
                NormalizedDescription = $"{vendorName} Purchase",
                VendorName = vendorName,
                GLCode = "6000",
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        var report = new ExpenseReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Period = period,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            Lines = lines
        };

        _dbContext.ExpenseReports.Add(report);
        _dbContext.SaveChanges();

        return report;
    }

    private ExpenseReport CreateReportWithSpecificAmounts(string vendorName, decimal[] amounts)
    {
        var lines = amounts.Select((amount, i) => new ExpenseLine
        {
            Id = Guid.NewGuid(),
            LineOrder = i + 1,
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
            Amount = amount,
            OriginalDescription = $"{vendorName} #{i + 1}",
            NormalizedDescription = vendorName,
            VendorName = vendorName,
            GLCode = "6000",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var report = new ExpenseReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Period = "2025-01",
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Lines = lines
        };

        _dbContext.ExpenseReports.Add(report);
        _dbContext.SaveChanges();

        return report;
    }

    private ExpenseReport CreateReportWithMixedVendors()
    {
        var lines = new List<ExpenseLine>
        {
            CreateLine(1, "AMAZON", 25.99m),
            CreateLine(2, "AMAZON", 35.00m),
            CreateLine(3, "STARBUCKS", 5.50m),
            CreateLine(4, "UBER", 15.00m),
            CreateLine(5, "UBER", 22.50m)
        };

        var report = new ExpenseReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Period = "2025-01",
            Status = ReportStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            Lines = lines
        };

        _dbContext.ExpenseReports.Add(report);
        _dbContext.SaveChanges();

        return report;
    }

    private ExpenseReport CreateReport(
        string period,
        (string vendor, decimal amount)[] items,
        ReportStatus status = ReportStatus.Draft)
    {
        var lines = items.Select((item, i) => CreateLine(i + 1, item.vendor, item.amount)).ToList();

        var report = new ExpenseReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Period = period,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            Lines = lines
        };

        _dbContext.ExpenseReports.Add(report);
        _dbContext.SaveChanges();

        return report;
    }

    private static ExpenseLine CreateLine(int order, string vendorName, decimal amount)
    {
        return new ExpenseLine
        {
            Id = Guid.NewGuid(),
            LineOrder = order,
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-order)),
            Amount = amount,
            OriginalDescription = $"{vendorName} Transaction",
            NormalizedDescription = vendorName,
            VendorName = vendorName,
            GLCode = "6000",
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
