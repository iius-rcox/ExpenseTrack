using ExpenseFlow.Core.Entities;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;

namespace ExpenseFlow.Core.Tests.Entities;

/// <summary>
/// Unit tests for Transaction entity.
/// Tests transaction creation, duplicate detection hashing, and match status transitions.
/// </summary>
[Trait("Category", "Unit")]
public class TransactionTests
{
    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var transaction = new Transaction();

        // Assert
        transaction.UserId.Should().Be(Guid.Empty);
        transaction.ImportId.Should().Be(Guid.Empty);
        transaction.Description.Should().BeEmpty();
        transaction.OriginalDescription.Should().BeEmpty();
        transaction.Amount.Should().Be(0);
        transaction.DuplicateHash.Should().BeEmpty();
        transaction.MatchedReceiptId.Should().BeNull();
        transaction.MatchStatus.Should().Be(MatchStatus.Unmatched);
    }

    [Fact]
    public void CanCreate_TransactionWithAllFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var transactionDate = new DateOnly(2025, 12, 15);
        var postDate = new DateOnly(2025, 12, 16);

        // Act
        var transaction = new Transaction
        {
            UserId = userId,
            ImportId = importId,
            TransactionDate = transactionDate,
            PostDate = postDate,
            Description = "Delta Airlines Flight",
            OriginalDescription = "DELTA AIR 0062363598531",
            Amount = 425.00m,
            DuplicateHash = "a1b2c3d4e5f6..."
        };

        // Assert
        transaction.UserId.Should().Be(userId);
        transaction.ImportId.Should().Be(importId);
        transaction.TransactionDate.Should().Be(transactionDate);
        transaction.PostDate.Should().Be(postDate);
        transaction.Description.Should().Be("Delta Airlines Flight");
        transaction.OriginalDescription.Should().Be("DELTA AIR 0062363598531");
        transaction.Amount.Should().Be(425.00m);
        transaction.DuplicateHash.Should().Be("a1b2c3d4e5f6...");
    }

    [Theory]
    [InlineData(MatchStatus.Unmatched)]
    [InlineData(MatchStatus.Proposed)]
    [InlineData(MatchStatus.Matched)]
    public void MatchStatus_CanBeSetToValidValues(MatchStatus status)
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            MatchStatus = status
        };

        // Assert
        transaction.MatchStatus.Should().Be(status);
    }

    [Fact]
    public void MatchStatus_TransitionsFromUnmatchedToProposed()
    {
        // Arrange
        var transaction = new Transaction
        {
            MatchStatus = MatchStatus.Unmatched
        };

        // Act - Simulate system proposing a match
        transaction.MatchStatus = MatchStatus.Proposed;
        transaction.MatchedReceiptId = Guid.NewGuid();

        // Assert
        transaction.MatchStatus.Should().Be(MatchStatus.Proposed);
        transaction.MatchedReceiptId.Should().NotBeNull();
    }

    [Fact]
    public void MatchStatus_TransitionsFromProposedToMatched()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var transaction = new Transaction
        {
            MatchStatus = MatchStatus.Proposed,
            MatchedReceiptId = receiptId
        };

        // Act - Simulate user confirming the match
        transaction.MatchStatus = MatchStatus.Matched;

        // Assert
        transaction.MatchStatus.Should().Be(MatchStatus.Matched);
        transaction.MatchedReceiptId.Should().Be(receiptId);
    }

    [Fact]
    public void Amount_Positive_RepresentsExpense()
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            Amount = 125.50m,
            Description = "Office Supplies"
        };

        // Assert
        transaction.Amount.Should().BePositive();
    }

    [Fact]
    public void Amount_Negative_RepresentsRefund()
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            Amount = -50.00m,
            Description = "Refund - Returned Item"
        };

        // Assert
        transaction.Amount.Should().BeNegative();
    }

    [Fact]
    public void PostDate_CanBeDifferentFromTransactionDate()
    {
        // Arrange - Transaction date is when purchase was made, post date is when it appears on statement
        var transactionDate = new DateOnly(2025, 12, 13); // Friday
        var postDate = new DateOnly(2025, 12, 16);        // Monday (weekend processing)

        // Act
        var transaction = new Transaction
        {
            TransactionDate = transactionDate,
            PostDate = postDate
        };

        // Assert
        transaction.PostDate.Should().BeAfter(transaction.TransactionDate);
        var daysDiff = transaction.PostDate.Value.DayNumber - transaction.TransactionDate.DayNumber;
        daysDiff.Should().Be(3);
    }

    [Fact]
    public void PostDate_CanBeNull_ForPendingTransactions()
    {
        // Arrange & Act
        var transaction = new Transaction
        {
            TransactionDate = new DateOnly(2025, 12, 15),
            PostDate = null
        };

        // Assert
        transaction.PostDate.Should().BeNull();
    }

    [Fact]
    public void DuplicateHash_UsedForDetection()
    {
        // Arrange - Same transaction imported twice should have same hash
        var hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        // Act
        var transaction1 = new Transaction { DuplicateHash = hash };
        var transaction2 = new Transaction { DuplicateHash = hash };

        // Assert
        transaction1.DuplicateHash.Should().Be(transaction2.DuplicateHash);
    }
}
