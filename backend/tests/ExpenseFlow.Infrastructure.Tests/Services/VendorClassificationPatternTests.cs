using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Repositories;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using ExpenseFlow.TestCommon;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Tests for vendor classification patterns feature.
///
/// Classification Rules:
/// - Business: 50%+ confirm rate AND total count >= 1
/// - Personal: 75%+ reject rate AND total count >= 4
/// - Otherwise: null (undetermined)
///
/// The ActiveClassification property is calculated from ConfirmCount and RejectCount.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class VendorClassificationPatternTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ExpensePredictionService _service;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<ITransactionPredictionRepository> _predictionRepositoryMock;
    private readonly ExpensePatternRepository _patternRepository;
    private readonly Guid _testUserId = Guid.NewGuid();

    public VendorClassificationPatternTests()
    {
        // Create InMemory database with unique name for test isolation
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: $"ExpenseFlow_Classification_{Guid.NewGuid()}")
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);

        // Setup mocks
        _vendorAliasServiceMock = new Mock<IVendorAliasService>();
        _vendorAliasServiceMock
            .Setup(x => x.FindMatchingAliasAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => null); // Return null to use default normalization

        _predictionRepositoryMock = new Mock<ITransactionPredictionRepository>();

        // Create real repository using the same DbContext
        _patternRepository = new ExpensePatternRepository(_dbContext);

        // Create service under test
        _service = new ExpensePredictionService(
            _dbContext,
            _patternRepository,
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

    #region Classification Calculation Tests

    [Fact]
    public void ActiveClassification_Returns_Business_When_ConfirmRate_50Percent_MinCount1()
    {
        // Arrange: Pattern with 1 confirm, 0 rejects (100% confirm rate, count = 1)
        var pattern = CreatePattern(confirmCount: 1, rejectCount: 0);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be classified as business (true)
        classification.Should().BeTrue("pattern has 100% confirm rate with 1+ total count");
    }

    [Fact]
    public void ActiveClassification_Returns_Business_When_ConfirmRate_Exactly50Percent()
    {
        // Arrange: Pattern with 2 confirms, 2 rejects (50% confirm rate, count = 4)
        var pattern = CreatePattern(confirmCount: 2, rejectCount: 2);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be classified as business (50% >= 50%)
        classification.Should().BeTrue("pattern has exactly 50% confirm rate");
    }

    [Fact]
    public void ActiveClassification_Returns_Business_When_ConfirmRate_Above50Percent()
    {
        // Arrange: Pattern with 6 confirms, 4 rejects (60% confirm rate, count = 10)
        var pattern = CreatePattern(confirmCount: 6, rejectCount: 4);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be classified as business
        classification.Should().BeTrue("pattern has 60% confirm rate");
    }

    [Fact]
    public void ActiveClassification_Returns_Personal_When_RejectRate_75Percent_MinCount4()
    {
        // Arrange: Pattern with 1 confirm, 3 rejects (75% reject rate, count = 4)
        var pattern = CreatePattern(confirmCount: 1, rejectCount: 3);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be classified as personal (false)
        classification.Should().BeFalse("pattern has 75% reject rate with 4+ total count");
    }

    [Fact]
    public void ActiveClassification_Returns_Personal_When_RejectRate_Above75Percent()
    {
        // Arrange: Pattern with 0 confirms, 5 rejects (100% reject rate, count = 5)
        var pattern = CreatePattern(confirmCount: 0, rejectCount: 5);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be classified as personal
        classification.Should().BeFalse("pattern has 100% reject rate");
    }

    [Fact]
    public void ActiveClassification_Returns_Null_When_BelowThresholds()
    {
        // Arrange: Pattern with 2 confirms, 3 rejects (40% confirm rate, 60% reject rate, count = 5)
        // Not 50%+ confirm, not 75%+ reject
        var pattern = CreatePattern(confirmCount: 2, rejectCount: 3);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be undetermined (null)
        classification.Should().BeNull("pattern doesn't meet either threshold (40% confirm, 60% reject)");
    }

    [Fact]
    public void ActiveClassification_Returns_Null_When_InsufficientData_ForPersonal()
    {
        // Arrange: Pattern with 0 confirms, 3 rejects (100% reject but only 3 count)
        // Needs 4+ for personal classification
        var pattern = CreatePattern(confirmCount: 0, rejectCount: 3);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be undetermined because count < 4 for personal
        classification.Should().BeNull("pattern needs 4+ total count for personal classification");
    }

    [Fact]
    public void ActiveClassification_Returns_Null_When_NoFeedback()
    {
        // Arrange: Pattern with no feedback
        var pattern = CreatePattern(confirmCount: 0, rejectCount: 0);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be undetermined
        classification.Should().BeNull("pattern has no feedback data");
    }

    [Fact]
    public void ActiveClassification_Business_Takes_Precedence_Over_Personal_At_Boundary()
    {
        // Arrange: Edge case - 2 confirms, 2 rejects = 50% confirm AND 50% reject
        // Business threshold (50%+ confirm) is met, personal threshold (75%+ reject) is NOT met
        var pattern = CreatePattern(confirmCount: 2, rejectCount: 2);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be business because confirm >= 50%
        classification.Should().BeTrue("business classification takes precedence when confirm rate >= 50%");
    }

    #endregion

    #region ConfirmPrediction Updates Classification Tests

    [Fact]
    public async Task ConfirmPredictionAsync_UpdatesPatternClassification_ToBusinessWhenThresholdMet()
    {
        // Arrange: Create a pattern with 0 confirms, 0 rejects (unclassified)
        var pattern = await CreatePatternInDbAsync(confirmCount: 0, rejectCount: 0);
        var prediction = await CreatePredictionInDbAsync(pattern.Id);

        _predictionRepositoryMock
            .Setup(x => x.GetByIdAsync(_testUserId, prediction.Id))
            .ReturnsAsync(prediction);

        // Act: Confirm the prediction
        var result = await _service.ConfirmPredictionAsync(_testUserId, new ConfirmPredictionRequestDto
        {
            PredictionId = prediction.Id
        });

        // Assert
        result.Success.Should().BeTrue();

        var updatedPattern = await _dbContext.ExpensePatterns.FindAsync(pattern.Id);
        updatedPattern!.ConfirmCount.Should().Be(1);
        updatedPattern.ActiveClassification.Should().BeTrue("1 confirm with 0 rejects = 100% confirm rate >= 50%");
    }

    [Fact]
    public async Task ConfirmPredictionAsync_UpdatesPatternClassification_FromPersonalToUndetermined()
    {
        // Arrange: Create a pattern currently classified as personal (1 confirm, 4 rejects = 80% reject)
        var pattern = await CreatePatternInDbAsync(confirmCount: 1, rejectCount: 4);
        pattern.ActiveClassification.Should().BeFalse("starting as personal");

        var prediction = await CreatePredictionInDbAsync(pattern.Id);

        _predictionRepositoryMock
            .Setup(x => x.GetByIdAsync(_testUserId, prediction.Id))
            .ReturnsAsync(prediction);

        // Act: Confirm the prediction (now 2 confirms, 4 rejects = 33% confirm, 67% reject)
        var result = await _service.ConfirmPredictionAsync(_testUserId, new ConfirmPredictionRequestDto
        {
            PredictionId = prediction.Id
        });

        // Assert
        result.Success.Should().BeTrue();

        var updatedPattern = await _dbContext.ExpensePatterns.FindAsync(pattern.Id);
        updatedPattern!.ConfirmCount.Should().Be(2);
        updatedPattern.ActiveClassification.Should().BeNull("33% confirm, 67% reject - neither threshold met");
    }

    #endregion

    #region RejectPrediction Updates Classification Tests

    [Fact]
    public async Task RejectPredictionAsync_UpdatesPatternClassification_ToPersonalWhenThresholdMet()
    {
        // Arrange: Create a pattern with 1 confirm, 2 rejects (need 1 more reject to reach 75% with count 4)
        var pattern = await CreatePatternInDbAsync(confirmCount: 1, rejectCount: 2);
        pattern.ActiveClassification.Should().BeNull("starting as undetermined");

        var prediction = await CreatePredictionInDbAsync(pattern.Id);

        _predictionRepositoryMock
            .Setup(x => x.GetByIdAsync(_testUserId, prediction.Id))
            .ReturnsAsync(prediction);

        // Act: Reject the prediction (now 1 confirm, 3 rejects = 75% reject with count 4)
        var result = await _service.RejectPredictionAsync(_testUserId, new RejectPredictionRequestDto
        {
            PredictionId = prediction.Id
        });

        // Assert
        result.Success.Should().BeTrue();

        var updatedPattern = await _dbContext.ExpensePatterns.FindAsync(pattern.Id);
        updatedPattern!.RejectCount.Should().Be(3);
        updatedPattern.ActiveClassification.Should().BeFalse("75% reject rate with 4 total count = personal");
    }

    [Fact]
    public async Task RejectPredictionAsync_UpdatesPatternClassification_FromBusinessToUndetermined()
    {
        // Arrange: Create a pattern currently classified as business (3 confirms, 1 reject = 75% confirm)
        var pattern = await CreatePatternInDbAsync(confirmCount: 3, rejectCount: 1);
        pattern.ActiveClassification.Should().BeTrue("starting as business");

        var prediction = await CreatePredictionInDbAsync(pattern.Id);

        _predictionRepositoryMock
            .Setup(x => x.GetByIdAsync(_testUserId, prediction.Id))
            .ReturnsAsync(prediction);

        // Act: Reject the prediction (now 3 confirms, 2 rejects = 60% confirm)
        var result = await _service.RejectPredictionAsync(_testUserId, new RejectPredictionRequestDto
        {
            PredictionId = prediction.Id
        });

        // Assert
        result.Success.Should().BeTrue();

        var updatedPattern = await _dbContext.ExpensePatterns.FindAsync(pattern.Id);
        updatedPattern!.RejectCount.Should().Be(2);
        // 60% confirm rate still >= 50%, so still business
        updatedPattern.ActiveClassification.Should().BeTrue("60% confirm rate still meets business threshold");
    }

    [Fact]
    public async Task RejectPredictionAsync_UpdatesPatternClassification_BusinessToUndetermined_EdgeCase()
    {
        // Arrange: Create a pattern currently classified as business (1 confirm, 1 reject = 50% confirm)
        var pattern = await CreatePatternInDbAsync(confirmCount: 1, rejectCount: 1);
        pattern.ActiveClassification.Should().BeTrue("starting as business at 50% confirm");

        var prediction = await CreatePredictionInDbAsync(pattern.Id);

        _predictionRepositoryMock
            .Setup(x => x.GetByIdAsync(_testUserId, prediction.Id))
            .ReturnsAsync(prediction);

        // Act: Reject the prediction (now 1 confirm, 2 rejects = 33% confirm)
        var result = await _service.RejectPredictionAsync(_testUserId, new RejectPredictionRequestDto
        {
            PredictionId = prediction.Id
        });

        // Assert
        result.Success.Should().BeTrue();

        var updatedPattern = await _dbContext.ExpensePatterns.FindAsync(pattern.Id);
        updatedPattern!.RejectCount.Should().Be(2);
        // 33% confirm, 67% reject - neither threshold met
        updatedPattern.ActiveClassification.Should().BeNull("33% confirm, 67% reject - undetermined");
    }

    #endregion

    #region Prediction Matching Skips Personal Patterns Tests

    [Fact]
    public async Task GeneratePredictionsAsync_SkipsPersonalPatterns()
    {
        // Arrange: Create a pattern classified as personal
        var personalPattern = await CreatePatternInDbAsync(
            confirmCount: 0,
            rejectCount: 5,
            vendorName: "PERSONAL_VENDOR",
            occurrenceCount: 5);
        personalPattern.ActiveClassification.Should().BeFalse("should be personal");

        // Create a transaction matching this vendor
        var transaction = await CreateTransactionInDbAsync("PERSONAL_VENDOR");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(transaction.Id))
            .ReturnsAsync(false);

        // Act: Generate predictions
        var predictionsGenerated = await _service.GeneratePredictionsAsync(_testUserId, new[] { transaction.Id });

        // Assert: No prediction should be generated because pattern is personal
        predictionsGenerated.Should().Be(0, "personal patterns should be skipped");
    }

    [Fact]
    public async Task GeneratePredictionsAsync_IncludesBusinessPatterns()
    {
        // Arrange: Create a pattern classified as business
        var businessPattern = await CreatePatternInDbAsync(
            confirmCount: 5,
            rejectCount: 0,
            vendorName: "BUSINESS_VENDOR",
            occurrenceCount: 5);
        businessPattern.ActiveClassification.Should().BeTrue("should be business");

        // Create a transaction matching this vendor
        var transaction = await CreateTransactionInDbAsync("BUSINESS_VENDOR");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(transaction.Id))
            .ReturnsAsync(false);

        // Act: Generate predictions
        var predictionsGenerated = await _service.GeneratePredictionsAsync(_testUserId, new[] { transaction.Id });

        // Assert: Prediction should be generated
        predictionsGenerated.Should().Be(1, "business patterns should generate predictions");
    }

    [Fact]
    public async Task GeneratePredictionsAsync_IncludesUndeterminedPatterns()
    {
        // Arrange: Create a pattern with undetermined classification
        var undeterminedPattern = await CreatePatternInDbAsync(
            confirmCount: 2,
            rejectCount: 3,
            vendorName: "UNDETERMINED_VENDOR",
            occurrenceCount: 5);
        undeterminedPattern.ActiveClassification.Should().BeNull("should be undetermined");

        // Create a transaction matching this vendor
        var transaction = await CreateTransactionInDbAsync("UNDETERMINED_VENDOR");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(transaction.Id))
            .ReturnsAsync(false);

        // Act: Generate predictions
        var predictionsGenerated = await _service.GeneratePredictionsAsync(_testUserId, new[] { transaction.Id });

        // Assert: Prediction should be generated (undetermined patterns are included)
        predictionsGenerated.Should().Be(1, "undetermined patterns should generate predictions");
    }

    [Fact]
    public async Task GeneratePredictionsAsync_MixedPatterns_OnlyGeneratesForNonPersonal()
    {
        // Arrange: Create multiple patterns with different classifications
        var businessPattern = await CreatePatternInDbAsync(
            confirmCount: 5, rejectCount: 0, vendorName: "BUSINESS_CO", occurrenceCount: 5);
        var personalPattern = await CreatePatternInDbAsync(
            confirmCount: 0, rejectCount: 5, vendorName: "PERSONAL_SHOP", occurrenceCount: 5);
        var undeterminedPattern = await CreatePatternInDbAsync(
            confirmCount: 2, rejectCount: 2, vendorName: "MIXED_VENDOR", occurrenceCount: 4);

        // Create transactions for each
        var tx1 = await CreateTransactionInDbAsync("BUSINESS_CO");
        var tx2 = await CreateTransactionInDbAsync("PERSONAL_SHOP");
        var tx3 = await CreateTransactionInDbAsync("MIXED_VENDOR");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Act: Generate predictions for all
        var predictionsGenerated = await _service.GeneratePredictionsAsync(
            _testUserId,
            new[] { tx1.Id, tx2.Id, tx3.Id });

        // Assert: Should skip personal pattern
        predictionsGenerated.Should().Be(2, "should skip personal pattern, include business and undetermined");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ConfirmPredictionAsync_ManualOverrideWithNoPattern_DoesNotThrow()
    {
        // Arrange: Create a prediction without a pattern (manual override)
        var prediction = await CreateManualPredictionInDbAsync();

        _predictionRepositoryMock
            .Setup(x => x.GetByIdAsync(_testUserId, prediction.Id))
            .ReturnsAsync(prediction);

        // Act & Assert: Should not throw
        var result = await _service.ConfirmPredictionAsync(_testUserId, new ConfirmPredictionRequestDto
        {
            PredictionId = prediction.Id
        });

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RejectPredictionAsync_ManualOverrideWithNoPattern_DoesNotThrow()
    {
        // Arrange: Create a prediction without a pattern (manual override)
        var prediction = await CreateManualPredictionInDbAsync();

        _predictionRepositoryMock
            .Setup(x => x.GetByIdAsync(_testUserId, prediction.Id))
            .ReturnsAsync(prediction);

        // Act & Assert: Should not throw
        var result = await _service.RejectPredictionAsync(_testUserId, new RejectPredictionRequestDto
        {
            PredictionId = prediction.Id
        });

        result.Success.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private ExpensePattern CreatePattern(int confirmCount, int rejectCount)
    {
        return new ExpensePattern
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            NormalizedVendor = $"VENDOR_{Guid.NewGuid():N}".Substring(0, 20),
            DisplayName = "Test Vendor",
            Category = "6000",
            AverageAmount = 50.00m,
            MinAmount = 25.00m,
            MaxAmount = 75.00m,
            OccurrenceCount = confirmCount + rejectCount + 2, // Ensure minimum occurrence count
            LastSeenAt = DateTime.UtcNow,
            ConfirmCount = confirmCount,
            RejectCount = rejectCount,
            IsSuppressed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private async Task<ExpensePattern> CreatePatternInDbAsync(
        int confirmCount,
        int rejectCount,
        string vendorName = "TEST_VENDOR",
        int? occurrenceCount = null)
    {
        var pattern = new ExpensePattern
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            NormalizedVendor = vendorName.ToUpperInvariant(),
            DisplayName = vendorName,
            Category = "6000",
            AverageAmount = 50.00m,
            MinAmount = 25.00m,
            MaxAmount = 75.00m,
            OccurrenceCount = occurrenceCount ?? Math.Max(2, confirmCount + rejectCount + 2),
            LastSeenAt = DateTime.UtcNow,
            ConfirmCount = confirmCount,
            RejectCount = rejectCount,
            IsSuppressed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.ExpensePatterns.Add(pattern);
        await _dbContext.SaveChangesAsync();

        return pattern;
    }

    private async Task<TransactionPrediction> CreatePredictionInDbAsync(Guid patternId)
    {
        // Create a transaction first
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ImportId = Guid.NewGuid(),
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PostDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "Test Transaction",
            OriginalDescription = "Test Transaction",
            Amount = 50.00m,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Transactions.Add(transaction);

        var prediction = new TransactionPrediction
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            UserId = _testUserId,
            PatternId = patternId,
            ConfidenceScore = 0.85m,
            ConfidenceLevel = PredictionConfidence.High,
            Status = PredictionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.TransactionPredictions.Add(prediction);

        await _dbContext.SaveChangesAsync();

        // Load the pattern navigation property
        prediction.Pattern = await _dbContext.ExpensePatterns.FindAsync(patternId);

        return prediction;
    }

    private async Task<TransactionPrediction> CreateManualPredictionInDbAsync()
    {
        // Create a transaction first
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ImportId = Guid.NewGuid(),
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PostDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "Manual Override Transaction",
            OriginalDescription = "Manual Override Transaction",
            Amount = 50.00m,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Transactions.Add(transaction);

        var prediction = new TransactionPrediction
        {
            Id = Guid.NewGuid(),
            TransactionId = transaction.Id,
            UserId = _testUserId,
            PatternId = null, // No pattern - manual override
            ConfidenceScore = 1.0m,
            ConfidenceLevel = PredictionConfidence.High,
            Status = PredictionStatus.Pending,
            IsManualOverride = true,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.TransactionPredictions.Add(prediction);

        await _dbContext.SaveChangesAsync();

        return prediction;
    }

    private async Task<Transaction> CreateTransactionInDbAsync(string vendorDescription)
    {
        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ImportId = Guid.NewGuid(),
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PostDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = vendorDescription,
            OriginalDescription = vendorDescription,
            Amount = 50.00m,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        return transaction;
    }

    #endregion
}
