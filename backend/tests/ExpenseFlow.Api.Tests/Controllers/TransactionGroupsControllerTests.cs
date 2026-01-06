using ExpenseFlow.Api.Controllers;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ExpenseFlow.Api.Tests.Controllers;

/// <summary>
/// Unit tests for TransactionGroupsController.
/// Tests controller logic including request validation, service delegation, and response handling.
/// </summary>
public class TransactionGroupsControllerTests
{
    private readonly Mock<ITransactionGroupService> _groupServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<ILogger<TransactionGroupsController>> _loggerMock;
    private readonly TransactionGroupsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();

    public TransactionGroupsControllerTests()
    {
        _groupServiceMock = new Mock<ITransactionGroupService>();
        _userServiceMock = new Mock<IUserService>();
        _loggerMock = new Mock<ILogger<TransactionGroupsController>>();

        _controller = new TransactionGroupsController(
            _groupServiceMock.Object,
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

    #region CreateGroup Tests

    [Fact]
    public async Task CreateGroup_WithValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            Name = "Test Group"
        };

        var expectedGroup = CreateTestGroupDto();
        _groupServiceMock
            .Setup(x => x.CreateGroupAsync(_testUserId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.CreateGroup(request);

        // Assert
        var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.StatusCode.Should().Be(201);
        createdResult.Value.Should().BeEquivalentTo(expectedGroup);
    }

    [Fact]
    public async Task CreateGroup_WithNullTransactionIds_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            TransactionIds = null!,
            Name = "Test Group"
        };

        // Act
        var result = await _controller.CreateGroup(request);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Detail.Should().Contain("At least 2 transactions");
    }

    [Fact]
    public async Task CreateGroup_WithOnlyOneTransaction_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var result = await _controller.CreateGroup(request);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Detail.Should().Contain("At least 2 transactions");
    }

    [Fact]
    public async Task CreateGroup_WhenServiceThrowsInvalidOperation_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateGroupRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        _groupServiceMock
            .Setup(x => x.CreateGroupAsync(_testUserId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction already in a group"));

        // Act
        var result = await _controller.CreateGroup(request);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Detail.Should().Be("Transaction already in a group");
    }

    #endregion

    #region GetGroup Tests

    [Fact]
    public async Task GetGroup_WhenGroupExists_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var expectedGroup = CreateTestGroupDto(groupId);

        _groupServiceMock
            .Setup(x => x.GetGroupAsync(_testUserId, groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.GetGroup(groupId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedGroup);
    }

    [Fact]
    public async Task GetGroup_WhenGroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        _groupServiceMock
            .Setup(x => x.GetGroupAsync(_testUserId, groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroupDetailDto?)null);

        // Act
        var result = await _controller.GetGroup(groupId);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = notFoundResult.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Title.Should().Be("Group not found");
    }

    #endregion

    #region GetGroups Tests

    [Fact]
    public async Task GetGroups_ReturnsOkWithGroupList()
    {
        // Arrange
        var expectedResponse = new TransactionGroupListResponse
        {
            Groups = new List<TransactionGroupSummaryDto>
            {
                new TransactionGroupSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Group 1",
                    TransactionCount = 2,
                    CombinedAmount = 100.00m,
                    DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    MatchStatus = MatchStatus.Unmatched,
                    CreatedAt = DateTime.UtcNow
                }
            },
            TotalCount = 1
        };

        _groupServiceMock
            .Setup(x => x.GetGroupsAsync(_testUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetGroups();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionGroupListResponse>().Subject;
        response.TotalCount.Should().Be(1);
        response.Groups.Should().HaveCount(1);
    }

    #endregion

    #region UpdateGroup Tests

    [Fact]
    public async Task UpdateGroup_WhenGroupExists_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new UpdateGroupRequest { Name = "Updated Name" };
        var expectedGroup = CreateTestGroupDto(groupId, "Updated Name");

        _groupServiceMock
            .Setup(x => x.UpdateGroupAsync(_testUserId, groupId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.UpdateGroup(groupId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var group = okResult.Value.Should().BeOfType<TransactionGroupDetailDto>().Subject;
        group.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task UpdateGroup_WhenGroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new UpdateGroupRequest { Name = "Updated Name" };

        _groupServiceMock
            .Setup(x => x.UpdateGroupAsync(_testUserId, groupId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroupDetailDto?)null);

        // Act
        var result = await _controller.UpdateGroup(groupId, request);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = notFoundResult.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Title.Should().Be("Group not found");
    }

    [Fact]
    public async Task UpdateGroup_WithDateOverride_SetsIsDateOverridden()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
        var request = new UpdateGroupRequest { DisplayDate = newDate };
        var expectedGroup = CreateTestGroupDto(groupId);
        expectedGroup.DisplayDate = newDate;
        expectedGroup.IsDateOverridden = true;

        _groupServiceMock
            .Setup(x => x.UpdateGroupAsync(_testUserId, groupId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.UpdateGroup(groupId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var group = okResult.Value.Should().BeOfType<TransactionGroupDetailDto>().Subject;
        group.IsDateOverridden.Should().BeTrue();
    }

    #endregion

    #region DeleteGroup Tests

    [Fact]
    public async Task DeleteGroup_WhenGroupExists_ReturnsNoContent()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        _groupServiceMock
            .Setup(x => x.DeleteGroupAsync(_testUserId, groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteGroup(groupId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteGroup_WhenGroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var groupId = Guid.NewGuid();

        _groupServiceMock
            .Setup(x => x.DeleteGroupAsync(_testUserId, groupId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteGroup(groupId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = notFoundResult.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Title.Should().Be("Group not found");
    }

    #endregion

    #region AddTransactions Tests

    [Fact]
    public async Task AddTransactions_WhenValid_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new AddToGroupRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid() }
        };
        var expectedGroup = CreateTestGroupDto(groupId);
        expectedGroup.TransactionCount = 3;

        _groupServiceMock
            .Setup(x => x.AddTransactionsToGroupAsync(_testUserId, groupId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.AddTransactions(groupId, request);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var group = okResult.Value.Should().BeOfType<TransactionGroupDetailDto>().Subject;
        group.TransactionCount.Should().Be(3);
    }

    [Fact]
    public async Task AddTransactions_WhenGroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new AddToGroupRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid() }
        };

        _groupServiceMock
            .Setup(x => x.AddTransactionsToGroupAsync(_testUserId, groupId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroupDetailDto?)null);

        // Act
        var result = await _controller.AddTransactions(groupId, request);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = notFoundResult.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Title.Should().Be("Group not found");
    }

    [Fact]
    public async Task AddTransactions_WhenTransactionAlreadyGrouped_ReturnsBadRequest()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new AddToGroupRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid() }
        };

        _groupServiceMock
            .Setup(x => x.AddTransactionsToGroupAsync(_testUserId, groupId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Transaction(s) already in a group"));

        // Act
        var result = await _controller.AddTransactions(groupId, request);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Detail.Should().Contain("already in a group");
    }

    [Fact]
    public async Task AddTransactions_WhenGroupIsMatched_ReturnsBadRequest()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var request = new AddToGroupRequest
        {
            TransactionIds = new List<Guid> { Guid.NewGuid() }
        };

        _groupServiceMock
            .Setup(x => x.AddTransactionsToGroupAsync(_testUserId, groupId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot add transactions to a matched group"));

        // Act
        var result = await _controller.AddTransactions(groupId, request);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Detail.Should().Contain("matched group");
    }

    #endregion

    #region RemoveTransaction Tests

    [Fact]
    public async Task RemoveTransaction_WhenValid_ReturnsOkResult()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var expectedGroup = CreateTestGroupDto(groupId);
        expectedGroup.TransactionCount = 2;

        _groupServiceMock
            .Setup(x => x.RemoveTransactionFromGroupAsync(_testUserId, groupId, transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedGroup);

        // Act
        var result = await _controller.RemoveTransaction(groupId, transactionId);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var group = okResult.Value.Should().BeOfType<TransactionGroupDetailDto>().Subject;
        group.TransactionCount.Should().Be(2);
    }

    [Fact]
    public async Task RemoveTransaction_WhenGroupNotFound_ReturnsNotFound()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        _groupServiceMock
            .Setup(x => x.RemoveTransactionFromGroupAsync(_testUserId, groupId, transactionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionGroupDetailDto?)null);

        // Act
        var result = await _controller.RemoveTransaction(groupId, transactionId);

        // Assert
        var notFoundResult = result.Result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var problem = notFoundResult.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Title.Should().Be("Group not found");
    }

    [Fact]
    public async Task RemoveTransaction_WhenWouldLeaveLessThanTwo_ReturnsBadRequest()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        _groupServiceMock
            .Setup(x => x.RemoveTransactionFromGroupAsync(_testUserId, groupId, transactionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Groups must have at least 2 transactions"));

        // Act
        var result = await _controller.RemoveTransaction(groupId, transactionId);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Detail.Should().Contain("at least 2 transactions");
    }

    [Fact]
    public async Task RemoveTransaction_WhenTransactionNotInGroup_ReturnsBadRequest()
    {
        // Arrange
        var groupId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();

        _groupServiceMock
            .Setup(x => x.RemoveTransactionFromGroupAsync(_testUserId, groupId, transactionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException($"Transaction {transactionId} is not in group"));

        // Act
        var result = await _controller.RemoveTransaction(groupId, transactionId);

        // Assert
        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var problem = badRequest.Value.Should().BeOfType<ProblemDetailsResponse>().Subject;
        problem.Detail.Should().Contain("is not in group");
    }

    #endregion

    #region GetMixedList Tests

    [Fact]
    public async Task GetMixedList_ReturnsOkWithMixedData()
    {
        // Arrange
        var expectedResponse = new TransactionMixedListResponse
        {
            Transactions = new List<TransactionSummaryDto>
            {
                new TransactionSummaryDto
                {
                    Id = Guid.NewGuid(),
                    Description = "Test Transaction",
                    Amount = 50.00m,
                    TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow)
                }
            },
            Groups = new List<TransactionGroupDetailDto>
            {
                new TransactionGroupDetailDto
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Group",
                    TransactionCount = 2,
                    CombinedAmount = 100.00m,
                    DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    MatchStatus = MatchStatus.Unmatched,
                    CreatedAt = DateTime.UtcNow,
                    IsDateOverridden = false,
                    Transactions = new List<GroupMemberTransactionDto>()
                }
            },
            TotalCount = 2,
            Page = 1,
            PageSize = 50
        };

        _groupServiceMock
            .Setup(x => x.GetMixedListAsync(
                _testUserId,
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<DateOnly?>(),
                It.IsAny<bool?>(),
                It.IsAny<string?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetMixedList();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<TransactionMixedListResponse>().Subject;
        response.Transactions.Should().HaveCount(1);
        response.Groups.Should().HaveCount(1);
        response.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetMixedList_WithFilters_PassesFiltersToService()
    {
        // Arrange
        var startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var endDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var search = "test";

        _groupServiceMock
            .Setup(x => x.GetMixedListAsync(
                _testUserId,
                1,
                50,
                startDate,
                endDate,
                true,
                search,
                "date",
                "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionMixedListResponse());

        // Act
        await _controller.GetMixedList(
            page: 1,
            pageSize: 50,
            startDate: startDate,
            endDate: endDate,
            matched: true,
            search: search);

        // Assert
        _groupServiceMock.Verify(x => x.GetMixedListAsync(
            _testUserId,
            1,
            50,
            startDate,
            endDate,
            true,
            search,
            "date",
            "desc",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static TransactionGroupDetailDto CreateTestGroupDto(
        Guid? id = null,
        string name = "Test Group")
    {
        return new TransactionGroupDetailDto
        {
            Id = id ?? Guid.NewGuid(),
            Name = name,
            DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsDateOverridden = false,
            CombinedAmount = 100.00m,
            TransactionCount = 2,
            MatchStatus = MatchStatus.Unmatched,
            MatchedReceiptId = null,
            CreatedAt = DateTime.UtcNow,
            Transactions = new List<GroupMemberTransactionDto>
            {
                new GroupMemberTransactionDto
                {
                    Id = Guid.NewGuid(),
                    TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    Amount = 50.00m,
                    Description = "Transaction 1"
                },
                new GroupMemberTransactionDto
                {
                    Id = Guid.NewGuid(),
                    TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
                    Amount = 50.00m,
                    Description = "Transaction 2"
                }
            }
        };
    }

    #endregion
}
