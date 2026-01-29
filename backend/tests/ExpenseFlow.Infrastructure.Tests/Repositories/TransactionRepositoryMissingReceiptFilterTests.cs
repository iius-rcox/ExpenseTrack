using ExpenseFlow.Core.Entities;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Infrastructure.Repositories;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ExpenseFlow.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for TransactionRepository's 'missing-receipt' filter.
/// Tests the filtering logic for transactions that should have receipts but don't.
///
/// Missing Receipt criteria:
/// 1. No matched receipt (MatchedReceiptId == null)
/// 2. Not dismissed (ReceiptDismissed == null or false)
/// 3. Either:
///    a. Transaction is in a Business group (Group.IsReimbursable == true), OR
///    b. Transaction has a Confirmed prediction (business expense)
/// </summary>
public class TransactionRepositoryMissingReceiptFilterTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly TransactionRepository _sut;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly Guid _testImportId = Guid.NewGuid();

    public TransactionRepositoryMissingReceiptFilterTests()
    {
        // Create in-memory database with unique name per test
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _sut = new TransactionRepository(_dbContext);

        // Seed required user
        _dbContext.Users.Add(new User
        {
            Id = _testUserId,
            EntraObjectId = "test-user-ext-id",
            Email = "test@example.com",
            DisplayName = "Test User",
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.Users.Add(new User
        {
            Id = _otherUserId,
            EntraObjectId = "other-user-ext-id",
            Email = "other@example.com",
            DisplayName = "Other User",
            CreatedAt = DateTime.UtcNow
        });
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region Missing Receipt Filter Tests

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_ReturnsTransactionsInBusinessGroups()
    {
        // Arrange - Transaction in a Business group without receipt
        var businessGroup = CreateTransactionGroup(_testUserId, isReimbursable: true);
        var transaction = CreateTransaction(_testUserId, hasReceipt: false, groupId: businessGroup.Id);

        _dbContext.TransactionGroups.Add(businessGroup);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().HaveCount(1);
        transactions[0].Id.Should().Be(transaction.Id);
        totalCount.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_ReturnsTransactionsWithConfirmedPredictions()
    {
        // Arrange - Transaction with a Confirmed prediction (marked as business expense)
        var transaction = CreateTransaction(_testUserId, hasReceipt: false);
        var prediction = CreatePrediction(transaction.Id, _testUserId, status: PredictionStatus.Confirmed);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().HaveCount(1);
        transactions[0].Id.Should().Be(transaction.Id);
        totalCount.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_ExcludesDismissedTransactions()
    {
        // Arrange - Dismissed transaction in Business group
        var businessGroup = CreateTransactionGroup(_testUserId, isReimbursable: true);
        var dismissedTransaction = CreateTransaction(_testUserId, hasReceipt: false, groupId: businessGroup.Id, isDismissed: true);

        _dbContext.TransactionGroups.Add(businessGroup);
        _dbContext.Transactions.Add(dismissedTransaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().BeEmpty("dismissed transactions should be excluded from missing-receipt filter");
        totalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_ExcludesPersonalExpenses()
    {
        // Arrange - Transaction in a Personal group (IsReimbursable = false)
        var personalGroup = CreateTransactionGroup(_testUserId, isReimbursable: false);
        var transaction = CreateTransaction(_testUserId, hasReceipt: false, groupId: personalGroup.Id);

        _dbContext.TransactionGroups.Add(personalGroup);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().BeEmpty("personal expenses should be excluded from missing-receipt filter");
        totalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_ExcludesRejectedPredictions()
    {
        // Arrange - Transaction with Rejected prediction (not a business expense)
        var transaction = CreateTransaction(_testUserId, hasReceipt: false);
        var prediction = CreatePrediction(transaction.Id, _testUserId, status: PredictionStatus.Rejected);

        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(prediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().BeEmpty("transactions with rejected predictions should be excluded");
        totalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_ExcludesTransactionsWithReceipts()
    {
        // Arrange - Transaction with receipt already matched
        var businessGroup = CreateTransactionGroup(_testUserId, isReimbursable: true);
        var transactionWithReceipt = CreateTransaction(_testUserId, hasReceipt: true, groupId: businessGroup.Id);

        _dbContext.TransactionGroups.Add(businessGroup);
        _dbContext.Transactions.Add(transactionWithReceipt);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().BeEmpty("transactions that already have receipts should be excluded");
        totalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_ExcludesUncategorizedTransactions()
    {
        // Arrange - Transaction without group and without prediction (uncategorized)
        var transaction = CreateTransaction(_testUserId, hasReceipt: false);

        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().BeEmpty("uncategorized transactions should not appear in missing-receipt filter");
        totalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_CombinedWithUnmatchedFilter_ReturnsUnion()
    {
        // Arrange
        // 1. Transaction in Business group without receipt (should match missing-receipt)
        var businessGroup = CreateTransactionGroup(_testUserId, isReimbursable: true);
        var businessTransaction = CreateTransaction(_testUserId, hasReceipt: false, groupId: businessGroup.Id);

        // 2. Uncategorized unmatched transaction (should match unmatched only)
        var unmatchedTransaction = CreateTransaction(_testUserId, hasReceipt: false);
        unmatchedTransaction.Description = "Unmatched Only";

        _dbContext.TransactionGroups.Add(businessGroup);
        _dbContext.Transactions.AddRange(businessTransaction, unmatchedTransaction);
        await _dbContext.SaveChangesAsync();

        // Act - Apply both filters (OR logic)
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt", "unmatched" });

        // Assert - Should return both (OR logic between status filters)
        transactions.Should().HaveCount(2);
        totalCount.Should().Be(2);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_RespectsRowLevelSecurity()
    {
        // Arrange - Business transaction for other user
        var otherBusinessGroup = CreateTransactionGroup(_otherUserId, isReimbursable: true);
        var otherTransaction = CreateTransaction(_otherUserId, hasReceipt: false, groupId: otherBusinessGroup.Id);

        _dbContext.TransactionGroups.Add(otherBusinessGroup);
        _dbContext.Transactions.Add(otherTransaction);
        await _dbContext.SaveChangesAsync();

        // Act - Query as test user
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().BeEmpty("should not see other user's transactions");
        totalCount.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_IncludesPendingPredictionsInBusinessGroup()
    {
        // Arrange - Transaction in Business group with Pending prediction
        // The group marks it as business, so it should show in missing receipts
        var businessGroup = CreateTransactionGroup(_testUserId, isReimbursable: true);
        var transaction = CreateTransaction(_testUserId, hasReceipt: false, groupId: businessGroup.Id);
        var pendingPrediction = CreatePrediction(transaction.Id, _testUserId, status: PredictionStatus.Pending);

        _dbContext.TransactionGroups.Add(businessGroup);
        _dbContext.Transactions.Add(transaction);
        _dbContext.TransactionPredictions.Add(pendingPrediction);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().HaveCount(1, "transaction in Business group should be included regardless of prediction status");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPagedAsync_MissingReceiptFilter_ExcludesGroupsWithNullReimbursable()
    {
        // Arrange - Transaction in a group where IsReimbursable is null (unknown)
        var unknownGroup = CreateTransactionGroup(_testUserId, isReimbursable: null);
        var transaction = CreateTransaction(_testUserId, hasReceipt: false, groupId: unknownGroup.Id);

        _dbContext.TransactionGroups.Add(unknownGroup);
        _dbContext.Transactions.Add(transaction);
        await _dbContext.SaveChangesAsync();

        // Act
        var (transactions, totalCount, _) = await _sut.GetPagedAsync(
            _testUserId,
            page: 1,
            pageSize: 50,
            matchStatus: new List<string> { "missing-receipt" });

        // Assert
        transactions.Should().BeEmpty("groups with unknown reimbursability should not appear in missing-receipt filter");
        totalCount.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private Transaction CreateTransaction(
        Guid userId,
        bool hasReceipt,
        Guid? groupId = null,
        bool isDismissed = false,
        decimal amount = 99.99m)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ImportId = _testImportId,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
            Description = "Test Transaction",
            Amount = amount,
            MatchedReceiptId = hasReceipt ? Guid.NewGuid() : null,
            MatchStatus = hasReceipt ? MatchStatus.Matched : MatchStatus.Unmatched,
            ReceiptDismissed = isDismissed ? true : null,
            GroupId = groupId,
            CreatedAt = DateTime.UtcNow,
            OriginalDescription = "TEST TRANSACTION 12345",
            PostDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-6)),
            DuplicateHash = Guid.NewGuid().ToString()
        };
    }

    private TransactionGroup CreateTransactionGroup(Guid userId, bool? isReimbursable)
    {
        return new TransactionGroup
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Test Group",
            DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
            CombinedAmount = 100m,
            TransactionCount = 1,
            IsReimbursable = isReimbursable,
            CreatedAt = DateTime.UtcNow
        };
    }

    private TransactionPrediction CreatePrediction(
        Guid transactionId,
        Guid userId,
        PredictionStatus status)
    {
        return new TransactionPrediction
        {
            Id = Guid.NewGuid(),
            TransactionId = transactionId,
            UserId = userId,
            Status = status,
            IsManualOverride = false,
            ConfidenceScore = 0.95m,
            ConfidenceLevel = PredictionConfidence.High,
            CreatedAt = DateTime.UtcNow
        };
    }

    #endregion
}
