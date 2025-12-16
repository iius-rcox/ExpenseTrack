using System.Text.Json;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Infrastructure.Data;
using ExpenseFlow.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for expense splitting functionality.
/// Uses Tier 1 (rule-based) pattern matching to suggest splits based on vendor patterns.
/// </summary>
public class ExpenseSplittingService : IExpenseSplittingService
{
    private readonly ExpenseFlowDbContext _dbContext;
    private readonly ISplitPatternRepository _patternRepository;
    private readonly IVendorAliasService _vendorAliasService;
    private readonly ILogger<ExpenseSplittingService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExpenseSplittingService(
        ExpenseFlowDbContext dbContext,
        ISplitPatternRepository patternRepository,
        IVendorAliasService vendorAliasService,
        ILogger<ExpenseSplittingService> logger)
    {
        _dbContext = dbContext;
        _patternRepository = patternRepository;
        _vendorAliasService = vendorAliasService;
        _logger = logger;
    }

    #region Split Operations

    public async Task<ExpenseSplitStatusDto?> GetSplitStatusAsync(Guid userId, Guid expenseId)
    {
        // Try to get as transaction first
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == expenseId && t.UserId == userId);

        if (transaction == null)
        {
            // Try as receipt
            var receipt = await _dbContext.Receipts
                .FirstOrDefaultAsync(r => r.Id == expenseId && r.UserId == userId);

            if (receipt == null)
                return null;

            return await BuildSplitStatusFromReceiptAsync(userId, receipt);
        }

        return await BuildSplitStatusFromTransactionAsync(userId, transaction);
    }

    public async Task<ApplySplitResultDto> ApplySplitAsync(
        Guid userId,
        Guid expenseId,
        ApplySplitRequestDto request)
    {
        // Validate allocations
        if (!ValidateAllocations(request.Allocations))
        {
            return new ApplySplitResultDto
            {
                Success = false,
                ExpenseId = expenseId,
                Message = "Allocations must sum to exactly 100%"
            };
        }

        // Verify expense exists
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == expenseId && t.UserId == userId);

        var receipt = transaction == null
            ? await _dbContext.Receipts.FirstOrDefaultAsync(r => r.Id == expenseId && r.UserId == userId)
            : null;

        if (transaction == null && receipt == null)
        {
            return new ApplySplitResultDto
            {
                Success = false,
                ExpenseId = expenseId,
                Message = "Expense not found"
            };
        }

        var totalAmount = transaction?.Amount ?? receipt?.AmountExtracted ?? 0;

        // Calculate actual amounts for each allocation
        foreach (var allocation in request.Allocations)
        {
            allocation.Amount = Math.Round(totalAmount * allocation.Percentage / 100, 2);
        }

        // Adjust for rounding - ensure amounts sum to total
        AdjustForRounding(request.Allocations, totalAmount);

        Guid? patternId = null;

        // Save as pattern if requested
        if (request.SaveAsPattern && !string.IsNullOrWhiteSpace(request.PatternName))
        {
            var vendorAliasId = await GetVendorAliasIdForExpenseAsync(userId, transaction, receipt);

            var pattern = new SplitPattern
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = request.PatternName,
                VendorAliasId = vendorAliasId,
                SplitConfig = JsonSerializer.Serialize(request.Allocations, JsonOptions),
                UsageCount = 1,
                LastUsedAt = DateTime.UtcNow,
                IsDefault = false
            };

            await _patternRepository.AddAsync(pattern);
            await _patternRepository.SaveChangesAsync();

            patternId = pattern.Id;

            _logger.LogInformation(
                "Created split pattern {PatternId} for user {UserId} from expense {ExpenseId}",
                patternId, userId, expenseId);
        }

        _logger.LogInformation(
            "Applied split to expense {ExpenseId} for user {UserId} with {AllocationCount} allocations",
            expenseId, userId, request.Allocations.Count);

        return new ApplySplitResultDto
        {
            Success = true,
            ExpenseId = expenseId,
            SplitLineIds = request.Allocations.Select(_ => Guid.NewGuid()).ToList(),
            PatternId = patternId,
            Message = $"Split applied with {request.Allocations.Count} allocations"
        };
    }

    public async Task<bool> RemoveSplitAsync(Guid userId, Guid expenseId)
    {
        // Verify expense exists
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == expenseId && t.UserId == userId);

        if (transaction == null)
        {
            var receipt = await _dbContext.Receipts
                .FirstOrDefaultAsync(r => r.Id == expenseId && r.UserId == userId);

            if (receipt == null)
                return false;
        }

        _logger.LogInformation(
            "Removed split from expense {ExpenseId} for user {UserId}",
            expenseId, userId);

        return true;
    }

    #endregion

    #region Split Pattern Management

    public async Task<SplitPatternListResponseDto> GetPatternsAsync(
        Guid userId,
        int page,
        int pageSize,
        Guid? vendorAliasId = null)
    {
        if (vendorAliasId.HasValue)
        {
            var vendorPatterns = await _patternRepository.GetByVendorAliasAsync(userId, vendorAliasId.Value);

            return new SplitPatternListResponseDto
            {
                Patterns = vendorPatterns.Select(MapToSummaryDto).ToList(),
                TotalCount = vendorPatterns.Count,
                Page = 1,
                PageSize = vendorPatterns.Count
            };
        }

        var (patterns, totalCount) = await _patternRepository.GetPagedAsync(userId, page, pageSize);

        return new SplitPatternListResponseDto
        {
            Patterns = patterns.Select(MapToSummaryDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SplitPatternDetailDto?> GetPatternAsync(Guid userId, Guid patternId)
    {
        var pattern = await _patternRepository.GetByIdAsync(userId, patternId);
        return pattern != null ? MapToDetailDto(pattern) : null;
    }

    public async Task<SplitPatternDetailDto> CreatePatternAsync(
        Guid userId,
        CreateSplitPatternRequestDto request)
    {
        if (!ValidateAllocations(request.Allocations))
        {
            throw new InvalidOperationException("Allocations must sum to exactly 100%");
        }

        // If this is set as default, clear other defaults for the vendor
        if (request.IsDefault && request.VendorAliasId.HasValue)
        {
            await ClearDefaultsForVendorAsync(userId, request.VendorAliasId.Value);
        }

        var pattern = new SplitPattern
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name,
            VendorAliasId = request.VendorAliasId,
            SplitConfig = JsonSerializer.Serialize(request.Allocations, JsonOptions),
            UsageCount = 0,
            LastUsedAt = null,
            IsDefault = request.IsDefault
        };

        await _patternRepository.AddAsync(pattern);
        await _patternRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Created split pattern {PatternId} '{PatternName}' for user {UserId}",
            pattern.Id, pattern.Name, userId);

        // Reload with navigation property
        var created = await _patternRepository.GetByIdAsync(userId, pattern.Id);
        return MapToDetailDto(created!);
    }

    public async Task<SplitPatternDetailDto?> UpdatePatternAsync(
        Guid userId,
        Guid patternId,
        UpdateSplitPatternRequestDto request)
    {
        if (!ValidateAllocations(request.Allocations))
        {
            throw new InvalidOperationException("Allocations must sum to exactly 100%");
        }

        var pattern = await _patternRepository.GetByIdAsync(userId, patternId);
        if (pattern == null)
            return null;

        // If this is being set as default, clear other defaults for the vendor
        if (request.IsDefault && pattern.VendorAliasId.HasValue && !pattern.IsDefault)
        {
            await ClearDefaultsForVendorAsync(userId, pattern.VendorAliasId.Value);
        }

        pattern.Name = request.Name;
        pattern.SplitConfig = JsonSerializer.Serialize(request.Allocations, JsonOptions);
        pattern.IsDefault = request.IsDefault;

        await _patternRepository.UpdateAsync(pattern);
        await _patternRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Updated split pattern {PatternId} for user {UserId}",
            patternId, userId);

        return MapToDetailDto(pattern);
    }

    public async Task<bool> DeletePatternAsync(Guid userId, Guid patternId)
    {
        var pattern = await _patternRepository.GetByIdAsync(userId, patternId);
        if (pattern == null)
            return false;

        await _patternRepository.DeleteAsync(pattern);
        await _patternRepository.SaveChangesAsync();

        _logger.LogInformation(
            "Deleted split pattern {PatternId} for user {UserId}",
            patternId, userId);

        return true;
    }

    #endregion

    #region Suggestions

    public async Task<SplitSuggestionDto?> GetSuggestionAsync(Guid userId, Guid expenseId)
    {
        // Get the expense
        var transaction = await _dbContext.Transactions
            .FirstOrDefaultAsync(t => t.Id == expenseId && t.UserId == userId);

        var receipt = transaction == null
            ? await _dbContext.Receipts.FirstOrDefaultAsync(r => r.Id == expenseId && r.UserId == userId)
            : null;

        if (transaction == null && receipt == null)
            return null;

        // Get vendor alias for the expense
        var vendorAliasId = await GetVendorAliasIdForExpenseAsync(userId, transaction, receipt);

        if (!vendorAliasId.HasValue)
        {
            return new SplitSuggestionDto
            {
                HasSuggestion = false,
                Source = SplitSuggestionSource.None,
                Reason = "No vendor pattern found for this expense"
            };
        }

        // Check for default pattern first (Tier 1)
        var defaultPattern = await _patternRepository.GetDefaultByVendorAsync(userId, vendorAliasId.Value);
        if (defaultPattern != null)
        {
            return BuildSuggestionFromPattern(defaultPattern, SplitSuggestionSource.VendorPattern,
                $"Default split pattern for {defaultPattern.VendorAlias?.DisplayName ?? "this vendor"}");
        }

        // Fall back to most recently used pattern
        var recentPattern = await _patternRepository.GetMostRecentByVendorAsync(userId, vendorAliasId.Value);
        if (recentPattern != null)
        {
            return BuildSuggestionFromPattern(recentPattern, SplitSuggestionSource.RecentUsage,
                $"Based on {recentPattern.UsageCount} previous uses");
        }

        return new SplitSuggestionDto
        {
            HasSuggestion = false,
            Source = SplitSuggestionSource.None,
            Reason = "No split patterns found for this vendor"
        };
    }

    public async Task<SplitPatternDetailDto?> GetDefaultPatternForVendorAsync(Guid userId, Guid vendorAliasId)
    {
        var pattern = await _patternRepository.GetDefaultByVendorAsync(userId, vendorAliasId);
        return pattern != null ? MapToDetailDto(pattern) : null;
    }

    #endregion

    #region Validation

    public bool ValidateAllocations(IEnumerable<SplitAllocationDto> allocations)
    {
        var list = allocations.ToList();

        if (list.Count < 2)
            return false;

        var sum = list.Sum(a => a.Percentage);

        // Allow for floating point tolerance
        return Math.Abs(sum - 100m) < 0.01m;
    }

    #endregion

    #region Private Methods

    private async Task<ExpenseSplitStatusDto> BuildSplitStatusFromTransactionAsync(
        Guid userId,
        Transaction transaction)
    {
        var suggestion = await GetSuggestionAsync(userId, transaction.Id);

        return new ExpenseSplitStatusDto
        {
            ExpenseId = transaction.Id,
            IsSplit = false, // Would check actual split records if they existed
            CurrentAllocations = new List<SplitAllocationDto>(),
            TotalAmount = transaction.Amount,
            Suggestion = suggestion
        };
    }

    private async Task<ExpenseSplitStatusDto> BuildSplitStatusFromReceiptAsync(
        Guid userId,
        Receipt receipt)
    {
        var suggestion = await GetSuggestionAsync(userId, receipt.Id);

        return new ExpenseSplitStatusDto
        {
            ExpenseId = receipt.Id,
            IsSplit = false,
            CurrentAllocations = new List<SplitAllocationDto>(),
            TotalAmount = receipt.AmountExtracted ?? 0,
            Suggestion = suggestion
        };
    }

    private async Task<Guid?> GetVendorAliasIdForExpenseAsync(
        Guid userId,
        Transaction? transaction,
        Receipt? receipt)
    {
        var description = transaction?.Description ?? receipt?.VendorExtracted ?? string.Empty;

        if (string.IsNullOrWhiteSpace(description))
            return null;

        var vendorAlias = await _vendorAliasService.FindMatchingAliasAsync(description);

        return vendorAlias?.Id;
    }

    private SplitSuggestionDto BuildSuggestionFromPattern(
        SplitPattern pattern,
        SplitSuggestionSource source,
        string reason)
    {
        var allocations = DeserializeAllocations(pattern.SplitConfig);

        return new SplitSuggestionDto
        {
            HasSuggestion = true,
            Source = source,
            PatternId = pattern.Id,
            PatternName = pattern.Name,
            SuggestedAllocations = allocations,
            Confidence = CalculateConfidence(pattern),
            PatternUsageCount = pattern.UsageCount,
            Reason = reason
        };
    }

    private static decimal CalculateConfidence(SplitPattern pattern)
    {
        // Base confidence
        var confidence = 0.5m;

        // Increase confidence based on usage
        if (pattern.UsageCount >= 10) confidence += 0.3m;
        else if (pattern.UsageCount >= 5) confidence += 0.2m;
        else if (pattern.UsageCount >= 2) confidence += 0.1m;

        // Increase for default patterns
        if (pattern.IsDefault) confidence += 0.1m;

        // Increase for recent usage
        if (pattern.LastUsedAt.HasValue)
        {
            var daysSinceUsed = (DateTime.UtcNow - pattern.LastUsedAt.Value).TotalDays;
            if (daysSinceUsed < 7) confidence += 0.1m;
            else if (daysSinceUsed < 30) confidence += 0.05m;
        }

        return Math.Min(confidence, 1.0m);
    }

    private static List<SplitAllocationDto> DeserializeAllocations(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<SplitAllocationDto>>(json, JsonOptions)
                   ?? new List<SplitAllocationDto>();
        }
        catch
        {
            return new List<SplitAllocationDto>();
        }
    }

    private static void AdjustForRounding(List<SplitAllocationDto> allocations, decimal totalAmount)
    {
        var calculatedSum = allocations.Sum(a => a.Amount);
        var diff = totalAmount - calculatedSum;

        if (diff != 0 && allocations.Count > 0)
        {
            // Add/subtract the difference from the largest allocation
            var largest = allocations.OrderByDescending(a => a.Amount).First();
            largest.Amount += diff;
        }
    }

    private async Task ClearDefaultsForVendorAsync(Guid userId, Guid vendorAliasId)
    {
        var patterns = await _patternRepository.GetByVendorAliasAsync(userId, vendorAliasId);

        foreach (var pattern in patterns.Where(p => p.IsDefault))
        {
            pattern.IsDefault = false;
            await _patternRepository.UpdateAsync(pattern);
        }

        await _patternRepository.SaveChangesAsync();
    }

    private SplitPatternSummaryDto MapToSummaryDto(SplitPattern pattern)
    {
        var allocations = DeserializeAllocations(pattern.SplitConfig);

        return new SplitPatternSummaryDto
        {
            Id = pattern.Id,
            Name = pattern.Name,
            VendorAliasId = pattern.VendorAliasId,
            VendorName = pattern.VendorAlias?.DisplayName,
            AllocationCount = allocations.Count,
            UsageCount = pattern.UsageCount,
            LastUsedAt = pattern.LastUsedAt,
            IsDefault = pattern.IsDefault
        };
    }

    private SplitPatternDetailDto MapToDetailDto(SplitPattern pattern)
    {
        return new SplitPatternDetailDto
        {
            Id = pattern.Id,
            Name = pattern.Name,
            UserId = pattern.UserId,
            VendorAliasId = pattern.VendorAliasId,
            VendorName = pattern.VendorAlias?.DisplayName,
            Allocations = DeserializeAllocations(pattern.SplitConfig),
            UsageCount = pattern.UsageCount,
            LastUsedAt = pattern.LastUsedAt,
            IsDefault = pattern.IsDefault,
            CreatedAt = pattern.CreatedAt,
            UpdatedAt = null
        };
    }

    #endregion
}
