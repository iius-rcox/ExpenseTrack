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
/// Unit tests for AllowanceService.
/// Tests CRUD operations and period-based retrieval for recurring allowances.
/// </summary>
public class AllowanceServiceTests : IDisposable
{
    private readonly ExpenseFlowDbContext _context;
    private readonly Mock<ILogger<AllowanceService>> _loggerMock;
    private readonly AllowanceService _service;

    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public AllowanceServiceTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ExpenseFlowDbContext(options);
        _loggerMock = new Mock<ILogger<AllowanceService>>();

        // Seed test users
        _context.Users.AddRange(
            new User { Id = _testUserId, EntraObjectId = "test-oid", Email = "test@example.com", DisplayName = "Test User" },
            new User { Id = _otherUserId, EntraObjectId = "other-oid", Email = "other@example.com", DisplayName = "Other User" }
        );

        // Seed GL accounts for lookup
        _context.GLAccounts.AddRange(
            new GLAccount { Id = Guid.NewGuid(), Code = "66300", Name = "Cell Phone Expense", IsActive = true },
            new GLAccount { Id = Guid.NewGuid(), Code = "66400", Name = "Internet Expense", IsActive = true }
        );

        _context.SaveChanges();

        _service = new AllowanceService(_context, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region GetByUserAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUserAsync_ReturnsOnlyUserAllowances()
    {
        // Arrange
        var userAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            GLCode = "66300",
            IsActive = true
        };
        var otherUserAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _otherUserId,
            VendorName = "AT&T",
            Amount = 60.00m,
            Frequency = AllowanceFrequency.Monthly,
            GLCode = "66300",
            IsActive = true
        };

        _context.RecurringAllowances.AddRange(userAllowance, otherUserAllowance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByUserAsync(_testUserId);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].VendorName.Should().Be("Verizon");
        result.Items[0].UserId.Should().Be(_testUserId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUserAsync_WithActiveOnlyTrue_ReturnsOnlyActiveAllowances()
    {
        // Arrange
        var activeAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };
        var inactiveAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Old Service",
            Amount = 30.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = false
        };

        _context.RecurringAllowances.AddRange(activeAllowance, inactiveAllowance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByUserAsync(_testUserId, activeOnly: true);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].VendorName.Should().Be("Verizon");
        result.Items[0].IsActive.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByUserAsync_WithActiveOnlyFalse_ReturnsAllAllowances()
    {
        // Arrange
        var activeAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };
        var inactiveAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Old Service",
            Amount = 30.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = false
        };

        _context.RecurringAllowances.AddRange(activeAllowance, inactiveAllowance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByUserAsync(_testUserId, activeOnly: false);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdAsync_OwnedAllowance_ReturnsAllowance()
    {
        // Arrange
        var allowanceId = Guid.NewGuid();
        var allowance = new RecurringAllowance
        {
            Id = allowanceId,
            UserId = _testUserId,
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            GLCode = "66300",
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(_testUserId, allowanceId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(allowanceId);
        result.VendorName.Should().Be("Verizon");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdAsync_NotOwnedAllowance_ReturnsNull()
    {
        // Arrange
        var allowanceId = Guid.NewGuid();
        var allowance = new RecurringAllowance
        {
            Id = allowanceId,
            UserId = _otherUserId, // Different user
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetByIdAsync(_testUserId, allowanceId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetByIdAsync_NonExistentAllowance_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(_testUserId, Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_ValidRequest_CreatesAllowance()
    {
        // Arrange
        var request = new CreateAllowanceRequest
        {
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            GLCode = "66300",
            DepartmentCode = "DEPT01",
            Description = "Monthly cell phone allowance"
        };

        // Act
        var result = await _service.CreateAsync(_testUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);
        result.VendorName.Should().Be("Verizon");
        result.Amount.Should().Be(50.00m);
        result.Frequency.Should().Be(AllowanceFrequency.Monthly);
        result.GLCode.Should().Be("66300");
        result.DepartmentCode.Should().Be("DEPT01");
        result.Description.Should().Be("Monthly cell phone allowance");
        result.IsActive.Should().BeTrue();

        // Verify persisted
        var persisted = await _context.RecurringAllowances.FindAsync(result.Id);
        persisted.Should().NotBeNull();
        persisted!.UserId.Should().Be(_testUserId);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_WithGLCode_LooksUpGLName()
    {
        // Arrange
        var request = new CreateAllowanceRequest
        {
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            GLCode = "66300" // Matches seeded GL account
        };

        // Act
        var result = await _service.CreateAsync(_testUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.GLName.Should().Be("Cell Phone Expense");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_WithWeeklyFrequency_CreatesCorrectly()
    {
        // Arrange
        var request = new CreateAllowanceRequest
        {
            VendorName = "Weekly Service",
            Amount = 25.00m,
            Frequency = AllowanceFrequency.Weekly
        };

        // Act
        var result = await _service.CreateAsync(_testUserId, request);

        // Assert
        result.Frequency.Should().Be(AllowanceFrequency.Weekly);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_WithQuarterlyFrequency_CreatesCorrectly()
    {
        // Arrange
        var request = new CreateAllowanceRequest
        {
            VendorName = "Quarterly Service",
            Amount = 150.00m,
            Frequency = AllowanceFrequency.Quarterly
        };

        // Act
        var result = await _service.CreateAsync(_testUserId, request);

        // Assert
        result.Frequency.Should().Be(AllowanceFrequency.Quarterly);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAsync_OwnedAllowance_UpdatesFields()
    {
        // Arrange
        var allowanceId = Guid.NewGuid();
        var allowance = new RecurringAllowance
        {
            Id = allowanceId,
            UserId = _testUserId,
            VendorName = "Original Vendor",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            GLCode = "66300",
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        var request = new UpdateAllowanceRequest
        {
            VendorName = "Updated Vendor",
            Amount = 75.00m,
            Frequency = AllowanceFrequency.Weekly,
            GLCode = "66400"
        };

        // Act
        var result = await _service.UpdateAsync(_testUserId, allowanceId, request);

        // Assert
        result.Should().NotBeNull();
        result!.VendorName.Should().Be("Updated Vendor");
        result.Amount.Should().Be(75.00m);
        result.Frequency.Should().Be(AllowanceFrequency.Weekly);
        result.GLCode.Should().Be("66400");
        result.GLName.Should().Be("Internet Expense");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAsync_NotOwnedAllowance_ReturnsNull()
    {
        // Arrange
        var allowanceId = Guid.NewGuid();
        var allowance = new RecurringAllowance
        {
            Id = allowanceId,
            UserId = _otherUserId, // Different user
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        var request = new UpdateAllowanceRequest
        {
            VendorName = "Hacked Vendor"
        };

        // Act
        var result = await _service.UpdateAsync(_testUserId, allowanceId, request);

        // Assert
        result.Should().BeNull();

        // Verify no change
        var original = await _context.RecurringAllowances.FindAsync(allowanceId);
        original!.VendorName.Should().Be("Verizon");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAsync_PartialUpdate_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var allowanceId = Guid.NewGuid();
        var allowance = new RecurringAllowance
        {
            Id = allowanceId,
            UserId = _testUserId,
            VendorName = "Original Vendor",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            GLCode = "66300",
            Description = "Original Description",
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Only update amount
        var request = new UpdateAllowanceRequest
        {
            Amount = 75.00m
        };

        // Act
        var result = await _service.UpdateAsync(_testUserId, allowanceId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Amount.Should().Be(75.00m);
        result.VendorName.Should().Be("Original Vendor"); // Unchanged
        result.Frequency.Should().Be(AllowanceFrequency.Monthly); // Unchanged
        result.GLCode.Should().Be("66300"); // Unchanged
        result.Description.Should().Be("Original Description"); // Unchanged
    }

    #endregion

    #region DeactivateAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeactivateAsync_OwnedAllowance_SetsIsActiveFalse()
    {
        // Arrange
        var allowanceId = Guid.NewGuid();
        var allowance = new RecurringAllowance
        {
            Id = allowanceId,
            UserId = _testUserId,
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateAsync(_testUserId, allowanceId);

        // Assert
        result.Should().BeTrue();

        var deactivated = await _context.RecurringAllowances.FindAsync(allowanceId);
        deactivated.Should().NotBeNull();
        deactivated!.IsActive.Should().BeFalse();
        deactivated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeactivateAsync_NotOwnedAllowance_ReturnsFalse()
    {
        // Arrange
        var allowanceId = Guid.NewGuid();
        var allowance = new RecurringAllowance
        {
            Id = allowanceId,
            UserId = _otherUserId, // Different user
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.DeactivateAsync(_testUserId, allowanceId);

        // Assert
        result.Should().BeFalse();

        // Verify not changed
        var original = await _context.RecurringAllowances.FindAsync(allowanceId);
        original!.IsActive.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeactivateAsync_PreservesAllowanceData()
    {
        // Arrange - Soft delete should preserve history
        var allowanceId = Guid.NewGuid();
        var allowance = new RecurringAllowance
        {
            Id = allowanceId,
            UserId = _testUserId,
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            GLCode = "66300",
            DepartmentCode = "DEPT01",
            Description = "Important history",
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Act
        await _service.DeactivateAsync(_testUserId, allowanceId);

        // Assert - All data preserved, only IsActive changed
        var deactivated = await _context.RecurringAllowances.FindAsync(allowanceId);
        deactivated!.VendorName.Should().Be("Verizon");
        deactivated.Amount.Should().Be(50.00m);
        deactivated.Frequency.Should().Be(AllowanceFrequency.Monthly);
        deactivated.GLCode.Should().Be("66300");
        deactivated.DepartmentCode.Should().Be("DEPT01");
        deactivated.Description.Should().Be("Important history");
        deactivated.IsActive.Should().BeFalse();
    }

    #endregion

    #region GetActiveAllowancesForPeriodAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetActiveAllowancesForPeriodAsync_MonthlyFrequency_ReturnsOncePerMonth()
    {
        // Arrange
        var allowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Verizon",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Period: January 2025
        var periodStart = new DateOnly(2025, 1, 1);
        var periodEnd = new DateOnly(2025, 1, 31);

        // Act
        var result = await _service.GetActiveAllowancesForPeriodAsync(_testUserId, periodStart, periodEnd);

        // Assert - Monthly should return once
        result.Should().HaveCount(1);
        result[0].VendorName.Should().Be("Verizon");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetActiveAllowancesForPeriodAsync_WeeklyFrequency_ReturnsForEachWeek()
    {
        // Arrange
        var allowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Weekly Service",
            Amount = 25.00m,
            Frequency = AllowanceFrequency.Weekly,
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Period: January 2025 (has ~4-5 weeks)
        var periodStart = new DateOnly(2025, 1, 1);
        var periodEnd = new DateOnly(2025, 1, 31);

        // Act
        var result = await _service.GetActiveAllowancesForPeriodAsync(_testUserId, periodStart, periodEnd);

        // Assert - Weekly should return multiple times (4-5 weeks in January)
        result.Should().HaveCountGreaterOrEqualTo(4);
        result.Should().HaveCountLessOrEqualTo(5);
        result.Should().AllSatisfy(a => a.VendorName.Should().Be("Weekly Service"));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetActiveAllowancesForPeriodAsync_QuarterlyFrequency_ReturnsOncePerQuarter()
    {
        // Arrange
        var allowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Quarterly Service",
            Amount = 150.00m,
            Frequency = AllowanceFrequency.Quarterly,
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Period: January 2025 (Q1 month)
        var periodStart = new DateOnly(2025, 1, 1);
        var periodEnd = new DateOnly(2025, 1, 31);

        // Act
        var result = await _service.GetActiveAllowancesForPeriodAsync(_testUserId, periodStart, periodEnd);

        // Assert - Quarterly only in Jan/Apr/Jul/Oct
        result.Should().HaveCount(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetActiveAllowancesForPeriodAsync_QuarterlyFrequency_NotInNonQuarterMonth()
    {
        // Arrange
        var allowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Quarterly Service",
            Amount = 150.00m,
            Frequency = AllowanceFrequency.Quarterly,
            IsActive = true
        };

        _context.RecurringAllowances.Add(allowance);
        await _context.SaveChangesAsync();

        // Period: February 2025 (not a quarterly month)
        var periodStart = new DateOnly(2025, 2, 1);
        var periodEnd = new DateOnly(2025, 2, 28);

        // Act
        var result = await _service.GetActiveAllowancesForPeriodAsync(_testUserId, periodStart, periodEnd);

        // Assert - No quarterly allowance in February
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetActiveAllowancesForPeriodAsync_ExcludesInactiveAllowances()
    {
        // Arrange
        var activeAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Active Service",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };
        var inactiveAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Inactive Service",
            Amount = 30.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = false
        };

        _context.RecurringAllowances.AddRange(activeAllowance, inactiveAllowance);
        await _context.SaveChangesAsync();

        var periodStart = new DateOnly(2025, 1, 1);
        var periodEnd = new DateOnly(2025, 1, 31);

        // Act
        var result = await _service.GetActiveAllowancesForPeriodAsync(_testUserId, periodStart, periodEnd);

        // Assert
        result.Should().HaveCount(1);
        result[0].VendorName.Should().Be("Active Service");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetActiveAllowancesForPeriodAsync_ExcludesOtherUsersAllowances()
    {
        // Arrange
        var userAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "My Service",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };
        var otherAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _otherUserId,
            VendorName = "Other Service",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };

        _context.RecurringAllowances.AddRange(userAllowance, otherAllowance);
        await _context.SaveChangesAsync();

        var periodStart = new DateOnly(2025, 1, 1);
        var periodEnd = new DateOnly(2025, 1, 31);

        // Act
        var result = await _service.GetActiveAllowancesForPeriodAsync(_testUserId, periodStart, periodEnd);

        // Assert
        result.Should().HaveCount(1);
        result[0].VendorName.Should().Be("My Service");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetActiveAllowancesForPeriodAsync_MixedFrequencies_ReturnsCorrectCounts()
    {
        // Arrange
        var monthlyAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Monthly",
            Amount = 50.00m,
            Frequency = AllowanceFrequency.Monthly,
            IsActive = true
        };
        var weeklyAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Weekly",
            Amount = 25.00m,
            Frequency = AllowanceFrequency.Weekly,
            IsActive = true
        };
        var quarterlyAllowance = new RecurringAllowance
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorName = "Quarterly",
            Amount = 150.00m,
            Frequency = AllowanceFrequency.Quarterly,
            IsActive = true
        };

        _context.RecurringAllowances.AddRange(monthlyAllowance, weeklyAllowance, quarterlyAllowance);
        await _context.SaveChangesAsync();

        // Period: January 2025 (Q1 month, ~4-5 weeks)
        var periodStart = new DateOnly(2025, 1, 1);
        var periodEnd = new DateOnly(2025, 1, 31);

        // Act
        var result = await _service.GetActiveAllowancesForPeriodAsync(_testUserId, periodStart, periodEnd);

        // Assert - 1 monthly + 4-5 weekly + 1 quarterly
        var monthlyCount = result.Count(a => a.VendorName == "Monthly");
        var weeklyCount = result.Count(a => a.VendorName == "Weekly");
        var quarterlyCount = result.Count(a => a.VendorName == "Quarterly");

        monthlyCount.Should().Be(1);
        weeklyCount.Should().BeGreaterOrEqualTo(4).And.BeLessOrEqualTo(5);
        quarterlyCount.Should().Be(1);
    }

    #endregion
}
