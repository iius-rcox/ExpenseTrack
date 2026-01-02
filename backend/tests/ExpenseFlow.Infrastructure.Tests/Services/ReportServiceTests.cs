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
/// Unit tests for ReportService.
/// Tests draft generation, CRUD operations, and learning loop integration.
/// </summary>
public class ReportServiceTests
{
    private readonly Mock<IExpenseReportRepository> _reportRepositoryMock;
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<ITransactionRepository> _transactionRepositoryMock;
    private readonly Mock<ICategorizationService> _categorizationServiceMock;
    private readonly Mock<IDescriptionNormalizationService> _normalizationServiceMock;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<IDescriptionCacheService> _descriptionCacheServiceMock;
    private readonly Mock<ILogger<ReportService>> _loggerMock;
    private readonly ReportService _service;

    private readonly Guid _testUserId = Guid.NewGuid();
    private const string TestPeriod = "2024-06";

    public ReportServiceTests()
    {
        _reportRepositoryMock = new Mock<IExpenseReportRepository>();
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _categorizationServiceMock = new Mock<ICategorizationService>();
        _normalizationServiceMock = new Mock<IDescriptionNormalizationService>();
        _vendorAliasServiceMock = new Mock<IVendorAliasService>();
        _descriptionCacheServiceMock = new Mock<IDescriptionCacheService>();
        _loggerMock = new Mock<ILogger<ReportService>>();

        _service = new ReportService(
            _reportRepositoryMock.Object,
            _matchRepositoryMock.Object,
            _transactionRepositoryMock.Object,
            _categorizationServiceMock.Object,
            _normalizationServiceMock.Object,
            _vendorAliasServiceMock.Object,
            _descriptionCacheServiceMock.Object,
            _loggerMock.Object);
    }

    #region GenerateDraftAsync Tests

    [Fact]
    public async Task GenerateDraftAsync_CreatesReport_WithCorrectLineCount()
    {
        // Arrange
        var matches = CreateTestMatches(3);
        var unmatchedTransactions = CreateTestTransactions(2);

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        result.Should().NotBeNull();
        result.LineCount.Should().Be(5); // 3 matches + 2 unmatched
        result.Lines.Should().HaveCount(5);
        result.Period.Should().Be(TestPeriod);
        result.Status.Should().Be(ReportStatus.Draft);
    }

    [Fact]
    public async Task GenerateDraftAsync_IncludesMatchedAndUnmatchedTransactions()
    {
        // Arrange
        var matches = CreateTestMatches(2);
        var unmatchedTransactions = CreateTestTransactions(3);

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        result.Lines.Count(l => l.HasReceipt).Should().Be(2); // From matches
        result.Lines.Count(l => !l.HasReceipt).Should().Be(3); // From unmatched
        result.MissingReceiptCount.Should().Be(3);
    }

    [Fact]
    public async Task GenerateDraftAsync_PrePopulatesGLCodes_FromCategorizationService()
    {
        // Arrange
        var matches = CreateTestMatches(1);
        var unmatchedTransactions = new List<Transaction>();

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        var transactionId = matches[0].TransactionId;
        var glSuggestion = new CategorizationSuggestionDto
        {
            Code = "6200",
            Name = "Office Supplies",
            Confidence = 0.95m,
            Tier = 1,
            Source = "vendor_alias"
        };

        _categorizationServiceMock
            .Setup(x => x.GetCategorizationAsync(transactionId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionCategorizationDto
            {
                TransactionId = transactionId,
                GL = new GLCategorizationSection { TopSuggestion = glSuggestion }
            });

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        var line = result.Lines.First();
        line.GlCode.Should().Be("6200");
        line.GlCodeSuggested.Should().Be("6200");
        line.GlCodeTier.Should().Be(1);
        line.GlCodeSource.Should().Be("vendor_alias");
    }

    [Fact]
    public async Task GenerateDraftAsync_TracksTierHitCounts_Correctly()
    {
        // Arrange
        var matches = CreateTestMatches(4);
        var unmatchedTransactions = new List<Transaction>();

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        // Setup different tiers for different transactions
        SetupCategorizationWithTier(matches[0].TransactionId, 1);
        SetupCategorizationWithTier(matches[1].TransactionId, 1);
        SetupCategorizationWithTier(matches[2].TransactionId, 2);
        SetupCategorizationWithTier(matches[3].TransactionId, 3);

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        result.Tier1HitCount.Should().Be(2);
        result.Tier2HitCount.Should().Be(1);
        result.Tier3HitCount.Should().Be(1);
    }

    [Fact]
    public async Task GenerateDraftAsync_ReplacesExistingDraft_WhenExists()
    {
        // Arrange
        var existingDraft = new ExpenseReport
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Period = TestPeriod,
            Status = ReportStatus.Draft
        };

        // Setup existing draft to be found
        _reportRepositoryMock
            .Setup(x => x.GetDraftByUserAndPeriodAsync(_testUserId, TestPeriod, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDraft);

        // Setup remaining mocks (without overwriting draft mock)
        _matchRepositoryMock
            .Setup(x => x.GetConfirmedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<ReceiptTransactionMatch>());

        _transactionRepositoryMock
            .Setup(x => x.GetUnmatchedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(new List<Transaction>());

        _reportRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken ct) =>
            {
                report.Id = Guid.NewGuid();
                return report;
            });

        // Act
        await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        _reportRepositoryMock.Verify(
            x => x.SoftDeleteAsync(existingDraft.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateDraftAsync_CalculatesTotalAmount_Correctly()
    {
        // Arrange
        var matches = new List<ReceiptTransactionMatch>
        {
            CreateTestMatch(100.00m),
            CreateTestMatch(50.50m)
        };
        var unmatchedTransactions = new List<Transaction>
        {
            CreateTestTransaction(25.00m),
            CreateTestTransaction(75.25m)
        };

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        result.TotalAmount.Should().Be(250.75m); // 100 + 50.50 + 25 + 75.25
    }

    [Fact]
    public async Task GenerateDraftAsync_SetsLineOrder_Sequentially()
    {
        // Arrange
        var matches = CreateTestMatches(2);
        var unmatchedTransactions = CreateTestTransactions(2);

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        var lineOrders = result.Lines.Select(l => l.LineOrder).ToList();
        lineOrders.Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
    }

    [Fact]
    public async Task GenerateDraftAsync_NormalizesDescriptions_ViaService()
    {
        // Arrange
        var matches = CreateTestMatches(1);
        var unmatchedTransactions = new List<Transaction>();
        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        var rawDesc = matches[0].Transaction.OriginalDescription;
        _normalizationServiceMock
            .Setup(x => x.NormalizeAsync(rawDesc, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NormalizationResultDto
            {
                RawDescription = rawDesc,
                NormalizedDescription = "Normalized Description",
                Tier = 1,
                CacheHit = true,
                Confidence = 1.0m
            });

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        result.Lines.First().NormalizedDescription.Should().Be("Normalized Description");
    }

    [Fact]
    public async Task GenerateDraftAsync_SetsVendorName_FromReceiptOrAlias()
    {
        // Arrange
        var match = CreateTestMatch(100m);
        match.Receipt.VendorExtracted = "Amazon.com";

        SetupMocksForDraftGeneration(new List<ReceiptTransactionMatch> { match }, new List<Transaction>());

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        result.Lines.First().VendorName.Should().Be("Amazon.com");
    }

    [Fact]
    public async Task GenerateDraftAsync_SetsMissingReceiptJustification_ForUnmatched()
    {
        // Arrange
        var matches = new List<ReceiptTransactionMatch>();
        var unmatchedTransactions = CreateTestTransactions(1);

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        var line = result.Lines.First();
        line.HasReceipt.Should().BeFalse();
        line.MissingReceiptJustification.Should().Be(MissingReceiptJustification.NotProvided);
    }

    [Fact]
    public async Task GenerateDraftAsync_HandlesCategorizationFailure_Gracefully()
    {
        // Arrange
        var matches = CreateTestMatches(1);
        var unmatchedTransactions = new List<Transaction>();

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        _categorizationServiceMock
            .Setup(x => x.GetCategorizationAsync(It.IsAny<Guid>(), _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Categorization service unavailable"));

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert - Should complete without throwing
        result.Should().NotBeNull();
        result.Lines.Should().HaveCount(1);
        // GL code should be null when categorization fails
        result.Lines.First().GlCode.Should().BeNull();
    }

    [Fact]
    public async Task GenerateDraftAsync_HandlesNormalizationFailure_Gracefully()
    {
        // Arrange
        var matches = CreateTestMatches(1);
        var unmatchedTransactions = new List<Transaction>();

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        var rawDesc = matches[0].Transaction.OriginalDescription;
        _normalizationServiceMock
            .Setup(x => x.NormalizeAsync(rawDesc, _testUserId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Normalization service unavailable"));

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert - Should use original description as fallback
        result.Lines.First().NormalizedDescription.Should().Be(rawDesc);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ReturnsReport_WhenOwnedByUser()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.GetByIdAsync(_testUserId, reportId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(reportId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotOwnedByUser()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var report = CreateTestReport(reportId, differentUserId);

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.GetByIdAsync(_testUserId, reportId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenReportNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport?)null);

        // Act
        var result = await _service.GetByIdAsync(_testUserId, reportId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetListAsync Tests

    [Fact]
    public async Task GetListAsync_ReturnsPaginatedList()
    {
        // Arrange
        var reports = new List<ExpenseReport>
        {
            CreateTestReport(Guid.NewGuid(), _testUserId),
            CreateTestReport(Guid.NewGuid(), _testUserId)
        };

        _reportRepositoryMock
            .Setup(x => x.GetByUserAsync(_testUserId, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reports);

        _reportRepositoryMock
            .Setup(x => x.GetCountByUserAsync(_testUserId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        var result = await _service.GetListAsync(_testUserId, null, null, 1, 20);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetListAsync_AppliesStatusFilter()
    {
        // Arrange
        _reportRepositoryMock
            .Setup(x => x.GetByUserAsync(_testUserId, ReportStatus.Draft, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpenseReport>());

        _reportRepositoryMock
            .Setup(x => x.GetCountByUserAsync(_testUserId, ReportStatus.Draft, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _service.GetListAsync(_testUserId, ReportStatus.Draft, null, 1, 20);

        // Assert
        _reportRepositoryMock.Verify(
            x => x.GetByUserAsync(_testUserId, ReportStatus.Draft, null, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetListAsync_AppliesPeriodFilter()
    {
        // Arrange
        _reportRepositoryMock
            .Setup(x => x.GetByUserAsync(_testUserId, null, "2024-06", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExpenseReport>());

        _reportRepositoryMock
            .Setup(x => x.GetCountByUserAsync(_testUserId, null, "2024-06", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _service.GetListAsync(_testUserId, null, "2024-06", 1, 20);

        // Assert
        _reportRepositoryMock.Verify(
            x => x.GetByUserAsync(_testUserId, null, "2024-06", 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region UpdateLineAsync Tests

    [Fact]
    public async Task UpdateLineAsync_MarksLineAsUserEdited_WhenGLCodeChanged()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, "5000", "5000");

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        var request = new UpdateLineRequest { GlCode = "6200" }; // Different from suggested

        // Act
        var result = await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        result.Should().NotBeNull();
        result!.IsUserEdited.Should().BeTrue();
        result.GlCode.Should().Be("6200");
    }

    [Fact]
    public async Task UpdateLineAsync_DoesNotMarkEdited_WhenGLCodeMatchesSuggestion()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, "5000", "5000");

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        var request = new UpdateLineRequest { GlCode = "5000" }; // Same as suggested

        // Act
        var result = await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        result.Should().NotBeNull();
        result!.IsUserEdited.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLineAsync_TriggersLearningLoop_WhenGLCodeChanged()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, "5000", "5000");
        line.VendorName = "TestVendor";

        var vendorAlias = new VendorAlias
        {
            Id = Guid.NewGuid(),
            CanonicalName = "TestVendor",
            DefaultGLCode = "5000"
        };

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        _vendorAliasServiceMock
            .Setup(x => x.GetByVendorNameAsync("TestVendor"))
            .ReturnsAsync(vendorAlias);

        var request = new UpdateLineRequest { GlCode = "6200" };

        // Act
        await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        _vendorAliasServiceMock.Verify(
            x => x.UpdateAsync(It.Is<VendorAlias>(a => a.DefaultGLCode == "6200")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateLineAsync_UpdatesMissingReceiptJustification()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, null, null);
        line.HasReceipt = false;

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        var request = new UpdateLineRequest
        {
            MissingReceiptJustification = MissingReceiptJustification.Lost,
            JustificationNote = "Receipt was lost"
        };

        // Act
        var result = await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        result.Should().NotBeNull();
        result!.MissingReceiptJustification.Should().Be(MissingReceiptJustification.Lost);
        result.JustificationNote.Should().Be("Receipt was lost");
    }

    [Fact]
    public async Task UpdateLineAsync_ReturnsNull_WhenReportNotOwned()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var report = CreateTestReport(reportId, differentUserId);

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var request = new UpdateLineRequest { GlCode = "6200" };

        // Act
        var result = await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLineAsync_ReturnsNull_WhenLineNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseLine?)null);

        var request = new UpdateLineRequest { GlCode = "6200" };

        // Act
        var result = await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLineAsync_UpdatesDepartment_WhenChanged()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, null, null);
        line.DepartmentSuggested = "DEPT-A";
        line.DepartmentCode = "DEPT-A";

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        var request = new UpdateLineRequest { DepartmentCode = "DEPT-B" };

        // Act
        var result = await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        result.Should().NotBeNull();
        result!.DepartmentCode.Should().Be("DEPT-B");
        result.IsUserEdited.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLineAsync_SetsUpdatedAt_Timestamp()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, null, null);

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        var request = new UpdateLineRequest { GlCode = "6200" };
        var beforeUpdate = DateTime.UtcNow;

        // Act
        var result = await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        result.Should().NotBeNull();
        result!.UpdatedAt.Should().NotBeNull();
        result.UpdatedAt.Should().BeOnOrAfter(beforeUpdate);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_SoftDeletesReport_WhenOwnedByUser()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.DeleteAsync(_testUserId, reportId);

        // Assert
        result.Should().BeTrue();
        _reportRepositoryMock.Verify(
            x => x.SoftDeleteAsync(reportId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotOwnedByUser()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var report = CreateTestReport(reportId, differentUserId);

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.DeleteAsync(_testUserId, reportId);

        // Assert
        result.Should().BeFalse();
        _reportRepositoryMock.Verify(
            x => x.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenReportNotFound()
    {
        // Arrange
        var reportId = Guid.NewGuid();

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport?)null);

        // Act
        var result = await _service.DeleteAsync(_testUserId, reportId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region GetExistingDraftIdAsync Tests

    [Fact]
    public async Task GetExistingDraftIdAsync_ReturnsDraftId_WhenExists()
    {
        // Arrange
        var draftId = Guid.NewGuid();
        var draft = new ExpenseReport { Id = draftId, UserId = _testUserId, Period = TestPeriod };

        _reportRepositoryMock
            .Setup(x => x.GetDraftByUserAndPeriodAsync(_testUserId, TestPeriod, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);

        // Act
        var result = await _service.GetExistingDraftIdAsync(_testUserId, TestPeriod);

        // Assert
        result.Should().Be(draftId);
    }

    [Fact]
    public async Task GetExistingDraftIdAsync_ReturnsNull_WhenNoDraft()
    {
        // Arrange
        _reportRepositoryMock
            .Setup(x => x.GetDraftByUserAndPeriodAsync(_testUserId, TestPeriod, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport?)null);

        // Act
        var result = await _service.GetExistingDraftIdAsync(_testUserId, TestPeriod);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Learning Loop Tests

    [Fact]
    public async Task UpdateLineAsync_AddsToDescriptionCache_WhenDescriptionsDiffer()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, "5000", "5000");
        line.OriginalDescription = "AMZN*KB4JW1FH3 SEATTLE WA";
        line.NormalizedDescription = "Amazon Purchase";

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        var request = new UpdateLineRequest { GlCode = "6200" }; // Different GL code triggers learning

        // Act
        await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        _descriptionCacheServiceMock.Verify(
            x => x.AddOrUpdateAsync("AMZN*KB4JW1FH3 SEATTLE WA", "Amazon Purchase"),
            Times.Once);
    }

    [Fact]
    public async Task UpdateLineAsync_UpdatesVendorAliasDepartment_WhenDepartmentChanged()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, null, null);
        line.VendorName = "Starbucks";
        line.DepartmentSuggested = "DEPT-A";
        line.DepartmentCode = "DEPT-A";

        var vendorAlias = new VendorAlias
        {
            Id = Guid.NewGuid(),
            CanonicalName = "Starbucks",
            DefaultDepartment = "DEPT-A"
        };

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        _vendorAliasServiceMock
            .Setup(x => x.GetByVendorNameAsync("Starbucks"))
            .ReturnsAsync(vendorAlias);

        var request = new UpdateLineRequest { DepartmentCode = "DEPT-B" };

        // Act
        await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert
        _vendorAliasServiceMock.Verify(
            x => x.UpdateAsync(It.Is<VendorAlias>(a => a.DefaultDepartment == "DEPT-B")),
            Times.Once);
    }

    [Fact]
    public async Task UpdateLineAsync_ContinuesOnLearningLoopFailure()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateTestLine(lineId, reportId, "5000", "5000");
        line.VendorName = "TestVendor";

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.GetLineByIdAsync(reportId, lineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(line);

        _vendorAliasServiceMock
            .Setup(x => x.GetByVendorNameAsync("TestVendor"))
            .ThrowsAsync(new Exception("Database error"));

        var request = new UpdateLineRequest { GlCode = "6200" };

        // Act
        var result = await _service.UpdateLineAsync(_testUserId, reportId, lineId, request);

        // Assert - Should still succeed despite learning loop failure
        result.Should().NotBeNull();
        result!.GlCode.Should().Be("6200");
        result.IsUserEdited.Should().BeTrue();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task GenerateDraftAsync_HandlesEmptyPeriod_WithNoData()
    {
        // Arrange
        SetupMocksForDraftGeneration(new List<ReceiptTransactionMatch>(), new List<Transaction>());

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        result.Should().NotBeNull();
        result.LineCount.Should().Be(0);
        result.Lines.Should().BeEmpty();
        result.TotalAmount.Should().Be(0);
    }

    [Fact]
    public async Task GenerateDraftAsync_PrePopulatesDepartment_FromCategorizationService()
    {
        // Arrange
        var matches = CreateTestMatches(1);
        var unmatchedTransactions = new List<Transaction>();

        SetupMocksForDraftGeneration(matches, unmatchedTransactions);

        var transactionId = matches[0].TransactionId;
        var deptSuggestion = new CategorizationSuggestionDto
        {
            Code = "SALES",
            Name = "Sales Department",
            Confidence = 0.90m,
            Tier = 2,
            Source = "embedding_similarity"
        };

        _categorizationServiceMock
            .Setup(x => x.GetCategorizationAsync(transactionId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionCategorizationDto
            {
                TransactionId = transactionId,
                Department = new DepartmentCategorizationSection { TopSuggestion = deptSuggestion }
            });

        // Act
        var result = await _service.GenerateDraftAsync(_testUserId, TestPeriod);

        // Assert
        var line = result.Lines.First();
        line.DepartmentCode.Should().Be("SALES");
        line.DepartmentSuggested.Should().Be("SALES");
        line.DepartmentTier.Should().Be(2);
        line.DepartmentSource.Should().Be("embedding_similarity");
    }

    #endregion

    #region ValidateReportAsync Tests

    [Fact]
    public async Task ValidateReportAsync_ReportNotFound_ReturnsError()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport?)null);

        // Act
        var result = await _service.ValidateReportAsync(reportId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "REPORT_NOT_FOUND");
    }

    [Fact]
    public async Task ValidateReportAsync_ReportAlreadyGenerated_ReturnsError()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        report.Status = ReportStatus.Generated;
        report.Lines = new List<ExpenseLine> { CreateValidExpenseLine(reportId) };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ValidateReportAsync(reportId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "REPORT_ALREADY_GENERATED");
    }

    [Fact]
    public async Task ValidateReportAsync_ReportHasNoLines_ReturnsError()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        report.Lines = new List<ExpenseLine>();

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ValidateReportAsync(reportId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "REPORT_EMPTY");
    }

    [Fact]
    public async Task ValidateReportAsync_LineMissingGLCode_ReturnsError()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateValidExpenseLine(reportId);
        line.GLCode = null;
        report.Lines = new List<ExpenseLine> { line };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ValidateReportAsync(reportId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LINE_NO_CATEGORY");
    }

    [Fact]
    public async Task ValidateReportAsync_LineHasZeroAmount_ReturnsError()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateValidExpenseLine(reportId);
        line.Amount = 0;
        report.Lines = new List<ExpenseLine> { line };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ValidateReportAsync(reportId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LINE_INVALID_AMOUNT");
    }

    [Fact]
    public async Task ValidateReportAsync_LineMissingReceipt_ReturnsError()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateValidExpenseLine(reportId);
        line.HasReceipt = false;
        line.ReceiptId = null;
        report.Lines = new List<ExpenseLine> { line };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ValidateReportAsync(reportId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "LINE_NO_RECEIPT");
    }

    [Fact]
    public async Task ValidateReportAsync_ValidReport_ReturnsSuccess()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        report.Lines = new List<ExpenseLine>
        {
            CreateValidExpenseLine(reportId),
            CreateValidExpenseLine(reportId)
        };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ValidateReportAsync(reportId);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateReportAsync_MultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);

        var line1 = CreateValidExpenseLine(reportId);
        line1.GLCode = null; // Missing GL code

        var line2 = CreateValidExpenseLine(reportId);
        line2.Amount = 0; // Invalid amount

        report.Lines = new List<ExpenseLine> { line1, line2 };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act
        var result = await _service.ValidateReportAsync(reportId);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.Code == "LINE_NO_CATEGORY");
        result.Errors.Should().Contain(e => e.Code == "LINE_INVALID_AMOUNT");
    }

    #endregion

    #region GenerateAsync Tests

    [Fact]
    public async Task GenerateAsync_ValidDraftReport_TransitionsToGenerated()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        report.Lines = new List<ExpenseLine> { CreateValidExpenseLine(reportId) };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GenerateAsync(_testUserId, reportId);

        // Assert
        result.Status.Should().Be("Generated");
        result.ReportId.Should().Be(reportId);
        result.GeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        _reportRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<ExpenseReport>(r =>
                r.Status == ReportStatus.Generated &&
                r.GeneratedAt != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GenerateAsync_ReportNotFound_ThrowsException()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport?)null);

        // Act & Assert
        var action = () => _service.GenerateAsync(_testUserId, reportId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GenerateAsync_DifferentUser_ThrowsException()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var report = CreateTestReport(reportId, differentUserId);
        report.Lines = new List<ExpenseLine> { CreateValidExpenseLine(reportId) };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act & Assert
        var action = () => _service.GenerateAsync(_testUserId, reportId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task GenerateAsync_ValidationFails_ThrowsException()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        var line = CreateValidExpenseLine(reportId);
        line.GLCode = null; // Invalid - missing GL code
        report.Lines = new List<ExpenseLine> { line };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act & Assert
        var action = () => _service.GenerateAsync(_testUserId, reportId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*validation failed*");
    }

    [Fact]
    public async Task GenerateAsync_ReturnsCorrectTotals()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        report.TotalAmount = 250.00m;
        report.LineCount = 3;
        report.Lines = new List<ExpenseLine>
        {
            CreateValidExpenseLine(reportId),
            CreateValidExpenseLine(reportId),
            CreateValidExpenseLine(reportId)
        };

        _reportRepositoryMock
            .Setup(x => x.GetByIdWithLinesAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GenerateAsync(_testUserId, reportId);

        // Assert
        result.TotalAmount.Should().Be(250.00m);
        result.LineCount.Should().Be(3);
    }

    #endregion

    #region SubmitAsync Tests

    [Fact]
    public async Task SubmitAsync_GeneratedReport_TransitionsToSubmitted()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        report.Status = ReportStatus.Generated;
        report.GeneratedAt = DateTimeOffset.UtcNow.AddHours(-1);

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitAsync(_testUserId, reportId);

        // Assert
        result.Status.Should().Be("Submitted");
        result.ReportId.Should().Be(reportId);
        result.SubmittedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));

        _reportRepositoryMock.Verify(
            x => x.UpdateAsync(It.Is<ExpenseReport>(r =>
                r.Status == ReportStatus.Submitted &&
                r.SubmittedAt != null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubmitAsync_ReportNotFound_ThrowsException()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport?)null);

        // Act & Assert
        var action = () => _service.SubmitAsync(_testUserId, reportId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task SubmitAsync_DifferentUser_ThrowsException()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var report = CreateTestReport(reportId, differentUserId);
        report.Status = ReportStatus.Generated;

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act & Assert
        var action = () => _service.SubmitAsync(_testUserId, reportId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public async Task SubmitAsync_AlreadySubmitted_ThrowsException()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        report.Status = ReportStatus.Submitted;
        report.SubmittedAt = DateTimeOffset.UtcNow.AddHours(-1);

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act & Assert
        var action = () => _service.SubmitAsync(_testUserId, reportId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already been submitted*");
    }

    [Fact]
    public async Task SubmitAsync_DraftReport_ThrowsException()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var report = CreateTestReport(reportId, _testUserId);
        report.Status = ReportStatus.Draft;

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        // Act & Assert
        var action = () => _service.SubmitAsync(_testUserId, reportId);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must be in Generated status*");
    }

    [Fact]
    public async Task SubmitAsync_PreservesGeneratedTimestamp()
    {
        // Arrange
        var reportId = Guid.NewGuid();
        var generatedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var report = CreateTestReport(reportId, _testUserId);
        report.Status = ReportStatus.Generated;
        report.GeneratedAt = generatedAt;

        _reportRepositoryMock
            .Setup(x => x.GetByIdAsync(reportId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        _reportRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.SubmitAsync(_testUserId, reportId);

        // Assert
        result.GeneratedAt.Should().Be(generatedAt);
    }

    #endregion

    #region Helper Methods

    private ExpenseLine CreateValidExpenseLine(Guid reportId)
    {
        return new ExpenseLine
        {
            Id = Guid.NewGuid(),
            ReportId = reportId,
            LineOrder = 1,
            ExpenseDate = new DateOnly(2024, 6, 15),
            Amount = 100.00m,
            OriginalDescription = "Test Transaction",
            NormalizedDescription = "Test Transaction",
            GLCode = "6200",
            GLCodeSuggested = "6200",
            HasReceipt = true,
            ReceiptId = Guid.NewGuid(),
            IsUserEdited = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    private void SetupMocksForDraftGeneration(
        List<ReceiptTransactionMatch> matches,
        List<Transaction> unmatchedTransactions)
    {
        _reportRepositoryMock
            .Setup(x => x.GetDraftByUserAndPeriodAsync(_testUserId, TestPeriod, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport?)null);

        _matchRepositoryMock
            .Setup(x => x.GetConfirmedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(matches);

        _transactionRepositoryMock
            .Setup(x => x.GetUnmatchedByPeriodAsync(_testUserId, It.IsAny<DateOnly>(), It.IsAny<DateOnly>()))
            .ReturnsAsync(unmatchedTransactions);

        _categorizationServiceMock
            .Setup(x => x.GetCategorizationAsync(It.IsAny<Guid>(), _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionCategorizationDto?)null);

        _normalizationServiceMock
            .Setup(x => x.NormalizeAsync(It.IsAny<string>(), _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string desc, Guid userId, CancellationToken ct) => new NormalizationResultDto
            {
                RawDescription = desc,
                NormalizedDescription = desc,
                Tier = 1,
                CacheHit = true,
                Confidence = 1.0m
            });

        _reportRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ExpenseReport>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseReport report, CancellationToken ct) =>
            {
                report.Id = Guid.NewGuid();
                return report;
            });
    }

    private void SetupCategorizationWithTier(Guid transactionId, int tier)
    {
        var suggestion = new CategorizationSuggestionDto
        {
            Code = $"GL-{tier}00",
            Tier = tier,
            Source = tier switch
            {
                1 => "vendor_alias",
                2 => "embedding_similarity",
                3 => "ai_inference",
                _ => "unknown"
            },
            Confidence = tier switch
            {
                1 => 1.0m,
                2 => 0.85m,
                3 => 0.70m,
                _ => 0.5m
            }
        };

        _categorizationServiceMock
            .Setup(x => x.GetCategorizationAsync(transactionId, _testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionCategorizationDto
            {
                TransactionId = transactionId,
                GL = new GLCategorizationSection { TopSuggestion = suggestion }
            });
    }

    private List<ReceiptTransactionMatch> CreateTestMatches(int count)
    {
        var matches = new List<ReceiptTransactionMatch>();
        for (var i = 0; i < count; i++)
        {
            matches.Add(CreateTestMatch(100.00m + i * 10));
        }
        return matches;
    }

    private ReceiptTransactionMatch CreateTestMatch(decimal amount)
    {
        var transactionId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();

        return new ReceiptTransactionMatch
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TransactionId = transactionId,
            ReceiptId = receiptId,
            Status = MatchProposalStatus.Confirmed,
            ConfidenceScore = 95m,
            Transaction = new Transaction
            {
                Id = transactionId,
                UserId = _testUserId,
                TransactionDate = new DateOnly(2024, 6, 15),
                Amount = amount,
                OriginalDescription = $"Test Transaction {Guid.NewGuid():N}",
                Description = "Test Transaction"
            },
            Receipt = new Receipt
            {
                Id = receiptId,
                UserId = _testUserId,
                DateExtracted = new DateOnly(2024, 6, 15),
                AmountExtracted = amount,
                BlobUrl = "https://storage/test.jpg",
                OriginalFilename = "receipt.jpg",
                ContentType = "image/jpeg",
                VendorExtracted = "Test Vendor"
            }
        };
    }

    private List<Transaction> CreateTestTransactions(int count)
    {
        var transactions = new List<Transaction>();
        for (var i = 0; i < count; i++)
        {
            transactions.Add(CreateTestTransaction(50.00m + i * 5));
        }
        return transactions;
    }

    private Transaction CreateTestTransaction(decimal amount)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            TransactionDate = new DateOnly(2024, 6, 15),
            Amount = amount,
            OriginalDescription = $"Unmatched Transaction {Guid.NewGuid():N}",
            Description = "Unmatched Transaction",
            MatchedReceiptId = null,
            MatchStatus = MatchStatus.Unmatched
        };
    }

    private ExpenseReport CreateTestReport(Guid reportId, Guid userId)
    {
        return new ExpenseReport
        {
            Id = reportId,
            UserId = userId,
            Period = TestPeriod,
            Status = ReportStatus.Draft,
            TotalAmount = 500.00m,
            LineCount = 5,
            MissingReceiptCount = 2,
            Tier1HitCount = 2,
            Tier2HitCount = 2,
            Tier3HitCount = 1,
            CreatedAt = DateTime.UtcNow,
            Lines = new List<ExpenseLine>()
        };
    }

    private ExpenseLine CreateTestLine(Guid lineId, Guid reportId, string? glCode, string? glCodeSuggested)
    {
        return new ExpenseLine
        {
            Id = lineId,
            ReportId = reportId,
            LineOrder = 1,
            ExpenseDate = new DateOnly(2024, 6, 15),
            Amount = 100.00m,
            OriginalDescription = "Test Transaction",
            NormalizedDescription = "Test Transaction",
            GLCode = glCode,
            GLCodeSuggested = glCodeSuggested,
            HasReceipt = true,
            IsUserEdited = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
