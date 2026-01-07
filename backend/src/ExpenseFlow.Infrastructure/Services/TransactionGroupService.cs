using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using ExpenseFlow.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for managing transaction groups.
/// Handles grouping multiple transactions into single units for receipt matching.
/// </summary>
public class TransactionGroupService : ITransactionGroupService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly IExpensePredictionService _predictionService;
    private readonly ILogger<TransactionGroupService> _logger;

    public TransactionGroupService(
        ExpenseFlowDbContext dbContext,
        IExpensePredictionService predictionService,
        ILogger<TransactionGroupService> logger)
    {
        _dbContext = dbContext;
        _predictionService = predictionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TransactionGroupDetailDto> CreateGroupAsync(
        Guid userId,
        CreateGroupRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating transaction group for user {UserId} with {Count} transactions",
            userId, request.TransactionIds.Count);

        // Use execution strategy to support NpgsqlRetryingExecutionStrategy with transactions
        // The strategy wraps the entire transaction so it can retry on transient failures
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Use a database transaction with row-level locking to prevent race conditions
            // where concurrent requests could group the same transactions
            await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, ct);

            try
            {
                // Fetch transactions with row locks (FOR UPDATE prevents concurrent modifications)
                // This ensures no other transaction can modify these rows until we commit
                var transactionIdsList = request.TransactionIds.ToList();
                var transactions = await _dbContext.Transactions
                    .FromSqlRaw(
                        @"SELECT * FROM transactions
                          WHERE user_id = {0} AND id = ANY({1})
                          FOR UPDATE",
                        userId, transactionIdsList)
                    .ToListAsync(ct);

                // Validate all requested transactions exist and belong to user
                if (transactions.Count != request.TransactionIds.Count)
                {
                    var foundIds = transactions.Select(t => t.Id).ToHashSet();
                    var missingIds = request.TransactionIds.Where(id => !foundIds.Contains(id)).ToList();
                    throw new InvalidOperationException(
                        $"Transaction(s) not found: {string.Join(", ", missingIds)}");
                }

                // Validate none are already grouped
                var alreadyGrouped = transactions.Where(t => t.GroupId.HasValue).ToList();
                if (alreadyGrouped.Any())
                {
                    var groupedIds = string.Join(", ", alreadyGrouped.Select(t => t.Id));
                    throw new InvalidOperationException(
                        $"Transaction(s) already in a group: {groupedIds}");
                }

                // Validate none already have matched receipts
                var alreadyMatched = transactions.Where(t => t.MatchedReceiptId.HasValue).ToList();
                if (alreadyMatched.Any())
                {
                    var matchedIds = string.Join(", ", alreadyMatched.Select(t => t.Id));
                    throw new InvalidOperationException(
                        $"Transaction(s) already have matched receipts: {matchedIds}");
                }

                // Calculate group properties
                var combinedAmount = transactions.Sum(t => t.Amount);
                var maxDate = transactions.Max(t => t.TransactionDate);
                var transactionCount = transactions.Count;

                // Generate name if not provided
                var name = request.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = GenerateGroupName(transactions);
                }

                // Use provided display date or max date
                var displayDate = request.DisplayDate ?? maxDate;
                var isDateOverridden = request.DisplayDate.HasValue;

                // Create the group
                var group = new TransactionGroup
                {
                    UserId = userId,
                    Name = name,
                    DisplayDate = displayDate,
                    IsDateOverridden = isDateOverridden,
                    CombinedAmount = combinedAmount,
                    TransactionCount = transactionCount,
                    MatchStatus = MatchStatus.Unmatched,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.TransactionGroups.Add(group);

                // Save the group first to ensure it exists in DB before FK references
                await _dbContext.SaveChangesAsync(ct);

                // Now assign transactions to the group (FK constraint is satisfied)
                foreach (var transaction in transactions)
                {
                    transaction.GroupId = group.Id;
                }

                await _dbContext.SaveChangesAsync(ct);
                await dbTransaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Created transaction group {GroupId} '{Name}' with {Count} transactions, amount {Amount}",
                    group.Id, group.Name, group.TransactionCount, group.CombinedAmount);

                return MapToDetailDto(group, transactions);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                // Rollback on any unexpected error (InvalidOperationException is validation, not DB error)
                await dbTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to create transaction group for user {UserId}", userId);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public async Task<TransactionGroupDetailDto?> GetGroupAsync(
        Guid userId,
        Guid groupId,
        CancellationToken ct = default)
    {
        var group = await _dbContext.TransactionGroups
            .Include(g => g.Transactions)
            .Where(g => g.Id == groupId && g.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (group == null)
        {
            return null;
        }

        return MapToDetailDto(group, group.Transactions.ToList());
    }

    /// <inheritdoc />
    public async Task<TransactionGroupListResponse> GetGroupsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var groups = await _dbContext.TransactionGroups
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.DisplayDate)
            .Select(g => new TransactionGroupSummaryDto
            {
                Id = g.Id,
                Name = g.Name,
                DisplayDate = g.DisplayDate,
                CombinedAmount = g.CombinedAmount,
                TransactionCount = g.TransactionCount,
                MatchStatus = g.MatchStatus,
                MatchedReceiptId = g.MatchedReceiptId,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync(ct);

        return new TransactionGroupListResponse
        {
            Groups = groups,
            TotalCount = groups.Count
        };
    }

    /// <inheritdoc />
    public async Task<TransactionGroupDetailDto?> UpdateGroupAsync(
        Guid userId,
        Guid groupId,
        UpdateGroupRequest request,
        CancellationToken ct = default)
    {
        var group = await _dbContext.TransactionGroups
            .Include(g => g.Transactions)
            .Where(g => g.Id == groupId && g.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (group == null)
        {
            return null;
        }

        // Update name if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            group.Name = request.Name;
        }

        // Update display date if provided (marks as overridden)
        if (request.DisplayDate.HasValue)
        {
            group.DisplayDate = request.DisplayDate.Value;
            group.IsDateOverridden = true;
        }

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Updated transaction group {GroupId} for user {UserId}",
            groupId, userId);

        return MapToDetailDto(group, group.Transactions.ToList());
    }

    /// <inheritdoc />
    public async Task<bool> DeleteGroupAsync(
        Guid userId,
        Guid groupId,
        CancellationToken ct = default)
    {
        var group = await _dbContext.TransactionGroups
            .Include(g => g.Transactions)
            .Where(g => g.Id == groupId && g.UserId == userId)
            .FirstOrDefaultAsync(ct);

        if (group == null)
        {
            return false;
        }

        // T046: Handle matched group deletion - return receipt to unmatched status
        if (group.MatchedReceiptId.HasValue)
        {
            var receipt = await _dbContext.Receipts
                .FirstOrDefaultAsync(r => r.Id == group.MatchedReceiptId.Value, ct);

            if (receipt != null)
            {
                receipt.MatchStatus = Core.Entities.MatchStatus.Unmatched;
                _logger.LogInformation(
                    "Resetting receipt {ReceiptId} to unmatched due to group {GroupId} deletion",
                    receipt.Id, groupId);
            }

            // Remove the match record (FK cascade should handle this, but explicit is clearer)
            var matchRecord = await _dbContext.ReceiptTransactionMatches
                .FirstOrDefaultAsync(m => m.TransactionGroupId == groupId, ct);
            if (matchRecord != null)
            {
                _dbContext.ReceiptTransactionMatches.Remove(matchRecord);
            }
        }

        // Clear group reference from all transactions
        foreach (var transaction in group.Transactions)
        {
            transaction.GroupId = null;
        }

        // Remove the group
        _dbContext.TransactionGroups.Remove(group);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Deleted transaction group {GroupId} for user {UserId}, released {Count} transactions{MatchInfo}",
            groupId, userId, group.TransactionCount,
            group.MatchedReceiptId.HasValue ? $", unmatched receipt {group.MatchedReceiptId}" : "");

        return true;
    }

    /// <inheritdoc />
    public async Task<TransactionGroupDetailDto?> AddTransactionsToGroupAsync(
        Guid userId,
        Guid groupId,
        AddToGroupRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Adding {Count} transactions to group {GroupId} for user {UserId}",
            request.TransactionIds.Count, groupId, userId);

        // Use execution strategy to support NpgsqlRetryingExecutionStrategy with transactions
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Use a database transaction with row-level locking to prevent race conditions
            // where concurrent requests could add the same transactions to multiple groups
            await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, ct);

            try
            {
                // Step 1: Lock the group row to prevent concurrent modifications
                // Note: We DON'T use Include() here because it generates a separate query
                // without FOR UPDATE, which would leave child transactions unlocked
                var group = await _dbContext.TransactionGroups
                    .FromSqlRaw(
                        @"SELECT * FROM transaction_groups
                          WHERE id = {0} AND user_id = {1}
                          FOR UPDATE",
                        groupId, userId)
                    .FirstOrDefaultAsync(ct);

                if (group == null)
                {
                    return null;
                }

                // Step 2: Lock and load existing transactions in the group
                // This ensures no concurrent operation can modify group membership
                var existingTransactions = await _dbContext.Transactions
                    .FromSqlRaw(
                        @"SELECT * FROM transactions
                          WHERE group_id = {0}
                          FOR UPDATE",
                        group.Id)
                    .ToListAsync(ct);

                // Populate the navigation collection with locked transactions
                foreach (var tx in existingTransactions)
                {
                    group.Transactions.Add(tx);
                }

                // Prevent modification of matched groups
                if (group.MatchedReceiptId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Cannot add transactions to a matched group. Remove the receipt match first.");
                }

                // Step 3: Fetch and lock new transactions (FOR UPDATE prevents concurrent modifications)
                var transactionIdsList = request.TransactionIds.ToList();
                var newTransactions = await _dbContext.Transactions
                    .FromSqlRaw(
                        @"SELECT * FROM transactions
                          WHERE user_id = {0} AND id = ANY({1})
                          FOR UPDATE",
                        userId, transactionIdsList)
                    .ToListAsync(ct);

                // Validate all requested transactions exist
                if (newTransactions.Count != request.TransactionIds.Count)
                {
                    var foundIds = newTransactions.Select(t => t.Id).ToHashSet();
                    var missingIds = request.TransactionIds.Where(id => !foundIds.Contains(id)).ToList();
                    throw new InvalidOperationException(
                        $"Transaction(s) not found: {string.Join(", ", missingIds)}");
                }

                // Validate none are already grouped
                var alreadyGrouped = newTransactions.Where(t => t.GroupId.HasValue).ToList();
                if (alreadyGrouped.Any())
                {
                    var groupedIds = string.Join(", ", alreadyGrouped.Select(t => t.Id));
                    throw new InvalidOperationException(
                        $"Transaction(s) already in a group: {groupedIds}");
                }

                // Validate none already have matched receipts
                var alreadyMatched = newTransactions.Where(t => t.MatchedReceiptId.HasValue).ToList();
                if (alreadyMatched.Any())
                {
                    var matchedIds = string.Join(", ", alreadyMatched.Select(t => t.Id));
                    throw new InvalidOperationException(
                        $"Transaction(s) already have matched receipts: {matchedIds}");
                }

                // Add transactions to group
                foreach (var transaction in newTransactions)
                {
                    transaction.GroupId = group.Id;
                    group.Transactions.Add(transaction);
                }

                // Recalculate aggregates
                RecalculateGroupAggregates(group);
                await _dbContext.SaveChangesAsync(ct);
                await dbTransaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Added {Count} transactions to group {GroupId}, new total: {Total}",
                    newTransactions.Count, groupId, group.TransactionCount);

                return MapToDetailDto(group, group.Transactions.ToList());
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                // Rollback on any unexpected error (InvalidOperationException is validation, not DB error)
                await dbTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to add transactions to group {GroupId}", groupId);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public async Task<TransactionGroupDetailDto?> RemoveTransactionFromGroupAsync(
        Guid userId,
        Guid groupId,
        Guid transactionId,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Removing transaction {TransactionId} from group {GroupId} for user {UserId}",
            transactionId, groupId, userId);

        // Use execution strategy to support NpgsqlRetryingExecutionStrategy with transactions
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Use a database transaction with row-level locking to prevent race conditions
            // where concurrent removals could cause inconsistent group state
            await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.ReadCommitted, ct);

            try
            {
                // Step 1: Lock the group row to prevent concurrent modifications
                // Note: We DON'T use Include() here because it generates a separate query
                // without FOR UPDATE, which would leave child transactions unlocked
                var group = await _dbContext.TransactionGroups
                    .FromSqlRaw(
                        @"SELECT * FROM transaction_groups
                          WHERE id = {0} AND user_id = {1}
                          FOR UPDATE",
                        groupId, userId)
                    .FirstOrDefaultAsync(ct);

                if (group == null)
                {
                    return null;
                }

                // Step 2: Lock and load ALL transactions in the group
                // This ensures no concurrent operation can modify group membership
                // and we get an accurate count for the "minimum 2 transactions" validation
                var existingTransactions = await _dbContext.Transactions
                    .FromSqlRaw(
                        @"SELECT * FROM transactions
                          WHERE group_id = {0}
                          FOR UPDATE",
                        group.Id)
                    .ToListAsync(ct);

                // Populate the navigation collection with locked transactions
                foreach (var tx in existingTransactions)
                {
                    group.Transactions.Add(tx);
                }

                // Step 3: Find the transaction being removed from the already-locked collection
                // No need for another DB query - we already have all transactions locked
                var transaction = existingTransactions.FirstOrDefault(t => t.Id == transactionId);

                if (transaction == null)
                {
                    throw new InvalidOperationException(
                        $"Transaction {transactionId} is not in group {groupId}");
                }

                // Validate removal won't leave fewer than 2 transactions
                if (group.Transactions.Count <= 2)
                {
                    throw new InvalidOperationException(
                        "Groups must have at least 2 transactions. Use ungroup (DELETE) to dissolve the group.");
                }

                // Remove from group
                transaction.GroupId = null;
                group.Transactions.Remove(transaction);

                // Recalculate aggregates
                RecalculateGroupAggregates(group);

                // T047: Check for amount mismatch if group is matched
                string? warning = null;
                if (group.MatchedReceiptId.HasValue)
                {
                    var receipt = await _dbContext.Receipts
                        .FirstOrDefaultAsync(r => r.Id == group.MatchedReceiptId.Value, ct);

                    if (receipt?.AmountExtracted.HasValue == true)
                    {
                        var difference = Math.Abs(group.CombinedAmount - receipt.AmountExtracted.Value);
                        if (difference > 1.00m)
                        {
                            warning = $"Amount mismatch: Group total ({group.CombinedAmount:C}) differs from matched receipt ({receipt.AmountExtracted.Value:C}) by {difference:C}. Consider reviewing this match.";
                            _logger.LogWarning(
                                "Transaction removal caused amount mismatch in group {GroupId}: " +
                                "Group amount {GroupAmount}, Receipt amount {ReceiptAmount}, Difference {Difference}",
                                groupId, group.CombinedAmount, receipt.AmountExtracted.Value, difference);
                        }
                    }
                }

                await _dbContext.SaveChangesAsync(ct);
                await dbTransaction.CommitAsync(ct);

                _logger.LogInformation(
                    "Removed transaction {TransactionId} from group {GroupId}, remaining: {Count}",
                    transactionId, groupId, group.TransactionCount);

                return MapToDetailDto(group, group.Transactions.ToList(), warning);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                // Rollback on any unexpected error (InvalidOperationException is validation, not DB error)
                await dbTransaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to remove transaction {TransactionId} from group {GroupId}",
                    transactionId, groupId);
                throw;
            }
        });
    }

    /// <inheritdoc />
    public async Task<TransactionMixedListResponse> GetMixedListAsync(
        Guid userId,
        int page = 1,
        int pageSize = 50,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool? matched = null,
        string? search = null,
        string sortBy = "date",
        string sortOrder = "desc",
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Getting mixed list for user {UserId}, page {Page}, pageSize {PageSize}",
            userId, page, pageSize);

        // Query ungrouped transactions
        var transactionsQuery = _dbContext.Transactions
            .Where(t => t.UserId == userId && t.GroupId == null);

        // Query groups
        var groupsQuery = _dbContext.TransactionGroups
            .Include(g => g.Transactions)
            .Where(g => g.UserId == userId);

        // Apply date filters
        if (startDate.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate >= startDate.Value);
            groupsQuery = groupsQuery.Where(g => g.DisplayDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            transactionsQuery = transactionsQuery.Where(t => t.TransactionDate <= endDate.Value);
            groupsQuery = groupsQuery.Where(g => g.DisplayDate <= endDate.Value);
        }

        // Apply match status filter
        if (matched.HasValue)
        {
            if (matched.Value)
            {
                transactionsQuery = transactionsQuery.Where(t => t.MatchedReceiptId != null);
                groupsQuery = groupsQuery.Where(g => g.MatchedReceiptId != null);
            }
            else
            {
                transactionsQuery = transactionsQuery.Where(t => t.MatchedReceiptId == null);
                groupsQuery = groupsQuery.Where(g => g.MatchedReceiptId == null);
            }
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            transactionsQuery = transactionsQuery.Where(t =>
                t.Description.ToLower().Contains(searchLower) ||
                t.OriginalDescription.ToLower().Contains(searchLower));
            groupsQuery = groupsQuery.Where(g => g.Name.ToLower().Contains(searchLower));
        }

        // Get counts first
        var transactionCount = await transactionsQuery.CountAsync(ct);
        var groupCount = await groupsQuery.CountAsync(ct);
        var totalCount = transactionCount + groupCount;

        // Fetch both sets - we'll combine and sort in memory for simplicity
        // For very large datasets, a union query with a common sort key would be more efficient
        var transactions = await transactionsQuery.ToListAsync(ct);
        var groups = await groupsQuery.ToListAsync(ct);

        // Create combined list with sort key
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        var combined = new List<(DateOnly Date, decimal Amount, object Item)>();

        foreach (var t in transactions)
        {
            combined.Add((t.TransactionDate, t.Amount, t));
        }
        foreach (var g in groups)
        {
            combined.Add((g.DisplayDate, g.CombinedAmount, g));
        }

        // Sort combined list
        IEnumerable<(DateOnly Date, decimal Amount, object Item)> sorted = sortBy.ToLowerInvariant() switch
        {
            "amount" => isDescending
                ? combined.OrderByDescending(x => x.Amount).ThenByDescending(x => x.Date)
                : combined.OrderBy(x => x.Amount).ThenBy(x => x.Date),
            _ => isDescending
                ? combined.OrderByDescending(x => x.Date).ThenByDescending(x => x.Amount)
                : combined.OrderBy(x => x.Date).ThenBy(x => x.Amount)
        };

        // Apply pagination
        var pagedItems = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        // Batch-fetch predictions for all transactions
        var transactionIds = pagedItems
            .Where(x => x.Item is Transaction)
            .Select(x => ((Transaction)x.Item).Id)
            .ToList();
        var predictions = await _predictionService.GetPredictionsForTransactionsAsync(transactionIds);

        // Map to DTOs
        var transactionDtos = new List<TransactionSummaryDto>();
        var groupDtos = new List<TransactionGroupDetailDto>();

        foreach (var item in pagedItems)
        {
            if (item.Item is Transaction t)
            {
                transactionDtos.Add(new TransactionSummaryDto
                {
                    Id = t.Id,
                    TransactionDate = t.TransactionDate,
                    Description = t.Description,
                    Amount = t.Amount,
                    HasMatchedReceipt = t.MatchedReceiptId.HasValue,
                    Prediction = predictions.TryGetValue(t.Id, out var pred) ? pred : null
                });
            }
            else if (item.Item is TransactionGroup g)
            {
                groupDtos.Add(MapToDetailDto(g, g.Transactions.ToList()));
            }
        }

        return new TransactionMixedListResponse
        {
            Transactions = transactionDtos,
            Groups = groupDtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    #region Private Methods

    /// <summary>
    /// Generates a group name from the transaction descriptions.
    /// Uses the most common vendor/description prefix.
    /// Ensures the result doesn't exceed 100 characters (DTO validation limit).
    /// </summary>
    private static string GenerateGroupName(List<Transaction> transactions)
    {
        const int MaxNameLength = 100;

        // Try to find a common prefix or use the first description
        var descriptions = transactions.Select(t => t.Description).ToList();

        // Find the most common word (likely the vendor name)
        var words = descriptions
            .SelectMany(d => d.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(w => w.Length > 2) // Skip short words
            .GroupBy(w => w.ToUpperInvariant())
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        var vendorName = words?.First() ?? descriptions.FirstOrDefault()?.Split(' ').FirstOrDefault() ?? "Group";
        var count = transactions.Count;
        var suffix = $" ({count} charge{(count > 1 ? "s" : "")})";

        // Ensure total length doesn't exceed limit
        var maxVendorLength = MaxNameLength - suffix.Length;
        if (vendorName.Length > maxVendorLength)
        {
            vendorName = vendorName[..(maxVendorLength - 3)] + "...";
        }

        return $"{vendorName}{suffix}";
    }

    /// <summary>
    /// Recalculates the aggregate fields on a group after membership changes.
    /// </summary>
    private static void RecalculateGroupAggregates(TransactionGroup group)
    {
        var transactions = group.Transactions.ToList();
        group.CombinedAmount = transactions.Sum(t => t.Amount);
        group.TransactionCount = transactions.Count;

        // Only recalculate display date if not manually overridden
        if (!group.IsDateOverridden && transactions.Any())
        {
            group.DisplayDate = transactions.Max(t => t.TransactionDate);
        }
    }

    /// <summary>
    /// Maps a TransactionGroup entity to a detail DTO.
    /// </summary>
    private static TransactionGroupDetailDto MapToDetailDto(
        TransactionGroup group,
        List<Transaction> transactions,
        string? warning = null)
    {
        return new TransactionGroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            DisplayDate = group.DisplayDate,
            IsDateOverridden = group.IsDateOverridden,
            CombinedAmount = group.CombinedAmount,
            TransactionCount = group.TransactionCount,
            MatchStatus = group.MatchStatus,
            MatchedReceiptId = group.MatchedReceiptId,
            CreatedAt = group.CreatedAt,
            Warning = warning,
            Transactions = transactions
                .OrderBy(t => t.TransactionDate)
                .Select(t => new GroupMemberTransactionDto
                {
                    Id = t.Id,
                    TransactionDate = t.TransactionDate,
                    Amount = t.Amount,
                    Description = t.Description
                })
                .ToList()
        };
    }

    #endregion
}
