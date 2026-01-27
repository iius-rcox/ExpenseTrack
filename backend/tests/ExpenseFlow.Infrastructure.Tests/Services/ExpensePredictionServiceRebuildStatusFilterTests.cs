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
/// Integration tests for ExpensePredictionService.RebuildPatternsAsync focusing on
/// the soft-delete filter bug.
///
/// Bug: RebuildPatternsAsync was including soft-deleted reports when it should
/// exclude them. The fix adds !IsDeleted filter to exclude deleted reports.
///
/// Expected Behavior: Includes Draft, Generated, and Submitted reports (all statuses)
/// but EXCLUDES soft-deleted reports (IsDeleted = true).
/// </summary>
[Trait("Category", TestCategories.Integration)]
public class ExpensePredictionServiceRebuildStatusFilterTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ExpensePredictionService _service;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<ITransactionPredictionRepository> _predictionRepositoryMock;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ExpensePredictionServiceRebuildStatusFilterTests()
    {
        // Create InMemory database with unique name for test isolation
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: $"ExpenseFlow_StatusFilter_Test_{Guid.NewGuid()}")
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

    #region RebuildPatternsAsync Status Filter Tests - All Statuses Allowed

    [Fact]
    public async Task RebuildPatternsAsync_LearnsFromDraftReports()
    {
        // Arrange: Create a Draft report
        var draftReport = CreateReport("2025-01", ReportStatus.Draft, new[]
        {
            ("DRAFT_VENDOR", 100.00m),
            ("DRAFT_VENDOR", 150.00m)
        });

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert: Should learn from Draft report
        patternsCreated.Should().Be(2, "both lines from Draft report should be processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(1, "DRAFT_VENDOR pattern should exist");
        patterns.First().NormalizedVendor.Should().Be("DRAFT_VENDOR");
        patterns.First().OccurrenceCount.Should().Be(2);
    }

    [Fact]
    public async Task RebuildPatternsAsync_LearnsFromGeneratedReports()
    {
        // Arrange: Create a Generated report
        var generatedReport = CreateReport("2025-01", ReportStatus.Generated, new[]
        {
            ("GENERATED_VENDOR", 100.00m),
            ("GENERATED_VENDOR", 150.00m),
            ("GENERATED_VENDOR", 175.00m)
        });

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert: Should learn from Generated report
        patternsCreated.Should().Be(3, "all 3 lines from Generated report should be processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(1, "GENERATED_VENDOR pattern should exist");
        patterns.First().NormalizedVendor.Should().Be("GENERATED_VENDOR");
        patterns.First().OccurrenceCount.Should().Be(3);
    }

    [Fact]
    public async Task RebuildPatternsAsync_LearnsFromSubmittedReports()
    {
        // Arrange: Create a Submitted report
        var submittedReport = CreateReport("2025-01", ReportStatus.Submitted, new[]
        {
            ("SUBMITTED_VENDOR", 200.00m),
            ("SUBMITTED_VENDOR", 250.00m)
        });

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert: Should learn from Submitted report
        patternsCreated.Should().Be(2, "both lines from Submitted report should be processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(1, "SUBMITTED_VENDOR pattern should exist");
        patterns.First().NormalizedVendor.Should().Be("SUBMITTED_VENDOR");
        patterns.First().OccurrenceCount.Should().Be(2);
    }

    [Fact]
    public async Task RebuildPatternsAsync_LearnsFromAllStatuses()
    {
        // Arrange: Create reports with all three statuses
        var draftReport = CreateReport("2025-01", ReportStatus.Draft, new[]
        {
            ("DRAFT_AMAZON", 100.00m)
        });

        var generatedReport = CreateReport("2025-02", ReportStatus.Generated, new[]
        {
            ("GENERATED_UBER", 35.00m)
        });

        var submittedReport = CreateReport("2025-03", ReportStatus.Submitted, new[]
        {
            ("SUBMITTED_HOTEL", 250.00m)
        });

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert: Should learn from ALL statuses
        patternsCreated.Should().Be(3, "all 3 lines from all statuses should be processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(3, "one pattern for each vendor from each status");
        patterns.Should().Contain(p => p.NormalizedVendor == "DRAFT_AMAZON");
        patterns.Should().Contain(p => p.NormalizedVendor == "GENERATED_UBER");
        patterns.Should().Contain(p => p.NormalizedVendor == "SUBMITTED_HOTEL");
    }

    #endregion

    #region RebuildPatternsAsync Soft-Delete Filter Tests

    [Fact]
    public async Task RebuildPatternsAsync_ExcludesDeletedReports()
    {
        // Arrange: Create a deleted report and an active report
        var deletedReport = CreateReport("2025-01", ReportStatus.Submitted, new[]
        {
            ("DELETED_VENDOR", 100.00m),
            ("DELETED_VENDOR", 150.00m)
        }, isDeleted: true);

        var activeReport = CreateReport("2025-02", ReportStatus.Submitted, new[]
        {
            ("ACTIVE_VENDOR", 200.00m)
        });

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert: Should only learn from active report
        patternsCreated.Should().Be(1, "only the 1 line from active report should be processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(1, "only ACTIVE_VENDOR pattern should exist");
        patterns.First().NormalizedVendor.Should().Be("ACTIVE_VENDOR");

        // Verify deleted report vendor is NOT in patterns
        patterns.Should().NotContain(p => p.NormalizedVendor == "DELETED_VENDOR",
            "deleted reports should NOT be used for pattern learning");
    }

    [Fact]
    public async Task RebuildPatternsAsync_UserWithOnlyDeletedReports_ReturnsZeroPatterns()
    {
        // Arrange: Create multiple deleted reports only
        var deleted1 = CreateReport("2025-01", ReportStatus.Submitted, new[]
        {
            ("AMAZON", 50.00m),
            ("STARBUCKS", 5.50m)
        }, isDeleted: true);

        var deleted2 = CreateReport("2025-02", ReportStatus.Draft, new[]
        {
            ("UBER", 25.00m),
            ("AMAZON", 75.00m)
        }, isDeleted: true);

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert: No patterns should be created from deleted reports
        patternsCreated.Should().Be(0, "no patterns should be created from deleted reports");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().BeEmpty("deleted reports should not generate any patterns");
    }

    [Fact]
    public async Task RebuildPatternsAsync_MixOfDeletedAndActiveReports_OnlyLearnsFromActive()
    {
        // Arrange: Create mix of deleted and active reports across all statuses
        var deletedDraft = CreateReport("2025-01", ReportStatus.Draft, new[]
        {
            ("DELETED_DRAFT_VENDOR", 100.00m)
        }, isDeleted: true);

        var deletedSubmitted = CreateReport("2025-02", ReportStatus.Submitted, new[]
        {
            ("DELETED_SUBMITTED_VENDOR", 200.00m)
        }, isDeleted: true);

        var activeDraft = CreateReport("2025-03", ReportStatus.Draft, new[]
        {
            ("ACTIVE_DRAFT_VENDOR", 300.00m)
        });

        var activeGenerated = CreateReport("2025-04", ReportStatus.Generated, new[]
        {
            ("ACTIVE_GENERATED_VENDOR", 400.00m)
        });

        var activeSubmitted = CreateReport("2025-05", ReportStatus.Submitted, new[]
        {
            ("ACTIVE_SUBMITTED_VENDOR", 500.00m)
        });

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert: Should only learn from the 3 active reports
        patternsCreated.Should().Be(3, "only the 3 lines from active reports should be processed");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(3);
        patterns.Should().Contain(p => p.NormalizedVendor == "ACTIVE_DRAFT_VENDOR");
        patterns.Should().Contain(p => p.NormalizedVendor == "ACTIVE_GENERATED_VENDOR");
        patterns.Should().Contain(p => p.NormalizedVendor == "ACTIVE_SUBMITTED_VENDOR");

        // Verify deleted vendors are NOT present
        patterns.Should().NotContain(p => p.NormalizedVendor == "DELETED_DRAFT_VENDOR");
        patterns.Should().NotContain(p => p.NormalizedVendor == "DELETED_SUBMITTED_VENDOR");
    }

    [Fact]
    public async Task LearnFromReportAsync_DeletedReport_ReturnsZeroAndDoesNotLearn()
    {
        // Arrange: Create a deleted report
        var deletedReport = CreateReport("2025-01", ReportStatus.Submitted, new[]
        {
            ("DELETED_VENDOR", 100.00m),
            ("DELETED_VENDOR", 150.00m)
        }, isDeleted: true);

        // Act: Directly call LearnFromReportAsync with deleted report ID
        var patternsCreated = await _service.LearnFromReportAsync(_testUserId, deletedReport.Id);

        // Assert: Should return 0 and not create any patterns
        patternsCreated.Should().Be(0, "deleted reports should not be learned from");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().BeEmpty("no patterns should be created from deleted reports");
    }

    [Fact]
    public async Task LearnFromReportAsync_ActiveReport_LearnsNormally()
    {
        // Arrange: Create an active report
        var activeReport = CreateReport("2025-01", ReportStatus.Draft, new[]
        {
            ("ACTIVE_VENDOR", 100.00m)
        });

        // Act: Directly call LearnFromReportAsync
        var patternsCreated = await _service.LearnFromReportAsync(_testUserId, activeReport.Id);

        // Assert: Should learn from the active report
        patternsCreated.Should().Be(1, "active report should be learned from");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(1);
        patterns.First().NormalizedVendor.Should().Be("ACTIVE_VENDOR");
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task RebuildPatternsAsync_NoReports_ReturnsZeroPatterns()
    {
        // Arrange: User has no reports at all

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert
        patternsCreated.Should().Be(0, "no reports means no patterns");

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().BeEmpty();
    }

    [Fact]
    public async Task RebuildPatternsAsync_MultipleActiveReports_LearnsFromAll()
    {
        // Arrange: Create multiple active reports across all statuses
        var draft = CreateReport("2025-01", ReportStatus.Draft, new[]
        {
            ("AMAZON", 100.00m),
            ("AMAZON", 150.00m)
        });

        var generated = CreateReport("2025-02", ReportStatus.Generated, new[]
        {
            ("AMAZON", 200.00m),
            ("STARBUCKS", 5.50m)
        });

        var submitted = CreateReport("2025-03", ReportStatus.Submitted, new[]
        {
            ("UBER", 35.00m),
            ("AMAZON", 175.00m)
        });

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert: Should learn from all 6 lines across 3 reports
        patternsCreated.Should().Be(6);

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(3, "Amazon, Starbucks, Uber");

        var amazonPattern = patterns.First(p => p.NormalizedVendor == "AMAZON");
        amazonPattern.OccurrenceCount.Should().Be(4, "Amazon appeared 4 times across reports");

        var starbucksPattern = patterns.First(p => p.NormalizedVendor == "STARBUCKS");
        starbucksPattern.OccurrenceCount.Should().Be(1);

        var uberPattern = patterns.First(p => p.NormalizedVendor == "UBER");
        uberPattern.OccurrenceCount.Should().Be(1);
    }

    [Fact]
    public async Task RebuildPatternsAsync_DeletesExistingPatternsBeforeRebuilding()
    {
        // Arrange: Create an existing pattern that should be deleted
        _dbContext.ExpensePatterns.Add(new ExpensePattern
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            NormalizedVendor = "OLD_PATTERN",
            DisplayName = "Old Pattern",
            OccurrenceCount = 50,
            AverageAmount = 500m,
            MinAmount = 100m,
            MaxAmount = 900m,
            LastSeenAt = DateTime.UtcNow.AddMonths(-12),
            CreatedAt = DateTime.UtcNow.AddMonths(-12),
            UpdatedAt = DateTime.UtcNow.AddMonths(-12)
        });
        await _dbContext.SaveChangesAsync();

        // Create an active report with different vendor
        var activeReport = CreateReport("2025-01", ReportStatus.Draft, new[]
        {
            ("NEW_VENDOR", 100.00m)
        });

        // Act
        var patternsCreated = await _service.RebuildPatternsAsync(_testUserId);

        // Assert
        patternsCreated.Should().Be(1);

        var patterns = await _dbContext.ExpensePatterns
            .Where(p => p.UserId == _testUserId)
            .ToListAsync();

        patterns.Should().HaveCount(1);
        patterns.First().NormalizedVendor.Should().Be("NEW_VENDOR");
        patterns.Should().NotContain(p => p.NormalizedVendor == "OLD_PATTERN",
            "old patterns should be deleted during rebuild");
    }

    #endregion

    #region Helper Methods

    private ExpenseReport CreateReport(string period, ReportStatus status, (string vendor, decimal amount)[] items, bool isDeleted = false)
    {
        var lines = items.Select((item, i) => new ExpenseLine
        {
            Id = Guid.NewGuid(),
            LineOrder = i + 1,
            ExpenseDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
            Amount = item.amount,
            OriginalDescription = $"{item.vendor} Transaction",
            NormalizedDescription = item.vendor,
            VendorName = item.vendor,
            GLCode = "6000",
            CreatedAt = DateTime.UtcNow
        }).ToList();

        var report = new ExpenseReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Period = period,
            Status = status,
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow,
            Lines = lines
        };

        // Set timestamps based on status
        if (status == ReportStatus.Generated || status == ReportStatus.Submitted)
        {
            report.GeneratedAt = DateTimeOffset.UtcNow.AddHours(-1);
        }
        if (status == ReportStatus.Submitted)
        {
            report.SubmittedAt = DateTimeOffset.UtcNow;
        }

        _dbContext.ExpenseReports.Add(report);
        _dbContext.SaveChanges();

        return report;
    }

    #endregion
}
