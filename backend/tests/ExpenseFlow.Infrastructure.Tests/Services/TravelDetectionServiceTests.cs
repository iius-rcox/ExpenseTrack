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
/// Unit tests for TravelDetectionService.
/// </summary>
public class TravelDetectionServiceTests
{
    private readonly Mock<ExpenseFlowDbContext> _dbContextMock;
    private readonly Mock<ITravelPeriodRepository> _travelPeriodRepoMock;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<ILogger<TravelDetectionService>> _loggerMock;
    private readonly TravelDetectionService _sut;
    private readonly Guid _testUserId = Guid.NewGuid();

    public TravelDetectionServiceTests()
    {
        // Create mock DbContext
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>().Options;
        _dbContextMock = new Mock<ExpenseFlowDbContext>(options);
        _travelPeriodRepoMock = new Mock<ITravelPeriodRepository>();
        _vendorAliasServiceMock = new Mock<IVendorAliasService>();
        _loggerMock = new Mock<ILogger<TravelDetectionService>>();

        _sut = new TravelDetectionService(
            _dbContextMock.Object,
            _travelPeriodRepoMock.Object,
            _vendorAliasServiceMock.Object,
            _loggerMock.Object);
    }

    #region DetectFromReceiptAsync Tests

    [Fact]
    public async Task DetectFromReceiptAsync_WithAirlineVendor_CreatesNewTravelPeriod()
    {
        // Arrange
        var vendorAlias = new VendorAlias
        {
            Id = Guid.NewGuid(),
            CanonicalName = "DELTA",
            AliasPattern = "DELTA",
            DisplayName = "Delta Airlines",
            Category = VendorCategory.Airline,
            Confidence = 1.0m
        };

        _vendorAliasServiceMock
            .Setup(v => v.FindMatchingAliasAsync(
                It.IsAny<string>(),
                VendorCategory.Airline, VendorCategory.Hotel))
            .ReturnsAsync(vendorAlias);

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorExtracted = "DELTA AIRLINES ATL",
            DateExtracted = new DateOnly(2024, 3, 15),
            AmountExtracted = 350.00m,
            BlobUrl = "https://test.blob.core.windows.net/receipts/test.jpg",
            OriginalFilename = "flight.jpg",
            ContentType = "image/jpeg",
            Status = ReceiptStatus.Ready
        };

        _travelPeriodRepoMock
            .Setup(r => r.GetOverlappingAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<TravelPeriod>());

        _travelPeriodRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TravelPeriod>()))
            .Returns(Task.CompletedTask);

        _travelPeriodRepoMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DetectFromReceiptAsync(receipt);

        // Assert
        result.Detected.Should().BeTrue();
        result.Action.Should().Be(TravelDetectionAction.Created);
        result.DetectedCategory.Should().Be(VendorCategory.Airline);
        result.Confidence.Should().Be(1.0m);
        result.TravelPeriod.Should().NotBeNull();
        result.TravelPeriod!.StartDate.Should().Be(new DateOnly(2024, 3, 15));
        result.TravelPeriod.EndDate.Should().Be(new DateOnly(2024, 3, 15));

        _travelPeriodRepoMock.Verify(r => r.AddAsync(It.IsAny<TravelPeriod>()), Times.Once);
    }

    [Fact]
    public async Task DetectFromReceiptAsync_WithHotelVendor_CreatesOneNightStay()
    {
        // Arrange
        var vendorAlias = new VendorAlias
        {
            Id = Guid.NewGuid(),
            CanonicalName = "MARRIOTT",
            AliasPattern = "MARRIOTT",
            DisplayName = "Marriott Hotels",
            Category = VendorCategory.Hotel,
            Confidence = 0.95m
        };

        _vendorAliasServiceMock
            .Setup(v => v.FindMatchingAliasAsync(
                It.IsAny<string>(),
                VendorCategory.Airline, VendorCategory.Hotel))
            .ReturnsAsync(vendorAlias);

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorExtracted = "MARRIOTT HOTEL NYC",
            DateExtracted = new DateOnly(2024, 3, 15),
            AmountExtracted = 250.00m,
            BlobUrl = "https://test.blob.core.windows.net/receipts/test.jpg",
            OriginalFilename = "hotel.jpg",
            ContentType = "image/jpeg",
            Status = ReceiptStatus.Ready
        };

        _travelPeriodRepoMock
            .Setup(r => r.GetOverlappingAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<TravelPeriod>());

        _travelPeriodRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TravelPeriod>()))
            .Returns(Task.CompletedTask);

        _travelPeriodRepoMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DetectFromReceiptAsync(receipt);

        // Assert
        result.Detected.Should().BeTrue();
        result.Action.Should().Be(TravelDetectionAction.Created);
        result.DetectedCategory.Should().Be(VendorCategory.Hotel);
        result.TravelPeriod.Should().NotBeNull();
        // Hotel creates one-night stay (end date = start date + 1)
        result.TravelPeriod!.StartDate.Should().Be(new DateOnly(2024, 3, 15));
        result.TravelPeriod.EndDate.Should().Be(new DateOnly(2024, 3, 16));
    }

    [Fact]
    public async Task DetectFromReceiptAsync_WithNonTravelVendor_ReturnsNotDetected()
    {
        // Arrange - no matching vendor alias for travel categories
        _vendorAliasServiceMock
            .Setup(v => v.FindMatchingAliasAsync(
                It.IsAny<string>(),
                VendorCategory.Airline, VendorCategory.Hotel))
            .ReturnsAsync((VendorAlias?)null);

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorExtracted = "STARBUCKS #12345",
            DateExtracted = new DateOnly(2024, 3, 15),
            AmountExtracted = 5.50m,
            BlobUrl = "https://test.blob.core.windows.net/receipts/test.jpg",
            OriginalFilename = "coffee.jpg",
            ContentType = "image/jpeg",
            Status = ReceiptStatus.Ready
        };

        // Act
        var result = await _sut.DetectFromReceiptAsync(receipt);

        // Assert
        result.Detected.Should().BeFalse();
        result.Action.Should().Be(TravelDetectionAction.None);
        result.TravelPeriod.Should().BeNull();
    }

    [Fact]
    public async Task DetectFromReceiptAsync_WithNoVendorExtracted_ReturnsNotDetected()
    {
        // Arrange - null vendor extracted returns null from vendor alias service
        _vendorAliasServiceMock
            .Setup(v => v.FindMatchingAliasAsync(
                It.IsAny<string>(),
                VendorCategory.Airline, VendorCategory.Hotel))
            .ReturnsAsync((VendorAlias?)null);

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorExtracted = null,
            DateExtracted = new DateOnly(2024, 3, 15),
            AmountExtracted = 100.00m,
            BlobUrl = "https://test.blob.core.windows.net/receipts/test.jpg",
            OriginalFilename = "unknown.jpg",
            ContentType = "image/jpeg",
            Status = ReceiptStatus.Ready
        };

        // Act
        var result = await _sut.DetectFromReceiptAsync(receipt);

        // Assert
        result.Detected.Should().BeFalse();
        result.Action.Should().Be(TravelDetectionAction.None);
    }

    [Fact]
    public async Task DetectFromReceiptAsync_WithOverlappingPeriod_ExtendsPeriod()
    {
        // Arrange
        var vendorAlias = new VendorAlias
        {
            Id = Guid.NewGuid(),
            CanonicalName = "HILTON",
            AliasPattern = "HILTON",
            DisplayName = "Hilton Hotels",
            Category = VendorCategory.Hotel,
            Confidence = 1.0m
        };

        _vendorAliasServiceMock
            .Setup(v => v.FindMatchingAliasAsync(
                It.IsAny<string>(),
                VendorCategory.Airline, VendorCategory.Hotel))
            .ReturnsAsync(vendorAlias);

        var existingPeriod = new TravelPeriod
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            StartDate = new DateOnly(2024, 3, 15),
            EndDate = new DateOnly(2024, 3, 16),
            Source = TravelPeriodSource.Flight
        };

        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            VendorExtracted = "HILTON HOTEL",
            DateExtracted = new DateOnly(2024, 3, 17),
            AmountExtracted = 200.00m,
            BlobUrl = "https://test.blob.core.windows.net/receipts/test.jpg",
            OriginalFilename = "hotel.jpg",
            ContentType = "image/jpeg",
            Status = ReceiptStatus.Ready
        };

        _travelPeriodRepoMock
            .Setup(r => r.GetOverlappingAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<TravelPeriod> { existingPeriod });

        _travelPeriodRepoMock
            .Setup(r => r.UpdateAsync(It.IsAny<TravelPeriod>()))
            .Returns(Task.CompletedTask);

        _travelPeriodRepoMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DetectFromReceiptAsync(receipt);

        // Assert
        result.Detected.Should().BeTrue();
        result.Action.Should().Be(TravelDetectionAction.Extended);
        existingPeriod.EndDate.Should().Be(new DateOnly(2024, 3, 18)); // Extended by one night

        _travelPeriodRepoMock.Verify(r => r.UpdateAsync(It.IsAny<TravelPeriod>()), Times.Once);
    }

    #endregion

    #region GetSuggestedGLCodeForDateAsync Tests

    [Fact]
    public async Task GetSuggestedGLCodeForDateAsync_WithinTravelPeriod_ReturnsTravelGLCode()
    {
        // Arrange
        var date = new DateOnly(2024, 3, 15);
        var travelPeriod = new TravelPeriod
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            StartDate = new DateOnly(2024, 3, 14),
            EndDate = new DateOnly(2024, 3, 17)
        };

        _travelPeriodRepoMock
            .Setup(r => r.GetByDateAsync(_testUserId, date))
            .ReturnsAsync(travelPeriod);

        // Act
        var result = await _sut.GetSuggestedGLCodeForDateAsync(_testUserId, date);

        // Assert
        result.Should().Be("66300");
    }

    [Fact]
    public async Task GetSuggestedGLCodeForDateAsync_NotInTravelPeriod_ReturnsNull()
    {
        // Arrange
        var date = new DateOnly(2024, 3, 15);

        _travelPeriodRepoMock
            .Setup(r => r.GetByDateAsync(_testUserId, date))
            .ReturnsAsync((TravelPeriod?)null);

        // Act
        var result = await _sut.GetSuggestedGLCodeForDateAsync(_testUserId, date);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsWithinTravelPeriodAsync Tests

    [Fact]
    public async Task IsWithinTravelPeriodAsync_DateInPeriod_ReturnsTrue()
    {
        // Arrange
        var date = new DateOnly(2024, 3, 15);
        var travelPeriod = new TravelPeriod
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            StartDate = new DateOnly(2024, 3, 14),
            EndDate = new DateOnly(2024, 3, 17)
        };

        _travelPeriodRepoMock
            .Setup(r => r.GetByDateAsync(_testUserId, date))
            .ReturnsAsync(travelPeriod);

        // Act
        var result = await _sut.IsWithinTravelPeriodAsync(_testUserId, date);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsWithinTravelPeriodAsync_DateNotInPeriod_ReturnsFalse()
    {
        // Arrange
        var date = new DateOnly(2024, 3, 15);

        _travelPeriodRepoMock
            .Setup(r => r.GetByDateAsync(_testUserId, date))
            .ReturnsAsync((TravelPeriod?)null);

        // Act
        var result = await _sut.IsWithinTravelPeriodAsync(_testUserId, date);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CRUD Tests

    [Fact]
    public async Task CreateTravelPeriodAsync_CreatesManualPeriod()
    {
        // Arrange
        var request = new CreateTravelPeriodRequestDto
        {
            StartDate = new DateOnly(2024, 6, 1),
            EndDate = new DateOnly(2024, 6, 5),
            Destination = "Las Vegas"
        };

        TravelPeriod? capturedPeriod = null;
        _travelPeriodRepoMock
            .Setup(r => r.AddAsync(It.IsAny<TravelPeriod>()))
            .Callback<TravelPeriod>(p => capturedPeriod = p)
            .Returns(Task.CompletedTask);

        _travelPeriodRepoMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.CreateTravelPeriodAsync(_testUserId, request);

        // Assert
        result.Should().NotBeNull();
        result.StartDate.Should().Be(request.StartDate);
        result.EndDate.Should().Be(request.EndDate);
        result.Destination.Should().Be(request.Destination);
        result.Source.Should().Be(TravelPeriodSource.Manual);

        capturedPeriod.Should().NotBeNull();
        capturedPeriod!.UserId.Should().Be(_testUserId);
    }

    [Fact]
    public async Task DeleteTravelPeriodAsync_ExistingPeriod_ReturnsTrue()
    {
        // Arrange
        var periodId = Guid.NewGuid();
        var existingPeriod = new TravelPeriod
        {
            Id = periodId,
            UserId = _testUserId,
            StartDate = new DateOnly(2024, 3, 15),
            EndDate = new DateOnly(2024, 3, 17)
        };

        _travelPeriodRepoMock
            .Setup(r => r.GetByIdAsync(_testUserId, periodId))
            .ReturnsAsync(existingPeriod);

        _travelPeriodRepoMock
            .Setup(r => r.DeleteAsync(It.IsAny<TravelPeriod>()))
            .Returns(Task.CompletedTask);

        _travelPeriodRepoMock
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.DeleteTravelPeriodAsync(_testUserId, periodId);

        // Assert
        result.Should().BeTrue();
        _travelPeriodRepoMock.Verify(r => r.DeleteAsync(existingPeriod), Times.Once);
    }

    [Fact]
    public async Task DeleteTravelPeriodAsync_NonExistingPeriod_ReturnsFalse()
    {
        // Arrange
        var periodId = Guid.NewGuid();

        _travelPeriodRepoMock
            .Setup(r => r.GetByIdAsync(_testUserId, periodId))
            .ReturnsAsync((TravelPeriod?)null);

        // Act
        var result = await _sut.DeleteTravelPeriodAsync(_testUserId, periodId);

        // Assert
        result.Should().BeFalse();
        _travelPeriodRepoMock.Verify(r => r.DeleteAsync(It.IsAny<TravelPeriod>()), Times.Never);
    }

    #endregion
}
