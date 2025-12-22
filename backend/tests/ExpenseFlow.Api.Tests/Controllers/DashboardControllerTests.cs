using ExpenseFlow.Api.Controllers;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ExpenseFlow.Api.Tests.Controllers;

public class DashboardControllerTests : IDisposable
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<DashboardController>> _loggerMock;
    private readonly DashboardController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public DashboardControllerTests()
    {
        var options = new DbContextOptionsBuilder<ExpenseFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ExpenseFlowDbContext(options);
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<DashboardController>>();

        _controller = new DashboardController(
            _dbContext,
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

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    #region GetActions Tests

    [Fact]
    public async Task GetActions_NoPendingMatches_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetActions();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var actions = okResult.Value.Should().BeOfType<List<PendingActionDto>>().Subject;
        actions.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActions_WithPendingMatches_ReturnsPendingActions()
    {
        // Arrange
        var receipt = new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            OriginalFilename = "receipt.jpg",
            VendorExtracted = "Starbucks",
            Status = ReceiptStatus.Processed,
            StorageUrl = "https://blob.test/receipts/receipt.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024
        };
        _dbContext.Receipts.Add(receipt);

        var transaction = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Description = "STARBUCKS #1234",
            Amount = 5.67m,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
        _dbContext.Transactions.Add(transaction);

        var match = new ReceiptTransactionMatch
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ReceiptId = receipt.Id,
            TransactionId = transaction.Id,
            Status = MatchProposalStatus.Proposed,
            ConfidenceScore = 85.5m,
            CreatedAt = DateTime.UtcNow,
            Receipt = receipt,
            Transaction = transaction
        };
        _dbContext.ReceiptTransactionMatches.Add(match);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetActions();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var actions = okResult.Value.Should().BeOfType<List<PendingActionDto>>().Subject;
        actions.Should().HaveCount(1);
        actions[0].Type.Should().Be("match_review");
        actions[0].Title.Should().Contain("Review");
    }

    [Fact]
    public async Task GetActions_WithLimit_RespectsLimit()
    {
        // Arrange
        var receipt1 = CreateReceipt("receipt1.jpg", "Vendor1");
        var receipt2 = CreateReceipt("receipt2.jpg", "Vendor2");
        var receipt3 = CreateReceipt("receipt3.jpg", "Vendor3");
        _dbContext.Receipts.AddRange(receipt1, receipt2, receipt3);

        var transaction1 = CreateTransaction("Transaction1", 10m);
        var transaction2 = CreateTransaction("Transaction2", 20m);
        var transaction3 = CreateTransaction("Transaction3", 30m);
        _dbContext.Transactions.AddRange(transaction1, transaction2, transaction3);

        _dbContext.ReceiptTransactionMatches.AddRange(
            CreateMatch(receipt1, transaction1, 1),
            CreateMatch(receipt2, transaction2, 2),
            CreateMatch(receipt3, transaction3, 3)
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetActions(limit: 2);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var actions = okResult.Value.Should().BeOfType<List<PendingActionDto>>().Subject;
        actions.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetActions_OnlyReturnsProposedStatus()
    {
        // Arrange
        var receipt1 = CreateReceipt("receipt1.jpg", "Vendor1");
        var receipt2 = CreateReceipt("receipt2.jpg", "Vendor2");
        _dbContext.Receipts.AddRange(receipt1, receipt2);

        var transaction1 = CreateTransaction("Transaction1", 10m);
        var transaction2 = CreateTransaction("Transaction2", 20m);
        _dbContext.Transactions.AddRange(transaction1, transaction2);

        var proposedMatch = CreateMatch(receipt1, transaction1, 1);
        var confirmedMatch = CreateMatch(receipt2, transaction2, 2);
        confirmedMatch.Status = MatchProposalStatus.Confirmed;
        confirmedMatch.ConfirmedAt = DateTime.UtcNow;

        _dbContext.ReceiptTransactionMatches.AddRange(proposedMatch, confirmedMatch);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetActions();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var actions = okResult.Value.Should().BeOfType<List<PendingActionDto>>().Subject;
        actions.Should().HaveCount(1);
        actions[0].Id.Should().Be(proposedMatch.Id.ToString());
    }

    [Fact]
    public async Task GetActions_ReturnsMetadataWithConfidenceScore()
    {
        // Arrange
        var receipt = CreateReceipt("receipt.jpg", "Starbucks");
        _dbContext.Receipts.Add(receipt);

        var transaction = CreateTransaction("STARBUCKS", 5.67m);
        _dbContext.Transactions.Add(transaction);

        var match = CreateMatch(receipt, transaction, 1);
        match.ConfidenceScore = 92.5m;
        _dbContext.ReceiptTransactionMatches.Add(match);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetActions();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var actions = okResult.Value.Should().BeOfType<List<PendingActionDto>>().Subject;
        actions.Should().HaveCount(1);
        actions[0].Metadata.Should().ContainKey("confidenceScore");
        actions[0].Metadata!["confidenceScore"].Should().Be(92.5m);
    }

    #endregion

    #region Helper Methods

    private Receipt CreateReceipt(string filename, string vendor)
    {
        return new Receipt
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            OriginalFilename = filename,
            VendorExtracted = vendor,
            Status = ReceiptStatus.Processed,
            StorageUrl = $"https://blob.test/receipts/{filename}",
            ContentType = "image/jpeg",
            FileSizeBytes = 1024
        };
    }

    private Transaction CreateTransaction(string description, decimal amount)
    {
        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            Description = description,
            Amount = amount,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow)
        };
    }

    private ReceiptTransactionMatch CreateMatch(Receipt receipt, Transaction transaction, int daysAgo)
    {
        return new ReceiptTransactionMatch
        {
            Id = Guid.NewGuid(),
            UserId = _testUserId,
            ReceiptId = receipt.Id,
            TransactionId = transaction.Id,
            Status = MatchProposalStatus.Proposed,
            ConfidenceScore = 80m,
            CreatedAt = DateTime.UtcNow.AddDays(-daysAgo),
            Receipt = receipt,
            Transaction = transaction
        };
    }

    #endregion
}
