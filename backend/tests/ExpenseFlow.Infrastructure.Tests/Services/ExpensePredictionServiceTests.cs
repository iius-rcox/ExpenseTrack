using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for ExpensePredictionService.
/// Tests T042-T045: decay formula, confidence scoring, pattern extraction, and batch prediction.
/// </summary>
public class ExpensePredictionServiceTests
{
    private readonly Mock<IExpensePatternRepository> _patternRepoMock;
    private readonly Mock<ITransactionPredictionRepository> _predictionRepoMock;
    private readonly Mock<IExpenseReportRepository> _reportRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<ILogger<ExpensePredictionService>> _loggerMock;
    private readonly ExpensePredictionService _sut;
    private readonly Guid _testUserId = Guid.NewGuid();

    public ExpensePredictionServiceTests()
    {
        _patternRepoMock = new Mock<IExpensePatternRepository>();
        _predictionRepoMock = new Mock<ITransactionPredictionRepository>();
        _reportRepoMock = new Mock<IExpenseReportRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _vendorAliasServiceMock = new Mock<IVendorAliasService>();
        _loggerMock = new Mock<ILogger<ExpensePredictionService>>();

        _sut = new ExpensePredictionService(
            _patternRepoMock.Object,
            _predictionRepoMock.Object,
            _reportRepoMock.Object,
            _transactionRepoMock.Object,
            _vendorAliasServiceMock.Object,
            _loggerMock.Object);
    }

    #region T042: CalculateDecayWeight Tests

    [Theory]
    [InlineData(0, 1.0)] // Today: full weight
    [InlineData(6, 0.5)] // 6 months ago: half weight (half-life)
    [InlineData(12, 0.25)] // 12 months ago: quarter weight
    [InlineData(24, 0.0625)] // 24 months ago: 1/16 weight
    public void CalculateDecayWeight_ExponentialDecay_ReturnsCorrectWeight(int monthsAgo, double expectedWeight)
    {
        // Arrange
        var reportDate = DateTime.UtcNow.AddMonths(-monthsAgo);
        var tolerance = 0.05; // 5% tolerance for floating point

        // Act - using reflection to access private method
        var methodInfo = typeof(ExpensePredictionService)
            .GetMethod("CalculateDecayWeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)methodInfo!.Invoke(_sut, new object[] { reportDate })!;

        // Assert
        ((double)result).Should().BeApproximately(expectedWeight, tolerance);
    }

    [Fact]
    public void CalculateDecayWeight_VeryOldDate_ReturnsMinimumWeight()
    {
        // Arrange - 5 years ago
        var reportDate = DateTime.UtcNow.AddYears(-5);

        // Act
        var methodInfo = typeof(ExpensePredictionService)
            .GetMethod("CalculateDecayWeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)methodInfo!.Invoke(_sut, new object[] { reportDate })!;

        // Assert - should be at least 1% minimum
        result.Should().BeGreaterOrEqualTo(0.01m);
    }

    [Fact]
    public void CalculateDecayWeight_FutureDate_ReturnsFull Weight()
    {
        // Arrange - future date (edge case)
        var reportDate = DateTime.UtcNow.AddDays(30);

        // Act
        var methodInfo = typeof(ExpensePredictionService)
            .GetMethod("CalculateDecayWeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)methodInfo!.Invoke(_sut, new object[] { reportDate })!;

        // Assert - future dates should get at least full weight
        result.Should().BeGreaterOrEqualTo(1.0m);
    }

    #endregion

    #region T043: CalculateConfidenceScore Tests

    [Fact]
    public void CalculateConfidenceScore_FrequentPattern_HighFrequencySignal()
    {
        // Arrange - pattern seen 10 times (max frequency signal)
        var pattern = CreateTestPattern(occurrenceCount: 10, confirmCount: 5, rejectCount: 0);
        var transactionAmount = pattern.AverageAmount; // Exact match

        // Act
        var methodInfo = typeof(ExpensePredictionService)
            .GetMethod("CalculateConfidenceScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)methodInfo!.Invoke(_sut, new object[] { pattern, transactionAmount })!;

        // Assert - high frequency + perfect amount match + positive feedback should yield high score
        result.Should().BeGreaterThan(0.7m);
    }

    [Fact]
    public void CalculateConfidenceScore_InfrequentPattern_LowFrequencySignal()
    {
        // Arrange - pattern seen only once
        var pattern = CreateTestPattern(occurrenceCount: 1, confirmCount: 0, rejectCount: 0);
        var transactionAmount = pattern.AverageAmount;

        // Act
        var methodInfo = typeof(ExpensePredictionService)
            .GetMethod("CalculateConfidenceScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)methodInfo!.Invoke(_sut, new object[] { pattern, transactionAmount })!;

        // Assert - low frequency should reduce confidence
        result.Should().BeLessThan(0.8m);
    }

    [Fact]
    public void CalculateConfidenceScore_AmountMismatch_LowAmountSignal()
    {
        // Arrange - pattern with average $100, but transaction is $500 (5x mismatch)
        var pattern = CreateTestPattern(occurrenceCount: 10, confirmCount: 5, rejectCount: 0, averageAmount: 100m);
        var transactionAmount = 500m;

        // Act
        var methodInfo = typeof(ExpensePredictionService)
            .GetMethod("CalculateConfidenceScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)methodInfo!.Invoke(_sut, new object[] { pattern, transactionAmount })!;

        // Assert - large amount mismatch reduces confidence
        result.Should().BeLessThan(0.8m);
    }

    [Fact]
    public void CalculateConfidenceScore_NegativeFeedback_LowFeedbackSignal()
    {
        // Arrange - pattern with more rejects than confirms
        var pattern = CreateTestPattern(occurrenceCount: 10, confirmCount: 1, rejectCount: 9);
        var transactionAmount = pattern.AverageAmount;

        // Act
        var methodInfo = typeof(ExpensePredictionService)
            .GetMethod("CalculateConfidenceScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)methodInfo!.Invoke(_sut, new object[] { pattern, transactionAmount })!;

        // Assert - negative feedback reduces confidence
        result.Should().BeLessThan(0.7m);
    }

    [Fact]
    public void CalculateConfidenceScore_NoFeedback_DefaultFeedbackSignal()
    {
        // Arrange - new pattern with no feedback yet
        var pattern = CreateTestPattern(occurrenceCount: 5, confirmCount: 0, rejectCount: 0);
        var transactionAmount = pattern.AverageAmount;

        // Act
        var methodInfo = typeof(ExpensePredictionService)
            .GetMethod("CalculateConfidenceScore", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = (decimal)methodInfo!.Invoke(_sut, new object[] { pattern, transactionAmount })!;

        // Assert - no feedback uses neutral 0.5 signal
        result.Should().BeGreaterThan(0.4m);
        result.Should().BeLessThan(0.9m);
    }

    #endregion

    #region T044: LearnFromReportAsync Tests

    [Fact]
    public async Task LearnFromReportAsync_SubmittedReport_ExtractsPatterns()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = new ExpenseReport
        {
            Id = reportId,
            UserId = _testUserId,
            Status = ReportStatus.Submitted,
            ExpenseLines = new List<ExpenseLine>
            {
                CreateTestExpenseLine("STARBUCKS", 5.50m, "Food & Beverage"),
                CreateTestExpenseLine("UBER", 25.00m, "Transportation"),
                CreateTestExpenseLine("STARBUCKS", 6.00m, "Food & Beverage") // Duplicate vendor
            }
        };

        _reportRepoMock
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(report);

        _patternRepoMock
            .Setup(p => p.GetByNormalizedVendorAsync(_testUserId, It.IsAny<string>()))
            .ReturnsAsync((ExpensePattern?)null); // No existing patterns

        _patternRepoMock
            .Setup(p => p.AddAsync(It.IsAny<ExpensePattern>()))
            .ReturnsAsync((ExpensePattern p) => p);

        // Act
        var result = await _sut.LearnFromReportAsync(_testUserId, reportId);

        // Assert - should create 2 patterns (STARBUCKS aggregated, UBER)
        result.Should().Be(2);
        _patternRepoMock.Verify(p => p.AddAsync(It.IsAny<ExpensePattern>()), Times.Exactly(2));
    }

    [Fact]
    public async Task LearnFromReportAsync_ExistingPattern_UpdatesPattern()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = new ExpenseReport
        {
            Id = reportId,
            UserId = _testUserId,
            Status = ReportStatus.Submitted,
            ExpenseLines = new List<ExpenseLine>
            {
                CreateTestExpenseLine("STARBUCKS", 5.50m, "Food & Beverage")
            }
        };

        var existingPattern = CreateTestPattern("starbucks", occurrenceCount: 5, averageAmount: 5.00m);

        _reportRepoMock
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(report);

        _patternRepoMock
            .Setup(p => p.GetByNormalizedVendorAsync(_testUserId, "starbucks"))
            .ReturnsAsync(existingPattern);

        _patternRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<ExpensePattern>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.LearnFromReportAsync(_testUserId, reportId);

        // Assert - should update existing pattern
        result.Should().Be(1);
        _patternRepoMock.Verify(p => p.UpdateAsync(It.Is<ExpensePattern>(
            pat => pat.OccurrenceCount == 6)), Times.Once);
    }

    [Fact]
    public async Task LearnFromReportAsync_NonSubmittedReport_ReturnsZero()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = new ExpenseReport
        {
            Id = reportId,
            UserId = _testUserId,
            Status = ReportStatus.Generated, // Not submitted
            ExpenseLines = new List<ExpenseLine>
            {
                CreateTestExpenseLine("STARBUCKS", 5.50m, "Food & Beverage")
            }
        };

        _reportRepoMock
            .Setup(r => r.GetByIdAsync(reportId))
            .ReturnsAsync(report);

        // Act
        var result = await _sut.LearnFromReportAsync(_testUserId, reportId);

        // Assert
        result.Should().Be(0);
        _patternRepoMock.Verify(p => p.AddAsync(It.IsAny<ExpensePattern>()), Times.Never);
    }

    #endregion

    #region T045: GeneratePredictionsAsync Tests

    [Fact]
    public async Task GenerateAllPendingPredictionsAsync_WithPatterns_CreatesPredictions()
    {
        // Arrange
        var patterns = new List<ExpensePattern>
        {
            CreateTestPattern("starbucks", occurrenceCount: 10)
        };

        var transactions = new List<TransactionSummaryDto>
        {
            new TransactionSummaryDto
            {
                Id = Guid.NewGuid(),
                Description = "STARBUCKS STORE 123",
                Amount = 5.50m,
                TransactionDate = DateTime.UtcNow
            }
        };

        _patternRepoMock
            .Setup(p => p.GetActiveAsync(_testUserId))
            .ReturnsAsync(patterns);

        _transactionRepoMock
            .Setup(t => t.GetUnprocessedForPredictionAsync(_testUserId))
            .ReturnsAsync(transactions);

        _predictionRepoMock
            .Setup(p => p.AddAsync(It.IsAny<TransactionPrediction>()))
            .ReturnsAsync((TransactionPrediction tp) => tp);

        // Act
        var result = await _sut.GenerateAllPendingPredictionsAsync(_testUserId);

        // Assert
        result.Should().Be(1);
        _predictionRepoMock.Verify(p => p.AddAsync(It.Is<TransactionPrediction>(
            tp => tp.TransactionId == transactions[0].Id)), Times.Once);
    }

    [Fact]
    public async Task GenerateAllPendingPredictionsAsync_LowConfidence_SkipsPrediction()
    {
        // Arrange - pattern with lots of rejections (low confidence)
        var patterns = new List<ExpensePattern>
        {
            CreateTestPattern("badvendor", occurrenceCount: 1, confirmCount: 0, rejectCount: 10)
        };

        var transactions = new List<TransactionSummaryDto>
        {
            new TransactionSummaryDto
            {
                Id = Guid.NewGuid(),
                Description = "BADVENDOR",
                Amount = 500m, // Also large amount mismatch
                TransactionDate = DateTime.UtcNow
            }
        };

        _patternRepoMock
            .Setup(p => p.GetActiveAsync(_testUserId))
            .ReturnsAsync(patterns);

        _transactionRepoMock
            .Setup(t => t.GetUnprocessedForPredictionAsync(_testUserId))
            .ReturnsAsync(transactions);

        // Act
        var result = await _sut.GenerateAllPendingPredictionsAsync(_testUserId);

        // Assert - low confidence predictions should not be created
        result.Should().Be(0);
    }

    [Fact]
    public async Task GenerateAllPendingPredictionsAsync_NoPatterns_ReturnsZero()
    {
        // Arrange - cold start scenario
        _patternRepoMock
            .Setup(p => p.GetActiveAsync(_testUserId))
            .ReturnsAsync(new List<ExpensePattern>());

        // Act
        var result = await _sut.GenerateAllPendingPredictionsAsync(_testUserId);

        // Assert
        result.Should().Be(0);
        _transactionRepoMock.Verify(t => t.GetUnprocessedForPredictionAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GenerateAllPendingPredictionsAsync_Performance_Under5SecondsFor1000Transactions()
    {
        // Arrange
        var patterns = new List<ExpensePattern>
        {
            CreateTestPattern("vendor1", occurrenceCount: 10),
            CreateTestPattern("vendor2", occurrenceCount: 10),
            CreateTestPattern("vendor3", occurrenceCount: 10)
        };

        var transactions = Enumerable.Range(0, 1000).Select(i => new TransactionSummaryDto
        {
            Id = Guid.NewGuid(),
            Description = $"VENDOR{i % 3 + 1} STORE {i}",
            Amount = 10m + (i % 50),
            TransactionDate = DateTime.UtcNow.AddDays(-i % 30)
        }).ToList();

        _patternRepoMock
            .Setup(p => p.GetActiveAsync(_testUserId))
            .ReturnsAsync(patterns);

        _transactionRepoMock
            .Setup(t => t.GetUnprocessedForPredictionAsync(_testUserId))
            .ReturnsAsync(transactions);

        _predictionRepoMock
            .Setup(p => p.AddAsync(It.IsAny<TransactionPrediction>()))
            .ReturnsAsync((TransactionPrediction tp) => tp);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _sut.GenerateAllPendingPredictionsAsync(_testUserId);
        stopwatch.Stop();

        // Assert - should complete in under 5 seconds
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    #endregion

    // Note: T055 GetPredictedTransactionsForPeriodAsync tests are covered by integration tests
    // in ReportsControllerIntegrationTests.cs (T056) because the method uses DbContext directly
    // rather than the repository pattern. See:
    // - GenerateDraft_WithHighConfidencePrediction_IncludesAutoSuggestedLine
    // - GenerateDraft_WithMediumConfidencePrediction_DoesNotAutoSuggest
    // - GenerateDraft_WithSuppressedPattern_DoesNotAutoSuggest
    // - GenerateDraft_WithMultiplePredictions_IncludesAllHighConfidence
    // - GenerateDraft_AutoSuggestedLine_UsesPredictedCategorization

    #region T067: ConfirmPredictionAsync Tests

    [Fact]
    public async Task ConfirmPredictionAsync_ValidPrediction_IncrementsPatternConfirmCount()
    {
        // Arrange
        var predictionId = Guid.NewGuid();
        var patternId = Guid.NewGuid();
        var pattern = CreateTestPattern("starbucks", confirmCount: 5, rejectCount: 1);
        pattern.Id = patternId;

        var prediction = new TransactionPrediction
        {
            Id = predictionId,
            UserId = _testUserId,
            PatternId = patternId,
            Status = PredictionStatus.Pending,
            ConfidenceScore = 0.85m
        };

        _predictionRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, predictionId))
            .ReturnsAsync(prediction);

        _patternRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, patternId))
            .ReturnsAsync(pattern);

        _patternRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<ExpensePattern>()))
            .Returns(Task.CompletedTask);

        _predictionRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<TransactionPrediction>()))
            .Returns(Task.CompletedTask);

        var request = new ConfirmPredictionRequestDto { PredictionId = predictionId };

        // Act
        var result = await _sut.ConfirmPredictionAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.NewStatus.Should().Be(PredictionStatus.Confirmed);
        _patternRepoMock.Verify(p => p.UpdateAsync(It.Is<ExpensePattern>(
            pat => pat.ConfirmCount == 6)), Times.Once);
    }

    [Fact]
    public async Task ConfirmPredictionAsync_NonExistingPrediction_ReturnsFalse()
    {
        // Arrange
        var predictionId = Guid.NewGuid();

        _predictionRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, predictionId))
            .ReturnsAsync((TransactionPrediction?)null);

        var request = new ConfirmPredictionRequestDto { PredictionId = predictionId };

        // Act
        var result = await _sut.ConfirmPredictionAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task ConfirmPredictionAsync_AlreadyConfirmed_ReturnsFalse()
    {
        // Arrange
        var predictionId = Guid.NewGuid();
        var prediction = new TransactionPrediction
        {
            Id = predictionId,
            UserId = _testUserId,
            PatternId = Guid.NewGuid(),
            Status = PredictionStatus.Confirmed, // Already confirmed
            ConfidenceScore = 0.85m
        };

        _predictionRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, predictionId))
            .ReturnsAsync(prediction);

        var request = new ConfirmPredictionRequestDto { PredictionId = predictionId };

        // Act
        var result = await _sut.ConfirmPredictionAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already");
    }

    #endregion

    #region T068: RejectPredictionAsync Tests

    [Fact]
    public async Task RejectPredictionAsync_ValidPrediction_IncrementsPatternRejectCount()
    {
        // Arrange
        var predictionId = Guid.NewGuid();
        var patternId = Guid.NewGuid();
        var pattern = CreateTestPattern("starbucks", confirmCount: 5, rejectCount: 1);
        pattern.Id = patternId;

        var prediction = new TransactionPrediction
        {
            Id = predictionId,
            UserId = _testUserId,
            PatternId = patternId,
            Status = PredictionStatus.Pending,
            ConfidenceScore = 0.85m
        };

        _predictionRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, predictionId))
            .ReturnsAsync(prediction);

        _patternRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, patternId))
            .ReturnsAsync(pattern);

        _patternRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<ExpensePattern>()))
            .Returns(Task.CompletedTask);

        _predictionRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<TransactionPrediction>()))
            .Returns(Task.CompletedTask);

        var request = new RejectPredictionRequestDto { PredictionId = predictionId };

        // Act
        var result = await _sut.RejectPredictionAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.NewStatus.Should().Be(PredictionStatus.Rejected);
        _patternRepoMock.Verify(p => p.UpdateAsync(It.Is<ExpensePattern>(
            pat => pat.RejectCount == 2)), Times.Once);
    }

    [Fact]
    public async Task RejectPredictionAsync_PatternExceedsThreshold_AutoSuppresses()
    {
        // Arrange - Pattern with >3 rejects and <30% confirm rate
        // Current: 1 confirm, 3 rejects. After this reject: 1 confirm, 4 rejects = 20% confirm rate
        var predictionId = Guid.NewGuid();
        var patternId = Guid.NewGuid();
        var pattern = CreateTestPattern("badvendor", confirmCount: 1, rejectCount: 3);
        pattern.Id = patternId;
        pattern.IsSuppressed = false;

        var prediction = new TransactionPrediction
        {
            Id = predictionId,
            UserId = _testUserId,
            PatternId = patternId,
            Status = PredictionStatus.Pending,
            ConfidenceScore = 0.60m
        };

        _predictionRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, predictionId))
            .ReturnsAsync(prediction);

        _patternRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, patternId))
            .ReturnsAsync(pattern);

        _patternRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<ExpensePattern>()))
            .Returns(Task.CompletedTask);

        _predictionRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<TransactionPrediction>()))
            .Returns(Task.CompletedTask);

        var request = new RejectPredictionRequestDto { PredictionId = predictionId };

        // Act
        var result = await _sut.RejectPredictionAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.PatternSuppressed.Should().BeTrue();
        result.Message.Should().Contain("auto-suppressed");
        _patternRepoMock.Verify(p => p.UpdateAsync(It.Is<ExpensePattern>(
            pat => pat.IsSuppressed == true && pat.RejectCount == 4)), Times.Once);
    }

    [Fact]
    public async Task RejectPredictionAsync_PatternBelowThreshold_DoesNotSuppress()
    {
        // Arrange - Pattern with 3 rejects but 50% confirm rate (3 confirms, 3 rejects)
        // After reject: 3 confirms, 4 rejects = 42.8% confirm rate - still above 30%
        var predictionId = Guid.NewGuid();
        var patternId = Guid.NewGuid();
        var pattern = CreateTestPattern("mixedvendor", confirmCount: 3, rejectCount: 3);
        pattern.Id = patternId;
        pattern.IsSuppressed = false;

        var prediction = new TransactionPrediction
        {
            Id = predictionId,
            UserId = _testUserId,
            PatternId = patternId,
            Status = PredictionStatus.Pending,
            ConfidenceScore = 0.70m
        };

        _predictionRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, predictionId))
            .ReturnsAsync(prediction);

        _patternRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, patternId))
            .ReturnsAsync(pattern);

        _patternRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<ExpensePattern>()))
            .Returns(Task.CompletedTask);

        _predictionRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<TransactionPrediction>()))
            .Returns(Task.CompletedTask);

        var request = new RejectPredictionRequestDto { PredictionId = predictionId };

        // Act
        var result = await _sut.RejectPredictionAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.PatternSuppressed.Should().BeFalse();
        _patternRepoMock.Verify(p => p.UpdateAsync(It.Is<ExpensePattern>(
            pat => pat.IsSuppressed == false && pat.RejectCount == 4)), Times.Once);
    }

    [Fact]
    public async Task RejectPredictionAsync_AlreadySuppressedPattern_DoesNotSuppressAgain()
    {
        // Arrange - Pattern already suppressed
        var predictionId = Guid.NewGuid();
        var patternId = Guid.NewGuid();
        var pattern = CreateTestPattern("suppressedvendor", confirmCount: 0, rejectCount: 10);
        pattern.Id = patternId;
        pattern.IsSuppressed = true; // Already suppressed

        var prediction = new TransactionPrediction
        {
            Id = predictionId,
            UserId = _testUserId,
            PatternId = patternId,
            Status = PredictionStatus.Pending,
            ConfidenceScore = 0.55m
        };

        _predictionRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, predictionId))
            .ReturnsAsync(prediction);

        _patternRepoMock
            .Setup(p => p.GetByIdAsync(_testUserId, patternId))
            .ReturnsAsync(pattern);

        _patternRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<ExpensePattern>()))
            .Returns(Task.CompletedTask);

        _predictionRepoMock
            .Setup(p => p.UpdateAsync(It.IsAny<TransactionPrediction>()))
            .Returns(Task.CompletedTask);

        var request = new RejectPredictionRequestDto { PredictionId = predictionId };

        // Act
        var result = await _sut.RejectPredictionAsync(_testUserId, request);

        // Assert
        result.Success.Should().BeTrue();
        result.PatternSuppressed.Should().BeFalse(); // Not "just" suppressed since it was already
        result.Message.Should().NotContain("auto-suppressed");
    }

    #endregion

    #region Helper Methods

    private static ExpensePattern CreateTestPattern(
        string normalizedVendor = "testvendor",
        int occurrenceCount = 5,
        int confirmCount = 3,
        int rejectCount = 0,
        decimal averageAmount = 50m)
    {
        return new ExpensePattern
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            NormalizedVendor = normalizedVendor,
            DisplayName = normalizedVendor.ToUpperInvariant(),
            DefaultCategory = "Test Category",
            AverageAmount = averageAmount,
            MinAmount = averageAmount * 0.5m,
            MaxAmount = averageAmount * 1.5m,
            OccurrenceCount = occurrenceCount,
            ConfirmCount = confirmCount,
            RejectCount = rejectCount,
            LastSeenAt = DateTime.UtcNow,
            IsSuppressed = false
        };
    }

    private static ExpenseLine CreateTestExpenseLine(string vendor, decimal amount, string category)
    {
        return new ExpenseLine
        {
            Id = Guid.NewGuid(),
            Description = vendor,
            Amount = amount,
            Category = category,
            TransactionDate = DateTime.UtcNow,
            ReceiptId = Guid.NewGuid()
        };
    }

    #endregion
}
