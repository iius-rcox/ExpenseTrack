using ExpenseFlow.Api.Controllers;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ExpenseFlow.Api.Tests.Controllers;

/// <summary>
/// Integration tests for PredictionsController endpoints.
/// Tests T046-T047: GET /api/predictions, GET /api/predictions/availability
/// </summary>
public class PredictionsControllerTests
{
    private readonly Mock<IExpensePredictionService> _predictionServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<PredictionsController>> _loggerMock;
    private readonly PredictionsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public PredictionsControllerTests()
    {
        _predictionServiceMock = new Mock<IExpensePredictionService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<PredictionsController>>();

        _controller = new PredictionsController(
            _predictionServiceMock.Object,
            _userServiceMock.Object,
            _loggerMock.Object);

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

    #region T046: GET /api/predictions Tests

    [Fact]
    public async Task GetPredictions_ReturnsOk_WithPaginatedResults()
    {
        // Arrange
        var expectedResponse = new PredictionListResponseDto
        {
            Predictions = new List<PredictionSummaryDto>
            {
                new PredictionSummaryDto
                {
                    Id = Guid.NewGuid(),
                    TransactionId = Guid.NewGuid(),
                    PatternId = Guid.NewGuid(),
                    VendorName = "STARBUCKS",
                    ConfidenceScore = 0.85m,
                    ConfidenceLevel = PredictionConfidence.High,
                    Status = PredictionStatus.Pending
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
            PendingCount = 1,
            HighConfidenceCount = 1
        };

        _predictionServiceMock
            .Setup(s => s.GetPredictionsAsync(_testUserId, 1, 20, null, null))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPredictions();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PredictionListResponseDto>().Subject;
        response.Predictions.Should().HaveCount(1);
        response.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetPredictions_WithStatusFilter_PassesFilterToService()
    {
        // Arrange
        var expectedResponse = new PredictionListResponseDto
        {
            Predictions = new List<PredictionSummaryDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _predictionServiceMock
            .Setup(s => s.GetPredictionsAsync(_testUserId, 1, 20, PredictionStatus.Confirmed, null))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPredictions(status: PredictionStatus.Confirmed);

        // Assert
        _predictionServiceMock.Verify(
            s => s.GetPredictionsAsync(_testUserId, 1, 20, PredictionStatus.Confirmed, null),
            Times.Once);
    }

    [Fact]
    public async Task GetPredictions_WithMinConfidence_PassesFilterToService()
    {
        // Arrange
        var expectedResponse = new PredictionListResponseDto
        {
            Predictions = new List<PredictionSummaryDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 20
        };

        _predictionServiceMock
            .Setup(s => s.GetPredictionsAsync(_testUserId, 1, 20, null, PredictionConfidence.High))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPredictions(minConfidence: PredictionConfidence.High);

        // Assert
        _predictionServiceMock.Verify(
            s => s.GetPredictionsAsync(_testUserId, 1, 20, null, PredictionConfidence.High),
            Times.Once);
    }

    [Fact]
    public async Task GetPredictions_NormalizesInvalidPagination()
    {
        // Arrange
        var expectedResponse = new PredictionListResponseDto
        {
            Predictions = new List<PredictionSummaryDto>(),
            TotalCount = 0,
            Page = 1,
            PageSize = 100 // Clamped to max
        };

        _predictionServiceMock
            .Setup(s => s.GetPredictionsAsync(_testUserId, 1, 100, null, null))
            .ReturnsAsync(expectedResponse);

        // Act - pass invalid values
        var result = await _controller.GetPredictions(page: -5, pageSize: 500);

        // Assert - should normalize to valid values
        _predictionServiceMock.Verify(
            s => s.GetPredictionsAsync(_testUserId, 1, 100, null, null),
            Times.Once);
    }

    #endregion

    #region T047: GET /api/predictions/availability Tests

    [Fact]
    public async Task CheckAvailability_WithPatterns_ReturnsAvailable()
    {
        // Arrange
        var expectedResponse = new PredictionAvailabilityDto
        {
            IsAvailable = true,
            PatternCount = 5,
            Message = "Predictions available based on 5 learned expense patterns."
        };

        _predictionServiceMock
            .Setup(s => s.CheckAvailabilityAsync(_testUserId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CheckAvailability();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PredictionAvailabilityDto>().Subject;
        response.IsAvailable.Should().BeTrue();
        response.PatternCount.Should().Be(5);
    }

    [Fact]
    public async Task CheckAvailability_NoPatterns_ReturnsNotAvailable()
    {
        // Arrange - cold start scenario
        var expectedResponse = new PredictionAvailabilityDto
        {
            IsAvailable = false,
            PatternCount = 0,
            Message = "No predictions available yet. Submit expense reports to help the system learn your expense patterns."
        };

        _predictionServiceMock
            .Setup(s => s.CheckAvailabilityAsync(_testUserId))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.CheckAvailability();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PredictionAvailabilityDto>().Subject;
        response.IsAvailable.Should().BeFalse();
        response.PatternCount.Should().Be(0);
        response.Message.Should().Contain("Submit expense reports");
    }

    #endregion

    #region Dashboard and Stats Tests

    [Fact]
    public async Task GetDashboard_ReturnsOk_WithDashboardData()
    {
        // Arrange
        var expectedDashboard = new PredictionDashboardDto
        {
            PendingCount = 10,
            HighConfidenceCount = 5,
            MediumConfidenceCount = 5,
            ActivePatternCount = 20,
            OverallAccuracyRate = 0.85m,
            TopPredictions = new List<PredictionTransactionDto>()
        };

        _predictionServiceMock
            .Setup(s => s.GetDashboardAsync(_testUserId))
            .ReturnsAsync(expectedDashboard);

        // Act
        var result = await _controller.GetDashboard();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dashboard = okResult.Value.Should().BeOfType<PredictionDashboardDto>().Subject;
        dashboard.PendingCount.Should().Be(10);
        dashboard.OverallAccuracyRate.Should().Be(0.85m);
    }

    [Fact]
    public async Task GetStats_ReturnsOk_WithAccuracyStats()
    {
        // Arrange
        var expectedStats = new PredictionAccuracyStatsDto
        {
            TotalPredictions = 100,
            ConfirmedCount = 75,
            RejectedCount = 15,
            IgnoredCount = 10,
            AccuracyRate = 0.833m,
            HighConfidenceAccuracyRate = 0.90m,
            MediumConfidenceAccuracyRate = 0.75m
        };

        _predictionServiceMock
            .Setup(s => s.GetAccuracyStatsAsync(_testUserId))
            .ReturnsAsync(expectedStats);

        // Act
        var result = await _controller.GetStats();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var stats = okResult.Value.Should().BeOfType<PredictionAccuracyStatsDto>().Subject;
        stats.AccuracyRate.Should().Be(0.833m);
    }

    #endregion

    #region Prediction Actions Tests

    [Fact]
    public async Task ConfirmPrediction_Success_ReturnsOk()
    {
        // Arrange
        var request = new ConfirmPredictionRequestDto { PredictionId = Guid.NewGuid() };
        var expectedResponse = new PredictionActionResponseDto
        {
            Success = true,
            NewStatus = PredictionStatus.Confirmed,
            Message = "Prediction confirmed successfully"
        };

        _predictionServiceMock
            .Setup(s => s.ConfirmPredictionAsync(_testUserId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ConfirmPrediction(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PredictionActionResponseDto>().Subject;
        response.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ConfirmPrediction_NotFound_Returns404()
    {
        // Arrange
        var request = new ConfirmPredictionRequestDto { PredictionId = Guid.NewGuid() };
        var expectedResponse = new PredictionActionResponseDto
        {
            Success = false,
            Message = "Prediction not found"
        };

        _predictionServiceMock
            .Setup(s => s.ConfirmPredictionAsync(_testUserId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ConfirmPrediction(request);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RejectPrediction_Success_ReturnsOk()
    {
        // Arrange
        var request = new RejectPredictionRequestDto { PredictionId = Guid.NewGuid() };
        var expectedResponse = new PredictionActionResponseDto
        {
            Success = true,
            NewStatus = PredictionStatus.Rejected,
            Message = "Prediction rejected"
        };

        _predictionServiceMock
            .Setup(s => s.RejectPredictionAsync(_testUserId, request))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RejectPrediction(request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PredictionActionResponseDto>().Subject;
        response.Success.Should().BeTrue();
    }

    #endregion

    #region Pattern Endpoints Tests

    [Fact]
    public async Task GetPatterns_ReturnsOk_WithPaginatedPatterns()
    {
        // Arrange
        var expectedResponse = new PatternListResponseDto
        {
            Patterns = new List<PatternSummaryDto>
            {
                new PatternSummaryDto
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "STARBUCKS",
                    Category = "Food & Beverage",
                    AverageAmount = 5.50m,
                    OccurrenceCount = 25
                }
            },
            TotalCount = 1,
            Page = 1,
            PageSize = 20,
            ActiveCount = 1,
            SuppressedCount = 0
        };

        _predictionServiceMock
            .Setup(s => s.GetPatternsAsync(
                _testUserId, 1, 20, false, false,
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPatterns();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PatternListResponseDto>().Subject;
        response.Patterns.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPattern_ExistingId_ReturnsOk()
    {
        // Arrange
        var patternId = Guid.NewGuid();
        var expectedPattern = new PatternDetailDto
        {
            Id = patternId,
            DisplayName = "STARBUCKS",
            NormalizedVendor = "starbucks",
            Category = "Food & Beverage",
            AverageAmount = 5.50m,
            OccurrenceCount = 25
        };

        _predictionServiceMock
            .Setup(s => s.GetPatternAsync(_testUserId, patternId))
            .ReturnsAsync(expectedPattern);

        // Act
        var result = await _controller.GetPattern(patternId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var pattern = okResult.Value.Should().BeOfType<PatternDetailDto>().Subject;
        pattern.Id.Should().Be(patternId);
    }

    [Fact]
    public async Task GetPattern_NonExistingId_Returns404()
    {
        // Arrange
        var patternId = Guid.NewGuid();

        _predictionServiceMock
            .Setup(s => s.GetPatternAsync(_testUserId, patternId))
            .ReturnsAsync((PatternDetailDto?)null);

        // Act
        var result = await _controller.GetPattern(patternId);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeletePattern_ExistingId_ReturnsNoContent()
    {
        // Arrange
        var patternId = Guid.NewGuid();

        _predictionServiceMock
            .Setup(s => s.DeletePatternAsync(_testUserId, patternId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeletePattern(patternId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    // T081: Test includeSuppressed filter
    [Fact]
    public async Task GetPatterns_WithIncludeSuppressed_PassesFilterToService()
    {
        // Arrange
        var expectedResponse = new PatternListResponseDto
        {
            Patterns = new List<PatternSummaryDto>
            {
                new PatternSummaryDto
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "STARBUCKS",
                    IsSuppressed = false
                },
                new PatternSummaryDto
                {
                    Id = Guid.NewGuid(),
                    DisplayName = "DUNKIN",
                    IsSuppressed = true
                }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 20,
            ActiveCount = 1,
            SuppressedCount = 1
        };

        _predictionServiceMock
            .Setup(s => s.GetPatternsAsync(
                _testUserId, 1, 20, true, false,
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetPatterns(includeSuppressed: true);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<PatternListResponseDto>().Subject;
        response.Patterns.Should().HaveCount(2);
        response.SuppressedCount.Should().Be(1);

        _predictionServiceMock.Verify(
            s => s.GetPatternsAsync(
                _testUserId, 1, 20, true, false,
                It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region T082: Pattern Suppression Tests

    [Fact]
    public async Task UpdatePatternSuppression_Suppress_ReturnsNoContent()
    {
        // Arrange
        var patternId = Guid.NewGuid();
        var request = new UpdatePatternSuppressionRequestDto
        {
            PatternId = patternId,
            IsSuppressed = true
        };

        _predictionServiceMock
            .Setup(s => s.UpdatePatternSuppressionAsync(_testUserId, It.Is<UpdatePatternSuppressionRequestDto>(r => r.PatternId == patternId && r.IsSuppressed == true)))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdatePatternSuppression(patternId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _predictionServiceMock.Verify(
            s => s.UpdatePatternSuppressionAsync(_testUserId, It.Is<UpdatePatternSuppressionRequestDto>(r => r.PatternId == patternId && r.IsSuppressed == true)),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePatternSuppression_Unsuppress_ReturnsNoContent()
    {
        // Arrange
        var patternId = Guid.NewGuid();
        var request = new UpdatePatternSuppressionRequestDto
        {
            PatternId = patternId,
            IsSuppressed = false
        };

        _predictionServiceMock
            .Setup(s => s.UpdatePatternSuppressionAsync(_testUserId, It.Is<UpdatePatternSuppressionRequestDto>(r => r.PatternId == patternId && r.IsSuppressed == false)))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.UpdatePatternSuppression(patternId, request);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _predictionServiceMock.Verify(
            s => s.UpdatePatternSuppressionAsync(_testUserId, It.Is<UpdatePatternSuppressionRequestDto>(r => r.PatternId == patternId && r.IsSuppressed == false)),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePatternSuppression_NonExistingPattern_Returns404()
    {
        // Arrange
        var patternId = Guid.NewGuid();
        var request = new UpdatePatternSuppressionRequestDto
        {
            PatternId = patternId,
            IsSuppressed = true
        };

        _predictionServiceMock
            .Setup(s => s.UpdatePatternSuppressionAsync(_testUserId, It.Is<UpdatePatternSuppressionRequestDto>(r => r.PatternId == patternId && r.IsSuppressed == true)))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.UpdatePatternSuppression(patternId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdatePatternSuppression_MismatchedId_ReturnsBadRequest()
    {
        // Arrange
        var patternId = Guid.NewGuid();
        var differentId = Guid.NewGuid();
        var request = new UpdatePatternSuppressionRequestDto
        {
            PatternId = differentId, // Different from route parameter
            IsSuppressed = true
        };

        // Act
        var result = await _controller.UpdatePatternSuppression(patternId, request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
