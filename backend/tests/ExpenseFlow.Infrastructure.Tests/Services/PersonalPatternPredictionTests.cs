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
/// Tests for personal transaction pattern matching feature.
///
/// NEW Classification Rules (lowered personal threshold):
/// - Business: 50%+ confirm rate AND total count >= 1 (unchanged)
/// - Personal: 60%+ reject rate AND total count >= 3 (was 75%/4)
/// - Otherwise: null (undetermined)
///
/// NEW Behavior:
/// - Personal patterns now generate predictions with IsPersonalPrediction = true
/// - Previously personal patterns were SKIPPED entirely
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class PersonalPatternPredictionTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ExpensePredictionService _service;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<ITransactionPredictionRepository> _predictionRepositoryMock;
    private readonly ExpensePatternRepository _patternRepository;
    private readonly Guid _testUserId = Guid.NewGuid();

    public PersonalPatternPredictionTests()
    {
        // Create InMemory database with unique name for test isolation
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: $"ExpenseFlow_PersonalPattern_{Guid.NewGuid()}")
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

    #region New Personal Threshold Tests (60%/3)

    [Fact]
    public void ActiveClassification_Returns_Personal_At_NewThreshold_60Percent_MinCount3()
    {
        // Arrange: Pattern with 60% reject rate and 3 total count
        // 1 confirm, 2 rejects = 67% reject rate >= 60%, count = 3
        var pattern = CreatePattern(confirmCount: 1, rejectCount: 2);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be classified as personal (false) with new threshold
        classification.Should().BeFalse("pattern has 67% reject rate with 3 total count - meets new 60%/3 threshold");
    }

    [Fact]
    public void ActiveClassification_Returns_Personal_At_Exactly_60Percent_Reject()
    {
        // Arrange: Pattern with exactly 60% reject rate
        // 2 confirms, 3 rejects = 60% reject rate, count = 5
        var pattern = CreatePattern(confirmCount: 2, rejectCount: 3);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be classified as personal at exactly 60%
        classification.Should().BeFalse("pattern has exactly 60% reject rate with 5 total count");
    }

    [Fact]
    public void ActiveClassification_Returns_Null_When_BelowNewThreshold_InsufficientCount()
    {
        // Arrange: Pattern with high reject rate but only 2 count
        // 0 confirms, 2 rejects = 100% reject but count < 3
        var pattern = CreatePattern(confirmCount: 0, rejectCount: 2);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be undetermined because count < 3 for personal
        classification.Should().BeNull("pattern needs 3+ total count for personal classification");
    }

    [Fact]
    public void ActiveClassification_Returns_Null_When_RejectRate_Below60Percent()
    {
        // Arrange: Pattern with 55% reject rate (below new threshold)
        // 9 confirms, 11 rejects = 55% reject rate
        var pattern = CreatePattern(confirmCount: 9, rejectCount: 11);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should be undetermined because 55% < 60%
        classification.Should().BeNull("pattern has 55% reject rate which is below 60% threshold");
    }

    [Theory]
    [InlineData(0, 3, false, "100% reject, 3 count")] // Personal
    [InlineData(1, 2, false, "67% reject, 3 count")]  // Personal
    [InlineData(1, 3, false, "75% reject, 4 count")]  // Personal
    [InlineData(2, 3, false, "60% reject, 5 count")]  // Personal at boundary
    [InlineData(0, 2, null, "100% reject but only 2 count")] // Undetermined - insufficient count
    [InlineData(2, 2, true, "50% confirm, 4 count")] // Business (confirm takes precedence)
    [InlineData(3, 2, true, "60% confirm, 5 count")] // Business
    public void ActiveClassification_HandlesVariousScenarios(
        int confirmCount,
        int rejectCount,
        bool? expectedClassification,
        string scenario)
    {
        // Arrange
        var pattern = CreatePattern(confirmCount, rejectCount);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert
        classification.Should().Be(expectedClassification, scenario);
    }

    #endregion

    #region Personal Prediction Generation Tests (IsPersonalPrediction)

    [Fact]
    public async Task GeneratePredictionsAsync_CreatesPersonalPrediction_WithIsPersonalFlag()
    {
        // Arrange: Create a pattern classified as personal (using new threshold)
        var personalPattern = await CreatePatternInDbAsync(
            confirmCount: 1,
            rejectCount: 2, // 67% reject, 3 count = personal
            vendorName: "PERSONAL_VENDOR",
            occurrenceCount: 5);
        personalPattern.ActiveClassification.Should().BeFalse("should be personal");

        // Create a transaction matching this vendor
        var transaction = await CreateTransactionInDbAsync("PERSONAL_VENDOR");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(transaction.Id))
            .ReturnsAsync(false);

        // Capture the prediction when it's added
        TransactionPrediction? capturedPrediction = null;
        _predictionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<TransactionPrediction>()))
            .Callback<TransactionPrediction>(p => capturedPrediction = p)
            .Returns(Task.CompletedTask);

        // Act: Generate predictions
        var predictionsGenerated = await _service.GeneratePredictionsAsync(_testUserId, new[] { transaction.Id });

        // Assert: Prediction should be generated with IsPersonalPrediction = true
        predictionsGenerated.Should().Be(1, "personal patterns should now generate predictions");

        // Verify the prediction was created with IsPersonalPrediction flag
        capturedPrediction.Should().NotBeNull("prediction should be created for personal pattern");
        capturedPrediction!.IsPersonalPrediction.Should().BeTrue("prediction from personal pattern should have IsPersonalPrediction = true");
        capturedPrediction.PatternId.Should().Be(personalPattern.Id);
    }

    [Fact]
    public async Task GeneratePredictionsAsync_BusinessPrediction_HasIsPersonalFlagFalse()
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

        // Capture the prediction when it's added
        TransactionPrediction? capturedPrediction = null;
        _predictionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<TransactionPrediction>()))
            .Callback<TransactionPrediction>(p => capturedPrediction = p)
            .Returns(Task.CompletedTask);

        // Act: Generate predictions
        var predictionsGenerated = await _service.GeneratePredictionsAsync(_testUserId, new[] { transaction.Id });

        // Assert
        predictionsGenerated.Should().Be(1);

        capturedPrediction.Should().NotBeNull();
        capturedPrediction!.IsPersonalPrediction.Should().BeFalse("business pattern predictions should have IsPersonalPrediction = false");
    }

    [Fact]
    public async Task GeneratePredictionsAsync_UndeterminedPrediction_HasIsPersonalFlagFalse()
    {
        // Arrange: Create a pattern with truly undetermined classification
        // Need: <50% confirm AND <60% reject
        // 9 confirms, 11 rejects = 45% confirm, 55% reject - undetermined
        var undeterminedPattern = await CreatePatternInDbAsync(
            confirmCount: 9,
            rejectCount: 11, // 45% confirm, 55% reject - undetermined
            vendorName: "UNDETERMINED_VENDOR",
            occurrenceCount: 20);
        undeterminedPattern.ActiveClassification.Should().BeNull("should be undetermined (45% confirm, 55% reject)");

        // Create a transaction matching this vendor
        var transaction = await CreateTransactionInDbAsync("UNDETERMINED_VENDOR");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(transaction.Id))
            .ReturnsAsync(false);

        // Capture the prediction when it's added
        TransactionPrediction? capturedPrediction = null;
        _predictionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<TransactionPrediction>()))
            .Callback<TransactionPrediction>(p => capturedPrediction = p)
            .Returns(Task.CompletedTask);

        // Act
        var predictionsGenerated = await _service.GeneratePredictionsAsync(_testUserId, new[] { transaction.Id });

        // Assert
        predictionsGenerated.Should().Be(1);

        capturedPrediction.Should().NotBeNull();
        capturedPrediction!.IsPersonalPrediction.Should().BeFalse("undetermined pattern predictions should have IsPersonalPrediction = false");
    }

    [Fact]
    public async Task GeneratePredictionsAsync_MixedPatterns_SetsIsPersonalFlagCorrectly()
    {
        // Arrange: Create patterns with different classifications
        var businessPattern = await CreatePatternInDbAsync(
            confirmCount: 5, rejectCount: 0, vendorName: "BUSINESS_CO", occurrenceCount: 5);
        var personalPattern = await CreatePatternInDbAsync(
            confirmCount: 1, rejectCount: 3, vendorName: "PERSONAL_SHOP", occurrenceCount: 5); // 75% reject

        // Create transactions for each
        var txBusiness = await CreateTransactionInDbAsync("BUSINESS_CO");
        var txPersonal = await CreateTransactionInDbAsync("PERSONAL_SHOP");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(It.IsAny<Guid>()))
            .ReturnsAsync(false);

        // Capture all predictions when they're added
        var capturedPredictions = new List<TransactionPrediction>();
        _predictionRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<TransactionPrediction>()))
            .Callback<TransactionPrediction>(p => capturedPredictions.Add(p))
            .Returns(Task.CompletedTask);

        // Act: Generate predictions for all
        var predictionsGenerated = await _service.GeneratePredictionsAsync(
            _testUserId,
            new[] { txBusiness.Id, txPersonal.Id });

        // Assert: Both should generate predictions
        predictionsGenerated.Should().Be(2, "both business and personal patterns should generate predictions");
        capturedPredictions.Should().HaveCount(2);

        // Verify business prediction
        var businessPrediction = capturedPredictions.FirstOrDefault(p => p.TransactionId == txBusiness.Id);
        businessPrediction.Should().NotBeNull();
        businessPrediction!.IsPersonalPrediction.Should().BeFalse();

        // Verify personal prediction
        var personalPrediction = capturedPredictions.FirstOrDefault(p => p.TransactionId == txPersonal.Id);
        personalPrediction.Should().NotBeNull();
        personalPrediction!.IsPersonalPrediction.Should().BeTrue();
    }

    #endregion

    #region DTO Mapping Tests

    [Fact]
    public async Task GetPredictionForTransactionAsync_Returns_IsPersonalPrediction_InDto()
    {
        // Arrange: Create personal pattern and prediction
        var personalPattern = await CreatePatternInDbAsync(
            confirmCount: 0,
            rejectCount: 3, // 100% reject, 3 count = personal
            vendorName: "PERSONAL_STORE",
            occurrenceCount: 5);

        var transaction = await CreateTransactionInDbAsync("PERSONAL_STORE");
        var prediction = await CreatePredictionInDbAsync(
            transaction.Id,
            personalPattern.Id,
            isPersonalPrediction: true);

        _predictionRepositoryMock
            .Setup(x => x.GetByTransactionIdAsync(_testUserId, transaction.Id))
            .ReturnsAsync(prediction);

        // Act
        var dto = await _service.GetPredictionForTransactionAsync(_testUserId, transaction.Id);

        // Assert
        dto.Should().NotBeNull();
        dto!.IsPersonalPrediction.Should().BeTrue("DTO should expose IsPersonalPrediction flag");
    }

    [Fact]
    public void PatternSummaryDto_Includes_ActiveClassification()
    {
        // This test verifies the DTO has the ActiveClassification property
        // The mapping will be tested in integration

        // Arrange
        var dto = new PatternSummaryDto();

        // Act & Assert - property should exist and be settable
        dto.ActiveClassification = "Personal";
        dto.ActiveClassification.Should().Be("Personal");

        dto.ActiveClassification = "Business";
        dto.ActiveClassification.Should().Be("Business");

        dto.ActiveClassification = null;
        dto.ActiveClassification.Should().BeNull();
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void OldPersonalThreshold_75Percent_4Count_StillClassifiesAsPersonal()
    {
        // Arrange: Pattern that meets OLD threshold (75%/4) should still be personal
        var pattern = CreatePattern(confirmCount: 1, rejectCount: 3); // 75% reject, 4 count

        // Act
        var classification = pattern.ActiveClassification;

        // Assert: Should still be personal (old threshold is more restrictive)
        classification.Should().BeFalse("patterns meeting old 75%/4 threshold should still be personal");
    }

    [Fact]
    public void BusinessThreshold_Unchanged_50Percent_1Count()
    {
        // Arrange: Business threshold should remain 50%/1
        var pattern = CreatePattern(confirmCount: 1, rejectCount: 0); // 100% confirm, 1 count

        // Act
        var classification = pattern.ActiveClassification;

        // Assert
        classification.Should().BeTrue("business threshold remains 50%+ confirm with 1+ count");
    }

    [Fact]
    public void BusinessStillTakesPrecedence_Over_Personal()
    {
        // Arrange: 50% confirm AND 50% reject - business wins
        var pattern = CreatePattern(confirmCount: 2, rejectCount: 2);

        // Act
        var classification = pattern.ActiveClassification;

        // Assert
        classification.Should().BeTrue("business classification (50%+ confirm) takes precedence");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task SuppressedPersonalPattern_DoesNotGeneratePrediction()
    {
        // Arrange: Suppressed personal pattern
        var suppressedPattern = await CreatePatternInDbAsync(
            confirmCount: 0,
            rejectCount: 5,
            vendorName: "SUPPRESSED_VENDOR",
            occurrenceCount: 5);
        suppressedPattern.IsSuppressed = true;
        await _dbContext.SaveChangesAsync();

        var transaction = await CreateTransactionInDbAsync("SUPPRESSED_VENDOR");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(transaction.Id))
            .ReturnsAsync(false);

        // Act
        var predictionsGenerated = await _service.GeneratePredictionsAsync(_testUserId, new[] { transaction.Id });

        // Assert
        predictionsGenerated.Should().Be(0, "suppressed patterns should not generate predictions even if personal");
    }

    [Fact]
    public async Task PersonalPattern_WithRequiresReceiptMatch_StillNeedsReceipt()
    {
        // Arrange: Personal pattern that requires receipt match
        var pattern = await CreatePatternInDbAsync(
            confirmCount: 0,
            rejectCount: 5,
            vendorName: "RECEIPT_REQUIRED_VENDOR",
            occurrenceCount: 5);
        pattern.RequiresReceiptMatch = true;
        await _dbContext.SaveChangesAsync();

        // Transaction without receipt match
        var transaction = await CreateTransactionInDbAsync("RECEIPT_REQUIRED_VENDOR");

        _predictionRepositoryMock
            .Setup(x => x.ExistsForTransactionAsync(transaction.Id))
            .ReturnsAsync(false);

        // Act
        var predictionsGenerated = await _service.GeneratePredictionsAsync(_testUserId, new[] { transaction.Id });

        // Assert
        predictionsGenerated.Should().Be(0, "RequiresReceiptMatch should still be honored for personal patterns");
    }

    [Fact]
    public void ManualPrediction_IsPersonalPrediction_DefaultsFalse()
    {
        // Arrange: Manual override prediction (no pattern)
        var prediction = new TransactionPrediction
        {
            Id = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            UserId = _testUserId,
            PatternId = null,
            IsManualOverride = true,
            ConfidenceScore = 1.0m,
            ConfidenceLevel = PredictionConfidence.High,
            Status = PredictionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // Assert: Manual overrides should default to non-personal
        prediction.IsPersonalPrediction.Should().BeFalse("manual overrides should default to IsPersonalPrediction = false");
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
            OccurrenceCount = Math.Max(2, confirmCount + rejectCount + 2),
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

    private async Task<TransactionPrediction> CreatePredictionInDbAsync(
        Guid transactionId,
        Guid patternId,
        bool isPersonalPrediction = false)
    {
        var prediction = new TransactionPrediction
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            UserId = _testUserId,
            PatternId = patternId,
            ConfidenceScore = 0.85m,
            ConfidenceLevel = PredictionConfidence.High,
            Status = PredictionStatus.Pending,
            IsPersonalPrediction = isPersonalPrediction,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Load pattern navigation
        prediction.Pattern = await _dbContext.ExpensePatterns.FindAsync(patternId);

        return prediction;
    }

    #endregion
}
