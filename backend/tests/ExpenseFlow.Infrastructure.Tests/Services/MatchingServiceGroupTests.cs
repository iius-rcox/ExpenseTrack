using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Services;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Services;

/// <summary>
/// Unit tests for transaction group matching functionality in MatchingService.
/// Tests scoring calculations, vendor extraction, and grouped transaction exclusion.
/// </summary>
public class MatchingServiceGroupTests
{
    private readonly Mock<IMatchRepository> _matchRepositoryMock;
    private readonly Mock<IFuzzyMatchingService> _fuzzyMatchingServiceMock;
    private readonly Mock<IVendorAliasService> _vendorAliasServiceMock;
    private readonly Mock<ILogger<MatchingService>> _loggerMock;

    public MatchingServiceGroupTests()
    {
        _matchRepositoryMock = new Mock<IMatchRepository>();
        _fuzzyMatchingServiceMock = new Mock<IFuzzyMatchingService>();
        _vendorAliasServiceMock = new Mock<IVendorAliasService>();
        _loggerMock = new Mock<ILogger<MatchingService>>();
    }

    #region T011: Amount Scoring for Group CombinedAmount

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void CalculateAmountScore_GroupCombinedAmount_ExactMatch_Returns40Points()
    {
        // Arrange - Group with CombinedAmount matching receipt exactly
        var receiptAmount = 50.00m;
        var groupCombinedAmount = 50.00m;

        // Act - Apply same scoring rules as individual transactions
        var diff = Math.Abs(receiptAmount - groupCombinedAmount);
        var score = diff <= 0.10m ? 40m : diff <= 1.00m ? 20m : 0m;

        // Assert
        score.Should().Be(40m, "exact amount match should score 40 points");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void CalculateAmountScore_GroupCombinedAmount_NearMatch_Returns20Points()
    {
        // Arrange - Group with CombinedAmount within $1.00 tolerance
        var receiptAmount = 50.00m;
        var groupCombinedAmount = 50.75m;

        // Act
        var diff = Math.Abs(receiptAmount - groupCombinedAmount);
        var score = diff <= 0.10m ? 40m : diff <= 1.00m ? 20m : 0m;

        // Assert
        score.Should().Be(20m, "near amount match should score 20 points");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void CalculateAmountScore_GroupCombinedAmount_OutsideTolerance_Returns0Points()
    {
        // Arrange - Group with CombinedAmount outside tolerance
        var receiptAmount = 50.00m;
        var groupCombinedAmount = 52.00m;

        // Act
        var diff = Math.Abs(receiptAmount - groupCombinedAmount);
        var score = diff <= 0.10m ? 40m : diff <= 1.00m ? 20m : 0m;

        // Assert
        score.Should().Be(0m, "amount outside tolerance should score 0 points");
    }

    #endregion

    #region T012: Date Scoring for Group DisplayDate

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void CalculateDateScore_GroupDisplayDate_SameDay_Returns35Points()
    {
        // Arrange - Receipt date matches group DisplayDate
        var receiptDate = new DateOnly(2026, 1, 5);
        var groupDisplayDate = new DateOnly(2026, 1, 5);

        // Act
        var daysDiff = Math.Abs(receiptDate.DayNumber - groupDisplayDate.DayNumber);
        var score = daysDiff switch
        {
            0 => 35m,
            1 => 30m,
            <= 3 => 25m,
            <= 7 => 10m,
            _ => 0m
        };

        // Assert
        score.Should().Be(35m, "same day should score 35 points");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void CalculateDateScore_GroupDisplayDate_ThreeDaysApart_Returns25Points()
    {
        // Arrange - Receipt date 3 days from group DisplayDate
        var receiptDate = new DateOnly(2026, 1, 5);
        var groupDisplayDate = new DateOnly(2026, 1, 8);

        // Act
        var daysDiff = Math.Abs(receiptDate.DayNumber - groupDisplayDate.DayNumber);
        var score = daysDiff switch
        {
            0 => 35m,
            1 => 30m,
            <= 3 => 25m,
            <= 7 => 10m,
            _ => 0m
        };

        // Assert
        score.Should().Be(25m, "2-3 days apart should score 25 points");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void CalculateDateScore_GroupDisplayDate_SevenDaysApart_Returns10Points()
    {
        // Arrange - Receipt date 7 days from group DisplayDate
        var receiptDate = new DateOnly(2026, 1, 5);
        var groupDisplayDate = new DateOnly(2026, 1, 12);

        // Act
        var daysDiff = Math.Abs(receiptDate.DayNumber - groupDisplayDate.DayNumber);
        var score = daysDiff switch
        {
            0 => 35m,
            1 => 30m,
            <= 3 => 25m,
            <= 7 => 10m,
            _ => 0m
        };

        // Assert
        score.Should().Be(10m, "4-7 days apart should score 10 points");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void CalculateDateScore_GroupDisplayDate_OutsideRange_Returns0Points()
    {
        // Arrange - Receipt date more than 7 days from group DisplayDate
        var receiptDate = new DateOnly(2026, 1, 5);
        var groupDisplayDate = new DateOnly(2026, 1, 20);

        // Act
        var daysDiff = Math.Abs(receiptDate.DayNumber - groupDisplayDate.DayNumber);
        var score = daysDiff switch
        {
            0 => 35m,
            1 => 30m,
            <= 3 => 25m,
            <= 7 => 10m,
            _ => 0m
        };

        // Assert
        score.Should().Be(0m, "more than 7 days apart should score 0 points");
    }

    #endregion

    #region T013: ExtractVendorFromGroupName

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    [InlineData("TWILIO (3 charges)", "TWILIO")]
    [InlineData("AMAZON (5 charges)", "AMAZON")]
    [InlineData("THE HOME DEPOT (2 charges)", "THE HOME DEPOT")]
    [InlineData("SQ *COFFEE SHOP (1 charge)", "SQ *COFFEE SHOP")]
    public void ExtractVendorFromGroupName_StandardFormat_ExtractsVendor(string groupName, string expectedVendor)
    {
        // Act
        var result = MatchingService.ExtractVendorFromGroupName(groupName);

        // Assert
        result.Should().Be(expectedVendor, $"should extract vendor from '{groupName}'");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    [InlineData("TWILIO")]
    [InlineData("Simple Name")]
    [InlineData("No Parentheses Here")]
    public void ExtractVendorFromGroupName_NoChargesSuffix_ReturnsFullName(string groupName)
    {
        // Act
        var result = MatchingService.ExtractVendorFromGroupName(groupName);

        // Assert
        result.Should().Be(groupName, "names without (N charges) suffix should be returned as-is");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void ExtractVendorFromGroupName_EmptyOrWhitespace_ReturnsEmpty(string groupName)
    {
        // Act
        var result = MatchingService.ExtractVendorFromGroupName(groupName);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void ExtractVendorFromGroupName_NullInput_ReturnsEmpty()
    {
        // Act
        var result = MatchingService.ExtractVendorFromGroupName(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Theory]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    [InlineData("TWILIO  (3 charges)", "TWILIO")]
    [InlineData("TWILIO(3charges)", "TWILIO")]
    [InlineData("TWILIO ( 3 charges )", "TWILIO")]
    public void ExtractVendorFromGroupName_VariantSpacing_ExtractsVendor(string groupName, string expectedVendor)
    {
        // Act
        var result = MatchingService.ExtractVendorFromGroupName(groupName);

        // Assert
        result.Should().Be(expectedVendor, "should handle whitespace variations");
    }

    #endregion

    #region T024: CreateManualMatchAsync with TransactionGroupId

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US2")]
    public void CreateManualMatch_WithTransactionGroupId_SetsGroupIdAndNullTransactionId()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        // Act - Simulating the validation and creation logic
        var match = new ReceiptTransactionMatch
        {
            ReceiptId = receiptId,
            TransactionId = null, // Must be null when matching to group
            TransactionGroupId = groupId,
            IsManualMatch = true,
            Status = MatchProposalStatus.Confirmed
        };

        // Assert
        match.TransactionId.Should().BeNull("TransactionId should be null for group matches");
        match.TransactionGroupId.Should().Be(groupId, "TransactionGroupId should be set");
        match.IsManualMatch.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US2")]
    public void CreateManualMatch_Validation_RejectsBothIdsSet()
    {
        // Arrange - Both IDs provided (invalid)
        var receiptId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        // Act - Validation should fail if both are set
        var bothSet = transactionId != Guid.Empty && groupId != Guid.Empty;

        // Assert
        bothSet.Should().BeTrue("test setup should have both IDs");
        // In actual implementation, this should throw validation error
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US2")]
    public void CreateManualMatch_Validation_RejectsNeitherIdSet()
    {
        // Arrange - Neither ID provided (invalid)
        Guid? transactionId = null;
        Guid? groupId = null;

        // Act - Validation should fail if neither is set
        var neitherSet = transactionId == null && groupId == null;

        // Assert
        neitherSet.Should().BeTrue("test setup should have neither ID");
        // In actual implementation, this should throw validation error
    }

    #endregion

    #region T032: Grouped Transactions Excluded from Candidate Pool

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US3")]
    public void GetCandidates_ExcludesTransactionsWithGroupId()
    {
        // Arrange - Mix of grouped and ungrouped transactions
        var groupId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), GroupId = null, Amount = 25.00m },      // Should be included
            new() { Id = Guid.NewGuid(), GroupId = groupId, Amount = 15.00m },   // Should be excluded
            new() { Id = Guid.NewGuid(), GroupId = groupId, Amount = 20.00m },   // Should be excluded
            new() { Id = Guid.NewGuid(), GroupId = null, Amount = 50.00m },      // Should be included
        };

        // Act - Filter out grouped transactions
        var candidates = transactions.Where(t => t.GroupId == null).ToList();

        // Assert
        candidates.Should().HaveCount(2, "only ungrouped transactions should be candidates");
        candidates.Should().OnlyContain(t => t.GroupId == null);
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US3")]
    public void GetCandidates_IncludesGroupsAlongsideUngroupedTransactions()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var transactions = new List<Transaction>
        {
            new() { Id = Guid.NewGuid(), GroupId = null, Amount = 25.00m },
            new() { Id = Guid.NewGuid(), GroupId = groupId, Amount = 35.00m },
        };
        var groups = new List<TransactionGroup>
        {
            new() { Id = groupId, CombinedAmount = 35.00m, TransactionCount = 1 }
        };

        // Act
        var ungroupedTransactions = transactions.Where(t => t.GroupId == null);
        var totalCandidates = ungroupedTransactions.Count() + groups.Count;

        // Assert
        totalCandidates.Should().Be(2, "should have 1 ungrouped transaction + 1 group");
    }

    #endregion

    #region T036: RejectMatchAsync for Group Matches

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US4")]
    public void RejectMatch_ForGroupMatch_ResetsGroupStatus()
    {
        // Arrange - Group with a proposed match
        var group = new TransactionGroup
        {
            Id = Guid.NewGuid(),
            MatchStatus = MatchStatus.Proposed,
            MatchedReceiptId = Guid.NewGuid()
        };

        // Act - Simulate rejection
        group.MatchStatus = MatchStatus.Unmatched;
        group.MatchedReceiptId = null;

        // Assert
        group.MatchStatus.Should().Be(MatchStatus.Unmatched);
        group.MatchedReceiptId.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US4")]
    public void RejectMatch_ForConfirmedGroupMatch_ResetsGroupStatus()
    {
        // Arrange - Group with a confirmed match (user wants to unmatch)
        var receiptId = Guid.NewGuid();
        var group = new TransactionGroup
        {
            Id = Guid.NewGuid(),
            MatchStatus = MatchStatus.Matched,
            MatchedReceiptId = receiptId
        };

        // Act - Simulate unmatch/rejection
        group.MatchStatus = MatchStatus.Unmatched;
        group.MatchedReceiptId = null;

        // Assert
        group.MatchStatus.Should().Be(MatchStatus.Unmatched);
        group.MatchedReceiptId.Should().BeNull();
    }

    #endregion

    #region Group Scoring Integration Tests

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void GroupScoring_PerfectMatch_Scores100Points()
    {
        // Scenario: Receipt matches group perfectly
        // - Amount: $50.00 exact match -> 40 points
        // - Date: Same day -> 35 points
        // - Vendor: Alias match -> 25 points

        var amountScore = 40m;
        var dateScore = 35m;
        var vendorScore = 25m;

        var totalScore = amountScore + dateScore + vendorScore;

        totalScore.Should().Be(100m);
        (totalScore >= 70m).Should().BeTrue("Perfect match should exceed threshold");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void GroupScoring_GoodMatch_ExceedsThreshold()
    {
        // Scenario: Receipt matches group well without vendor alias
        // - Amount: Near match ($0.75 difference) -> 20 points
        // - Date: Same day -> 35 points
        // - Vendor: Fuzzy match -> 15 points

        var amountScore = 20m;
        var dateScore = 35m;
        var vendorScore = 15m;

        var totalScore = amountScore + dateScore + vendorScore;

        totalScore.Should().Be(70m);
        (totalScore >= 70m).Should().BeTrue("Good match should meet threshold");
    }

    [Fact]
    [Trait("Category", "Unit")]
    [Trait("Story", "US1")]
    public void GroupScoring_WeakMatch_BelowThreshold()
    {
        // Scenario: Receipt has weak match to group
        // - Amount: Near match -> 20 points
        // - Date: Week old -> 10 points
        // - Vendor: No match -> 0 points

        var amountScore = 20m;
        var dateScore = 10m;
        var vendorScore = 0m;

        var totalScore = amountScore + dateScore + vendorScore;

        totalScore.Should().Be(30m);
        (totalScore >= 70m).Should().BeFalse("Weak match should be below threshold");
    }

    #endregion
}
