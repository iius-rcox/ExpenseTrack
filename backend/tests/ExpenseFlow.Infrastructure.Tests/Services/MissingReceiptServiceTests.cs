using ExpenseFlow.Core.Entities;
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
/// Unit tests for MissingReceiptService.
/// Tests query logic, pagination, sorting, and mutation operations.
///
/// Uses in-memory DbContext for database operations.
/// </summary>
public class MissingReceiptServiceTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly MissingReceiptService _sut;
    private readonly Mock<ILogger<MissingReceiptService>> _loggerMock;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();

    public MissingReceiptServiceTests()
    {
        _loggerMock = new Mock<ILogger<MissingReceiptService>>();

        // Create in-memory database with unique name per test
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _sut = new MissingReceiptService(_dbContext, _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetMissingReceiptsAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithNoTransactions_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetMissingReceiptsAsync(_testUserId);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.Page.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithConfirmedPrediction_ReturnsTransaction()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: false);
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetMissingReceiptsAsync(_testUserId);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].TransactionId.Should().Be(transaction.Id);
        result.Items[0].Source.Should().Be(ReimbursabilitySource.AIPrediction);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithManualOverride_ReturnsUserOverrideSource()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: false);
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: true);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetMissingReceiptsAsync(_testUserId);

        // Assert
        result.Items[0].Source.Should().Be(ReimbursabilitySource.UserOverride);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithMatchedReceipt_ExcludesTransaction()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: true);
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetMissingReceiptsAsync(_testUserId);

        // Assert
        result.Items.Should().BeEmpty("transactions with matched receipts should be excluded");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithDismissedTransaction_ExcludesFromDefaultQuery()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: false, isDismissed: true);
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetMissingReceiptsAsync(_testUserId, includeDismissed: false);

        // Assert
        result.Items.Should().BeEmpty("dismissed transactions should be excluded by default");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithIncludeDismissed_IncludesDismissedTransactions()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: false, isDismissed: true);
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetMissingReceiptsAsync(_testUserId, includeDismissed: true);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].IsDismissed.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithOtherUserTransaction_ExcludesOtherUserData()
    {
        // Arrange
        var ownTransaction = CreateTransaction(_testUserId, hasReceipt: false);
        var otherTransaction = CreateTransaction(_otherUserId, hasReceipt: false);
        var ownPrediction = CreatePrediction(ownTransaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);
        var otherPrediction = CreatePrediction(otherTransaction.Id, _otherUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.AddRange(ownTransaction, otherTransaction);
        _dbContext.TransactionPredictions.AddRange(ownPrediction, otherPrediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetMissingReceiptsAsync(_testUserId);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].TransactionId.Should().Be(ownTransaction.Id);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange - Create 30 transactions
        for (int i = 0; i < 30; i++)
        {
            var transaction = CreateTransaction(_testUserId, hasReceipt: false);
            transaction.TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i));
            var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

            _dbContext.Transactions.Add(transaction);
            _dbContext.TransactionPredictions.Add(prediction);
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var page1 = await _sut.GetMissingReceiptsAsync(_testUserId, page: 1, pageSize: 10);
        var page2 = await _sut.GetMissingReceiptsAsync(_testUserId, page: 2, pageSize: 10);

        // Assert
        page1.Items.Should().HaveCount(10);
        page1.Page.Should().Be(1);
        page1.TotalCount.Should().Be(30);
        page1.TotalPages.Should().Be(3);

        page2.Items.Should().HaveCount(10);
        page2.Page.Should().Be(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMissingReceiptsAsync_WithSortByAmount_ReturnsSortedResults()
    {
        // Arrange
        var transactions = new[]
        {
            CreateTransaction(_testUserId, hasReceipt: false, amount: 100m),
            CreateTransaction(_testUserId, hasReceipt: false, amount: 50m),
            CreateTransaction(_testUserId, hasReceipt: false, amount: 200m)
        };

        foreach (var t in transactions)
        {
            _dbContext.Transactions.Add(t);
            _dbContext.TransactionPredictions.Add(
                CreatePrediction(t.Id, _testUserId, isConfirmed: true, isManualOverride: false));
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var descResult = await _sut.GetMissingReceiptsAsync(_testUserId, sortBy: "amount", sortOrder: "desc");
        var ascResult = await _sut.GetMissingReceiptsAsync(_testUserId, sortBy: "amount", sortOrder: "asc");

        // Assert
        descResult.Items.Select(i => i.Amount).Should().BeInDescendingOrder();
        ascResult.Items.Select(i => i.Amount).Should().BeInAscendingOrder();
    }

    #endregion

    #region GetWidgetDataAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetWidgetDataAsync_ReturnsTop3Items()
    {
        // Arrange - Create 5 transactions
        for (int i = 0; i < 5; i++)
        {
            var transaction = CreateTransaction(_testUserId, hasReceipt: false);
            transaction.TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i));
            var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

            _dbContext.Transactions.Add(transaction);
            _dbContext.TransactionPredictions.Add(prediction);
        }
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetWidgetDataAsync(_testUserId);

        // Assert
        result.TotalCount.Should().Be(5);
        result.RecentItems.Should().HaveCount(3);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetWidgetDataAsync_ExcludesDismissedTransactions()
    {
        // Arrange
        var activeTransaction = CreateTransaction(_testUserId, hasReceipt: false, isDismissed: false);
        var dismissedTransaction = CreateTransaction(_testUserId, hasReceipt: false, isDismissed: true);

        _dbContext.Transactions.AddRange(activeTransaction, dismissedTransaction);
        _dbContext.TransactionPredictions.AddRange(
            CreatePrediction(activeTransaction.Id, _testUserId, isConfirmed: true, isManualOverride: false),
            CreatePrediction(dismissedTransaction.Id, _testUserId, isConfirmed: true, isManualOverride: false));
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.GetWidgetDataAsync(_testUserId);

        // Assert
        result.TotalCount.Should().Be(1);
        result.RecentItems.Should().HaveCount(1);
    }

    #endregion

    #region UpdateReceiptUrlAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptUrlAsync_WithValidTransaction_UpdatesUrl()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: false);
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.UpdateReceiptUrlAsync(_testUserId, transaction.Id, "https://example.com/receipt.pdf");

        // Assert
        result.Should().NotBeNull();
        result!.ReceiptUrl.Should().Be("https://example.com/receipt.pdf");

        var updated = await _dbContext.Transactions.FindAsync(transaction.Id);
        updated!.ReceiptUrl.Should().Be("https://example.com/receipt.pdf");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptUrlAsync_WithEmptyUrl_ClearsUrl()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: false);
        transaction.ReceiptUrl = "https://example.com/old.pdf";
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.UpdateReceiptUrlAsync(_testUserId, transaction.Id, "");

        // Assert
        result!.ReceiptUrl.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptUrlAsync_WithNonExistentTransaction_ReturnsNull()
    {
        // Act
        var result = await _sut.UpdateReceiptUrlAsync(_testUserId, Guid.NewGuid(), "https://example.com/receipt.pdf");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateReceiptUrlAsync_WithOtherUserTransaction_ReturnsNull()
    {
        // Arrange
        var transaction = CreateTransaction(_otherUserId, hasReceipt: false);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.UpdateReceiptUrlAsync(_testUserId, transaction.Id, "https://example.com/receipt.pdf");

        // Assert
        result.Should().BeNull("users should not be able to update other users' transactions");
    }

    #endregion

    #region DismissTransactionAsync Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DismissTransactionAsync_WithTrue_DismissesTransaction()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: false);
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DismissTransactionAsync(_testUserId, transaction.Id, true);

        // Assert
        result!.IsDismissed.Should().BeTrue();

        var updated = await _dbContext.Transactions.FindAsync(transaction.Id);
        updated!.ReceiptDismissed.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DismissTransactionAsync_WithFalse_RestoresTransaction()
    {
        // Arrange
        var transaction = CreateTransaction(_testUserId, hasReceipt: false, isDismissed: true);
        var prediction = CreatePrediction(transaction.Id, _testUserId, isConfirmed: true, isManualOverride: false);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DismissTransactionAsync(_testUserId, transaction.Id, false);

        // Assert
        result!.IsDismissed.Should().BeFalse();

        var updated = await _dbContext.Transactions.FindAsync(transaction.Id);
        updated!.ReceiptDismissed.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DismissTransactionAsync_WithOtherUserTransaction_ReturnsNull()
    {
        // Arrange
        var transaction = CreateTransaction(_otherUserId, hasReceipt: false);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _sut.DismissTransactionAsync(_testUserId, transaction.Id, true);

        // Assert
        result.Should().BeNull("users should not be able to dismiss other users' transactions");
    }

    #endregion

    #region Helper Methods

    private Transaction CreateTransaction(
        Guid userId,
        bool hasReceipt,
        bool isDismissed = false,
        decimal amount = 99.99m)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            Description = "Test Transaction",
            Amount = amount,
            MatchedReceiptId = hasReceipt ? Guid.NewGuid() : null,
            ReceiptDismissed = isDismissed ? true : null,
            CreatedAt = DateTime.UtcNow,
            // Required fields
            RawDescription = "TEST TRANSACTION 12345",
            PostedDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)),
            DuplicateHash = Guid.NewGuid().ToString()
        };
    }

    private TransactionPrediction CreatePrediction(
        Guid transactionId,
        Guid userId,
        bool isConfirmed,
        bool isManualOverride)
    {
        return new TransactionPrediction
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            UserId = userId,
            Status = isConfirmed ? PredictionStatus.Confirmed : PredictionStatus.Pending,
            IsManualOverride = isManualOverride,
            IsReimbursable = true,
            Confidence = 0.95m,
            CreatedAt = DateTime.UtcNow,
            // Required fields
            PredictedGlAccountCode = "6000",
            PredictedDepartment = "IT"
        };
    }

    #endregion
}
