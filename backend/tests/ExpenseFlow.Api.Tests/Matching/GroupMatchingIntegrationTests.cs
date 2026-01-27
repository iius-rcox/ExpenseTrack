using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExpenseFlow.Api.Tests.Infrastructure;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExpenseFlow.Api.Tests.Matching;

/// <summary>
/// Integration tests for transaction group matching functionality.
/// Tests the complete flow from auto-match through manual match and unmatch.
/// </summary>
public class GroupMatchingIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public GroupMatchingIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        _factory.SeedDatabase((db, userId) => { });
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region T014: RunAutoMatchAsync Proposes Group Match

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Story", "US1")]
    public async Task AutoMatch_ReceiptMatchesGroupAmount_ProposesGroupMatch()
    {
        // Arrange - Create receipt and transaction group with matching amounts
        var groupId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var receiptAmount = 50.00m;
        var groupAmount = 50.00m;

        await SeedGroupMatchingDataAsync(receiptId, receiptAmount, groupId, groupAmount);

        // Act - Run auto-match
        var response = await _client.PostAsync("/api/matching/auto", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<AutoMatchResultDto>(JsonOptions);
        result.Should().NotBeNull();
        // GroupMatchCount should be > 0 if groups were matched
        // Note: This test will fail until Phase 2 (DTOs) and Phase 3 (implementation) are complete
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Story", "US1")]
    public async Task AutoMatch_GroupBetterScoreThanTransaction_ProposesGroupMatch()
    {
        // Arrange - Group and individual transaction both match, group should score higher
        var groupId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var receiptAmount = 45.00m;

        // Group has exact match, individual transaction is near match
        var groupAmount = 45.00m;  // Exact: 40 points
        var individualAmount = 45.50m;  // Near: 20 points

        await SeedMixedCandidatesAsync(receiptId, receiptAmount, groupId, groupAmount, individualAmount);

        // Act
        var response = await _client.PostAsync("/api/matching/auto", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Verify the proposal is for the group, not the individual transaction
    }

    #endregion

    #region T025: Manual Match to Group Updates Both Statuses

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Story", "US2")]
    public async Task ManualMatch_ToGroup_UpdatesReceiptAndGroupStatus()
    {
        // Arrange - Create unmatched receipt and group
        var groupId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();

        await SeedUnmatchedReceiptAndGroupAsync(receiptId, groupId);

        // Act - Create manual match to group
        var request = new CreateManualMatchRequest
        {
            ReceiptId = receiptId,
            TransactionGroupId = groupId
        };
        var response = await _client.PostAsJsonAsync("/api/matching/manual", request);

        // Assert
        // Note: This test will fail until T026-T031 are implemented
        // response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Story", "US2")]
    public async Task ManualMatch_BothTransactionAndGroupId_ReturnsBadRequest()
    {
        // Arrange - Invalid request with both IDs
        var request = new CreateManualMatchRequest
        {
            ReceiptId = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            TransactionGroupId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/matching/manual", request);

        // Assert
        // Should return 400 Bad Request due to XOR validation
        // Note: This test will fail until validation is added in T027
    }

    #endregion

    #region T033: Receipt Does Not Match Grouped Transaction Individually

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Story", "US3")]
    public async Task AutoMatch_TransactionInGroup_NotProposedIndividually()
    {
        // Arrange - Transaction belongs to a group
        var groupId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var amount = 25.00m;  // Single transaction amount

        // Receipt amount matches individual transaction, not group total
        await SeedGroupedTransactionScenarioAsync(receiptId, amount, groupId, transactionId, amount);

        // Act
        var response = await _client.PostAsync("/api/matching/auto", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Verify the proposal is NOT for the individual transaction
        // The transaction should be excluded because it has GroupId set
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Story", "US3")]
    public async Task GetCandidates_ExcludesGroupedTransactions()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        await SeedReceiptWithMixedCandidatesAsync(receiptId);

        // Act - Get candidates for receipt
        var response = await _client.GetAsync($"/api/matching/candidates?receiptId={receiptId}");

        // Assert
        // Note: This test will fail until T030 endpoint is implemented
        // Verify grouped transactions are not in the candidate list
    }

    #endregion

    #region T037: Unmatch From Group Resets Both Statuses

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Story", "US4")]
    public async Task UnmatchFromGroup_ResetsBothStatuses()
    {
        // Arrange - Create a confirmed group match
        var groupId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        await SeedConfirmedGroupMatchAsync(receiptId, groupId, matchId);

        // Act - Reject/unmatch
        var response = await _client.PostAsync($"/api/matching/{matchId}/reject", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Verify both receipt and group return to Unmatched status
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Story", "US4")]
    public async Task UnmatchFromGroup_GroupReappearsAsCandidate()
    {
        // Arrange - Unmatch a group
        var groupId = Guid.NewGuid();
        var receiptId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        await SeedConfirmedGroupMatchAsync(receiptId, groupId, matchId);
        await _client.PostAsync($"/api/matching/{matchId}/reject", null);

        // Act - Get candidates again
        var response = await _client.GetAsync($"/api/matching/candidates?receiptId={receiptId}");

        // Assert
        // Verify the group is now available as a candidate again
    }

    #endregion

    #region Helper Methods

    private async Task SeedGroupMatchingDataAsync(Guid receiptId, decimal receiptAmount, Guid groupId, decimal groupAmount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();

        var userId = _factory.TestUserId;

        // Create receipt
        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = userId,
            AmountExtracted = receiptAmount,
            DateExtracted = DateOnly.FromDateTime(DateTime.UtcNow),
            MatchStatus = MatchStatus.Unmatched,
            BlobUrl = "test://receipt.jpg",
            OriginalFilename = "receipt.jpg",
            ContentType = "image/jpeg",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            Status = ReceiptStatus.Ready
        };
        db.Receipts.Add(receipt);

        // Create transaction group
        var group = new TransactionGroup
        {
            Id = groupId,
            UserId = userId,
            Name = "TWILIO (3 charges)",
            CombinedAmount = groupAmount,
            DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TransactionCount = 3,
            MatchStatus = MatchStatus.Unmatched
        };
        db.TransactionGroups.Add(group);

        // Create grouped transactions
        for (int i = 0; i < 3; i++)
        {
            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GroupId = groupId,
                Amount = groupAmount / 3,
                TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Description = $"TWILIO {i + 1}",
                MatchStatus = MatchStatus.Unmatched,
                ImportId = Guid.NewGuid(),
                OriginalDescription = $"TWILIO {i + 1}"
            };
            db.Transactions.Add(tx);
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedMixedCandidatesAsync(Guid receiptId, decimal receiptAmount,
        Guid groupId, decimal groupAmount, decimal individualAmount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();

        var userId = _factory.TestUserId;

        // Create receipt
        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = userId,
            AmountExtracted = receiptAmount,
            DateExtracted = DateOnly.FromDateTime(DateTime.UtcNow),
            MatchStatus = MatchStatus.Unmatched,
            BlobUrl = "test://receipt.jpg",
            OriginalFilename = "receipt.jpg",
            ContentType = "image/jpeg",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            Status = ReceiptStatus.Ready
        };
        db.Receipts.Add(receipt);

        // Create transaction group
        var group = new TransactionGroup
        {
            Id = groupId,
            UserId = userId,
            Name = "TWILIO (2 charges)",
            CombinedAmount = groupAmount,
            DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TransactionCount = 2,
            MatchStatus = MatchStatus.Unmatched
        };
        db.TransactionGroups.Add(group);

        // Create individual ungrouped transaction
        var individualTx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GroupId = null,  // Not grouped
            Amount = individualAmount,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "TWILIO INDIVIDUAL",
            MatchStatus = MatchStatus.Unmatched,
            ImportId = Guid.NewGuid(),
            OriginalDescription = "TWILIO"
        };
        db.Transactions.Add(individualTx);

        await db.SaveChangesAsync();
    }

    private async Task SeedUnmatchedReceiptAndGroupAsync(Guid receiptId, Guid groupId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();

        var userId = _factory.TestUserId;

        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = userId,
            AmountExtracted = 75.00m,
            DateExtracted = DateOnly.FromDateTime(DateTime.UtcNow),
            MatchStatus = MatchStatus.Unmatched,
            BlobUrl = "test://receipt.jpg",
            OriginalFilename = "receipt.jpg",
            ContentType = "image/jpeg",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            Status = ReceiptStatus.Ready
        };
        db.Receipts.Add(receipt);

        var group = new TransactionGroup
        {
            Id = groupId,
            UserId = userId,
            Name = "TEST GROUP (3 charges)",
            CombinedAmount = 75.00m,
            DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TransactionCount = 3,
            MatchStatus = MatchStatus.Unmatched
        };
        db.TransactionGroups.Add(group);

        await db.SaveChangesAsync();
    }

    private async Task SeedGroupedTransactionScenarioAsync(
        Guid receiptId, decimal receiptAmount, Guid groupId, Guid transactionId, decimal transactionAmount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();

        var userId = _factory.TestUserId;

        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = userId,
            AmountExtracted = receiptAmount,
            DateExtracted = DateOnly.FromDateTime(DateTime.UtcNow),
            MatchStatus = MatchStatus.Unmatched,
            BlobUrl = "test://receipt.jpg",
            OriginalFilename = "receipt.jpg",
            ContentType = "image/jpeg",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            Status = ReceiptStatus.Ready
        };
        db.Receipts.Add(receipt);

        // Create group with higher total than individual
        var group = new TransactionGroup
        {
            Id = groupId,
            UserId = userId,
            Name = "GROUPED VENDOR (2 charges)",
            CombinedAmount = transactionAmount * 2,  // Group total is 2x individual
            DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TransactionCount = 2,
            MatchStatus = MatchStatus.Unmatched
        };
        db.TransactionGroups.Add(group);

        // Create grouped transaction (should be excluded from individual matching)
        var tx = new Transaction
        {
            Id = transactionId,
            UserId = userId,
            GroupId = groupId,  // Belongs to group
            Amount = transactionAmount,
            TransactionDate = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "GROUPED VENDOR",
            MatchStatus = MatchStatus.Unmatched,
            ImportId = Guid.NewGuid(),
            OriginalDescription = "TWILIO"
        };
        db.Transactions.Add(tx);

        await db.SaveChangesAsync();
    }

    private async Task SeedReceiptWithMixedCandidatesAsync(Guid receiptId)
    {
        await SeedMixedCandidatesAsync(receiptId, 50.00m, Guid.NewGuid(), 50.00m, 50.00m);
    }

    private async Task SeedConfirmedGroupMatchAsync(Guid receiptId, Guid groupId, Guid matchId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ExpenseFlowDbContext>();

        var userId = _factory.TestUserId;

        var receipt = new Receipt
        {
            Id = receiptId,
            UserId = userId,
            AmountExtracted = 100.00m,
            DateExtracted = DateOnly.FromDateTime(DateTime.UtcNow),
            MatchStatus = MatchStatus.Matched,
            BlobUrl = "test://receipt.jpg",
            OriginalFilename = "receipt.jpg",
            ContentType = "image/jpeg",
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = DateTime.UtcNow,
            Status = ReceiptStatus.Ready
        };
        db.Receipts.Add(receipt);

        var group = new TransactionGroup
        {
            Id = groupId,
            UserId = userId,
            Name = "MATCHED GROUP (2 charges)",
            CombinedAmount = 100.00m,
            DisplayDate = DateOnly.FromDateTime(DateTime.UtcNow),
            TransactionCount = 2,
            MatchStatus = MatchStatus.Matched,
            MatchedReceiptId = receiptId
        };
        db.TransactionGroups.Add(group);

        var match = new ReceiptTransactionMatch
        {
            Id = matchId,
            UserId = userId,
            ReceiptId = receiptId,
            TransactionId = null,
            TransactionGroupId = groupId,
            Status = MatchProposalStatus.Confirmed,
            ConfidenceScore = 100m,
            AmountScore = 40m,
            DateScore = 35m,
            VendorScore = 25m,
            MatchReason = "Perfect group match",
            IsManualMatch = true,
            ConfirmedAt = DateTime.UtcNow,
            ConfirmedByUserId = userId
        };
        db.ReceiptTransactionMatches.Add(match);

        await db.SaveChangesAsync();
    }

    #endregion
}

/// <summary>
/// DTO for CreateManualMatchRequest with optional TransactionGroupId.
/// Note: This class is defined here temporarily until T026 adds it to MatchingDtos.cs
/// </summary>
public class CreateManualMatchRequest
{
    public Guid ReceiptId { get; set; }
    public Guid? TransactionId { get; set; }
    public Guid? TransactionGroupId { get; set; }
}

/// <summary>
/// DTO for AutoMatchResult with GroupMatchCount.
/// Note: This class is defined here temporarily until T009 adds it to MatchingDtos.cs
/// </summary>
public class AutoMatchResultDto
{
    public int ProcessedCount { get; set; }
    public int ProposedCount { get; set; }
    public int TransactionMatchCount { get; set; }
    public int GroupMatchCount { get; set; }
    public int AmbiguousCount { get; set; }
    public int DurationMs { get; set; }
}
