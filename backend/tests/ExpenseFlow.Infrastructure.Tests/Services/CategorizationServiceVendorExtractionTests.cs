using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.DTOs;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Moq;
using Pgvector;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for vendor extraction functionality in CategorizationService.
/// Tests the integration with VendorAliasService for extracting clean vendor names.
/// </summary>
public class CategorizationServiceVendorExtractionTests
{
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<IEmbeddingService> _embeddingServiceMock;
    private readonly Mock<IDescriptionNormalizationService> _normalizationServiceMock;
    private readonly Mock<ITierUsageService> _tierUsageServiceMock;
    private readonly Mock<IReferenceDataService> _referenceDataServiceMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<CategorizationService>> _loggerMock;
    private readonly CategorizationService _sut;

    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testTransactionId = Guid.NewGuid();

    public CategorizationServiceVendorExtractionTests()
    {
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _vendorAliasServiceMock = new Mock<IVendorAliasService>();
        _embeddingServiceMock = new Mock<IEmbeddingService>();
        _normalizationServiceMock = new Mock<IDescriptionNormalizationService>();
        _tierUsageServiceMock = new Mock<ITierUsageService>();
        _referenceDataServiceMock = new Mock<IReferenceDataService>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<CategorizationService>>();

        // Setup default configuration values
        _configurationMock.Setup(c => c.GetValue("Categorization:EmbeddingSimilarityThreshold", It.IsAny<float>()))
            .Returns(0.92f);
        _configurationMock.Setup(c => c.GetValue("Categorization:VendorAliasConfirmThreshold", It.IsAny<int>()))
            .Returns(3);

        // Setup default normalization service response
        _normalizationServiceMock.Setup(n => n.NormalizeAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string desc, Guid _, CancellationToken _) =>
                new NormalizationResultDto { NormalizedDescription = desc, RawDescription = desc });

        // Setup default reference data service responses (empty lists)
        _referenceDataServiceMock.Setup(r => r.GetGLAccountsAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<GLAccount>());
        _referenceDataServiceMock.Setup(r => r.GetDepartmentsAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<Department>());

        // Setup embedding service to return empty list (no similar embeddings)
        _embeddingServiceMock.Setup(e => e.GenerateEmbeddingAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Vector(new float[1536]));

        _embeddingServiceMock.Setup(e => e.FindSimilarAsync(
                It.IsAny<Vector>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<float>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpenseEmbedding>());

        // Setup vendor alias service - default to no match
        _vendorAliasServiceMock.Setup(v => v.GetByVendorNameAsync(It.IsAny<string>()))
            .ReturnsAsync((VendorAlias?)null);

        _sut = new CategorizationService(
            _transactionRepositoryMock.Object,
            _vendorAliasServiceMock.Object,
            _embeddingServiceMock.Object,
            _normalizationServiceMock.Object,
            _tierUsageServiceMock.Object,
            null, // No ChatCompletionService for unit tests
            _referenceDataServiceMock.Object,
            _configurationMock.Object,
            _loggerMock.Object);
    }

    #region Vendor Extraction Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCategorizationAsync_WhenVendorAliasMatches_ReturnsDisplayName()
    {
        // Arrange
        var transaction = CreateTestTransaction("AMZN MKTP US*2K7XY9Z03");
        var vendorAlias = CreateTestVendorAlias("Amazon", "AMZN");

        _transactionRepositoryMock.Setup(r => r.GetByIdAsync(_testUserId, _testTransactionId))
            .ReturnsAsync(transaction);

        _vendorAliasServiceMock.Setup(v => v.FindMatchingAliasAsync(transaction.Description))
            .ReturnsAsync(vendorAlias);

        // Act
        var result = await _sut.GetCategorizationAsync(_testTransactionId, _testUserId);

        // Assert
        result.Vendor.Should().Be("Amazon",
            "because the VendorAlias DisplayName should be returned when a match is found");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCategorizationAsync_WhenNoVendorAliasMatch_ReturnsOriginalDescription()
    {
        // Arrange
        var description = "JONES HARDWARE SUPPLIES";
        var transaction = CreateTestTransaction(description);

        _transactionRepositoryMock.Setup(r => r.GetByIdAsync(_testUserId, _testTransactionId))
            .ReturnsAsync(transaction);

        _vendorAliasServiceMock.Setup(v => v.FindMatchingAliasAsync(description))
            .ReturnsAsync((VendorAlias?)null);

        // Act
        var result = await _sut.GetCategorizationAsync(_testTransactionId, _testUserId);

        // Assert
        result.Vendor.Should().Be(description,
            "because the original description should be returned when no VendorAlias matches");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCategorizationAsync_WhenVendorAliasMatches_CallsRecordMatchAsync()
    {
        // Arrange
        var transaction = CreateTestTransaction("UBER *TRIP HELP.UBER.COM");
        var vendorAlias = CreateTestVendorAlias("Uber", "UBER");
        var aliasId = vendorAlias.Id;

        _transactionRepositoryMock.Setup(r => r.GetByIdAsync(_testUserId, _testTransactionId))
            .ReturnsAsync(transaction);

        _vendorAliasServiceMock.Setup(v => v.FindMatchingAliasAsync(transaction.Description))
            .ReturnsAsync(vendorAlias);

        // Act
        await _sut.GetCategorizationAsync(_testTransactionId, _testUserId);

        // Assert
        _vendorAliasServiceMock.Verify(
            v => v.RecordMatchAsync(aliasId),
            Times.Once,
            "because RecordMatchAsync should be called to update match statistics");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCategorizationAsync_WhenDescriptionEmpty_ReturnsEmptyVendor()
    {
        // Arrange
        var transaction = CreateTestTransaction(string.Empty);

        _transactionRepositoryMock.Setup(r => r.GetByIdAsync(_testUserId, _testTransactionId))
            .ReturnsAsync(transaction);

        _vendorAliasServiceMock.Setup(v => v.FindMatchingAliasAsync(string.Empty))
            .ReturnsAsync((VendorAlias?)null);

        // Act
        var result = await _sut.GetCategorizationAsync(_testTransactionId, _testUserId);

        // Assert
        result.Vendor.Should().BeEmpty(
            "because an empty description should result in an empty vendor name");
    }

    #endregion

    #region Vendor Default Suggestions Tests (US2)

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGLSuggestionsAsync_WhenVendorHasDefaultGL_IncludesInSuggestions()
    {
        // Arrange
        var transaction = CreateTestTransaction("AMZN MKTP US*2K7XY9Z03");
        var vendorAlias = CreateTestVendorAlias("Amazon", "AMZN");
        vendorAlias.DefaultGLCode = "6100";

        _transactionRepositoryMock.Setup(r => r.GetByIdAsync(_testUserId, _testTransactionId))
            .ReturnsAsync(transaction);

        _vendorAliasServiceMock.Setup(v => v.GetByVendorNameAsync(transaction.Description))
            .ReturnsAsync(vendorAlias);

        _referenceDataServiceMock.Setup(r => r.GetGLAccountByCodeAsync("6100"))
            .ReturnsAsync(new GLAccount { Code = "6100", Name = "Office Supplies" });

        // Act
        var result = await _sut.GetGLSuggestionsAsync(_testTransactionId, _testUserId);

        // Assert
        result.TopSuggestion.Should().NotBeNull(
            "because a suggestion should be returned when vendor has a default GL code");
        result.TopSuggestion!.Code.Should().Be("6100",
            "because the vendor's default GL code should be the top suggestion");
        result.TopSuggestion.Source.Should().Be("vendor_alias",
            "because the suggestion came from the vendor alias defaults");
    }

    #endregion

    #region Helper Methods

    private Transaction CreateTestTransaction(string description)
    {
        return new Transaction
        {
            Id = _testTransactionId,
            UserId = _testUserId,
            Description = description,
            OriginalDescription = description,
            Amount = 99.99m,
            PostedDate = DateTime.UtcNow,
            StatementId = Guid.NewGuid()
        };
    }

    private VendorAlias CreateTestVendorAlias(string displayName, string aliasPattern)
    {
        return new VendorAlias
        {
            Id = Guid.NewGuid(),
            CanonicalName = displayName.ToLowerInvariant(),
            DisplayName = displayName,
            AliasPattern = aliasPattern,
            Confidence = 1.0m,
            MatchCount = 5
        };
    }

    #endregion
}
