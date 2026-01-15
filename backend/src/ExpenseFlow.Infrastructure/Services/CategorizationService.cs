using System.Diagnostics;
using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ExpenseFlow.Infrastructure.Services;

/// <summary>
/// Service for tiered expense categorization (GL code and department suggestions).
/// Tier 1: Vendor alias default → Tier 2: Embedding similarity → Tier 3: AI inference.
/// </summary>
public class CategorizationService : ICategorizationService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IVendorAliasService _vendorAliasService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IDescriptionNormalizationService _normalizationService;
    private readonly ITierUsageService _tierUsageService;
    private readonly IChatCompletionService? _chatCompletionService;
    private readonly IReferenceDataService _referenceDataService;
    private readonly ILogger<CategorizationService> _logger;
    private readonly float _similarityThreshold;
    private readonly int _vendorConfirmThreshold;

    public CategorizationService(
        ITransactionRepository transactionRepository,
        IVendorAliasService vendorAliasService,
        IEmbeddingService embeddingService,
        IDescriptionNormalizationService normalizationService,
        ITierUsageService tierUsageService,
        IChatCompletionService? chatCompletionService,
        IReferenceDataService referenceDataService,
        IConfiguration configuration,
        ILogger<CategorizationService> logger)
    {
        _transactionRepository = transactionRepository;
        _vendorAliasService = vendorAliasService;
        _embeddingService = embeddingService;
        _normalizationService = normalizationService;
        _tierUsageService = tierUsageService;
        _chatCompletionService = chatCompletionService;
        _referenceDataService = referenceDataService;
        _logger = logger;
        _similarityThreshold = configuration.GetValue("Categorization:EmbeddingSimilarityThreshold", 0.92f);
        _vendorConfirmThreshold = configuration.GetValue("Categorization:VendorAliasConfirmThreshold", 3);
    }

    public async Task<GLSuggestionsDto> GetGLSuggestionsAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var transaction = await _transactionRepository.GetByIdAsync(userId, transactionId);
        if (transaction == null)
        {
            return new GLSuggestionsDto
            {
                TransactionId = transactionId,
                Message = "Transaction not found"
            };
        }

        var suggestions = new List<CategorizationSuggestionDto>();
        var tierUsed = 0;

        // Normalize description first
        var normalizedResult = await _normalizationService.NormalizeAsync(
            transaction.Description,
            userId,
            cancellationToken);

        // Tier 1: Vendor alias lookup
        var vendorAlias = await _vendorAliasService.GetByVendorNameAsync(transaction.Description);
        if (vendorAlias?.DefaultGLCode != null)
        {
            tierUsed = 1;
            var glAccount = await _referenceDataService.GetGLAccountByCodeAsync(vendorAlias.DefaultGLCode);
            suggestions.Add(new CategorizationSuggestionDto
            {
                Code = vendorAlias.DefaultGLCode,
                Name = glAccount?.Name ?? vendorAlias.DefaultGLCode,
                Confidence = 0.95m,
                Tier = 1,
                Source = "vendor_alias",
                Explanation = $"Based on vendor alias '{vendorAlias.CanonicalName}'"
            });
        }

        // Tier 2: Embedding similarity (if Tier 1 didn't find a match)
        if (!suggestions.Any())
        {
            try
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
                    normalizedResult.NormalizedDescription,
                    cancellationToken);

                var similarEmbeddings = await _embeddingService.FindSimilarAsync(
                    queryEmbedding,
                    userId,
                    limit: 3,
                    threshold: _similarityThreshold,
                    cancellationToken);

                foreach (var similar in similarEmbeddings.Where(e => !string.IsNullOrEmpty(e.GLCode)))
                {
                    tierUsed = 2;
                    var glAccount = await _referenceDataService.GetGLAccountByCodeAsync(similar.GLCode!);
                    suggestions.Add(new CategorizationSuggestionDto
                    {
                        Code = similar.GLCode!,
                        Name = glAccount?.Name ?? similar.GLCode!,
                        Confidence = similar.Verified ? 0.90m : 0.80m,
                        Tier = 2,
                        Source = "embedding_similarity",
                        Explanation = similar.Verified
                            ? "Based on verified similar transaction"
                            : "Based on similar transaction pattern"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tier 2 embedding search failed, falling back to Tier 3");
            }
        }

        // Tier 3: AI inference (if no suggestions yet)
        if (!suggestions.Any() && _chatCompletionService != null)
        {
            try
            {
                tierUsed = 3;
                var aiSuggestion = await GetGLSuggestionFromAIAsync(
                    normalizedResult.NormalizedDescription,
                    cancellationToken);

                if (aiSuggestion != null)
                {
                    suggestions.Add(aiSuggestion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tier 3 AI inference failed for GL suggestion");
            }
        }

        stopwatch.Stop();

        if (tierUsed > 0)
        {
            await _tierUsageService.LogUsageAsync(
                userId,
                "gl_suggestion",
                tierUsed,
                suggestions.FirstOrDefault()?.Confidence,
                (int)stopwatch.ElapsedMilliseconds,
                cacheHit: tierUsed == 1,
                transactionId,
                cancellationToken);
        }

        // Structured logging for categorization operation
        _logger.LogInformation(
            "GL suggestion completed for transaction {TransactionId}: " +
            "Tier={TierUsed}, Confidence={Confidence:F2}, SuggestionCount={SuggestionCount}, Duration={DurationMs}ms",
            transactionId,
            tierUsed,
            suggestions.FirstOrDefault()?.Confidence ?? 0,
            suggestions.Count,
            stopwatch.ElapsedMilliseconds);

        return new GLSuggestionsDto
        {
            TransactionId = transactionId,
            Suggestions = suggestions,
            TopSuggestion = suggestions.FirstOrDefault(),
            ServiceStatus = _chatCompletionService != null ? "healthy" : "degraded"
        };
    }

    public async Task<DepartmentSuggestionsDto> GetDepartmentSuggestionsAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        var transaction = await _transactionRepository.GetByIdAsync(userId, transactionId);
        if (transaction == null)
        {
            return new DepartmentSuggestionsDto
            {
                TransactionId = transactionId
            };
        }

        var suggestions = new List<CategorizationSuggestionDto>();
        var tierUsed = 0;

        // Normalize description first
        var normalizedResult = await _normalizationService.NormalizeAsync(
            transaction.Description,
            userId,
            cancellationToken);

        // Tier 1: Vendor alias lookup
        var vendorAlias = await _vendorAliasService.GetByVendorNameAsync(transaction.Description);
        if (vendorAlias?.DefaultDepartment != null)
        {
            tierUsed = 1;
            var department = await _referenceDataService.GetDepartmentByCodeAsync(vendorAlias.DefaultDepartment);
            suggestions.Add(new CategorizationSuggestionDto
            {
                Code = vendorAlias.DefaultDepartment,
                Name = department?.Name ?? vendorAlias.DefaultDepartment,
                Confidence = 0.95m,
                Tier = 1,
                Source = "vendor_alias",
                Explanation = $"Based on vendor alias '{vendorAlias.CanonicalName}'"
            });
        }

        // Tier 2: Embedding similarity
        if (!suggestions.Any())
        {
            try
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(
                    normalizedResult.NormalizedDescription,
                    cancellationToken);

                var similarEmbeddings = await _embeddingService.FindSimilarAsync(
                    queryEmbedding,
                    userId,
                    limit: 3,
                    threshold: _similarityThreshold,
                    cancellationToken);

                foreach (var similar in similarEmbeddings.Where(e => !string.IsNullOrEmpty(e.Department)))
                {
                    tierUsed = 2;
                    var department = await _referenceDataService.GetDepartmentByCodeAsync(similar.Department!);
                    suggestions.Add(new CategorizationSuggestionDto
                    {
                        Code = similar.Department!,
                        Name = department?.Name ?? similar.Department!,
                        Confidence = similar.Verified ? 0.90m : 0.80m,
                        Tier = 2,
                        Source = "embedding_similarity",
                        Explanation = similar.Verified
                            ? "Based on verified similar transaction"
                            : "Based on similar transaction pattern"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tier 2 embedding search failed for department");
            }
        }

        // Tier 3: AI inference
        if (!suggestions.Any() && _chatCompletionService != null)
        {
            try
            {
                tierUsed = 3;
                var aiSuggestion = await GetDepartmentSuggestionFromAIAsync(
                    normalizedResult.NormalizedDescription,
                    cancellationToken);

                if (aiSuggestion != null)
                {
                    suggestions.Add(aiSuggestion);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tier 3 AI inference failed for department suggestion");
            }
        }

        stopwatch.Stop();

        if (tierUsed > 0)
        {
            await _tierUsageService.LogUsageAsync(
                userId,
                "dept_suggestion",
                tierUsed,
                suggestions.FirstOrDefault()?.Confidence,
                (int)stopwatch.ElapsedMilliseconds,
                cacheHit: tierUsed == 1,
                transactionId,
                cancellationToken);
        }

        // Structured logging for department suggestion operation
        _logger.LogInformation(
            "Department suggestion completed for transaction {TransactionId}: " +
            "Tier={TierUsed}, Confidence={Confidence:F2}, SuggestionCount={SuggestionCount}, Duration={DurationMs}ms",
            transactionId,
            tierUsed,
            suggestions.FirstOrDefault()?.Confidence ?? 0,
            suggestions.Count,
            stopwatch.ElapsedMilliseconds);

        return new DepartmentSuggestionsDto
        {
            TransactionId = transactionId,
            Suggestions = suggestions,
            TopSuggestion = suggestions.FirstOrDefault()
        };
    }

    public async Task<TransactionCategorizationDto> GetCategorizationAsync(
        Guid transactionId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(userId, transactionId);
        if (transaction == null)
        {
            return new TransactionCategorizationDto
            {
                TransactionId = transactionId
            };
        }

        // Get normalized description
        var normalizedResult = await _normalizationService.NormalizeAsync(
            transaction.Description,
            userId,
            cancellationToken);

        // Extract vendor name using alias matching
        var vendorAlias = await _vendorAliasService.FindMatchingAliasAsync(transaction.Description);
        string extractedVendor;

        if (vendorAlias != null)
        {
            extractedVendor = vendorAlias.DisplayName;
            await _vendorAliasService.RecordMatchAsync(vendorAlias.Id);
            _logger.LogDebug("Extracted vendor {Vendor} from description {Description}",
                extractedVendor, transaction.Description);
        }
        else
        {
            extractedVendor = transaction.Description;
            _logger.LogDebug("No vendor alias match for description {Description}",
                transaction.Description);
        }

        // Get GL and Department suggestions sequentially (DbContext is not thread-safe)
        var glSuggestions = await GetGLSuggestionsAsync(transactionId, userId, cancellationToken);
        var deptSuggestions = await GetDepartmentSuggestionsAsync(transactionId, userId, cancellationToken);

        return new TransactionCategorizationDto
        {
            TransactionId = transactionId,
            NormalizedDescription = normalizedResult.NormalizedDescription,
            Vendor = extractedVendor,
            GL = new GLCategorizationSection
            {
                TopSuggestion = glSuggestions.TopSuggestion,
                Alternatives = glSuggestions.Suggestions.Skip(1).ToList()
            },
            Department = new DepartmentCategorizationSection
            {
                TopSuggestion = deptSuggestions.TopSuggestion,
                Alternatives = deptSuggestions.Suggestions.Skip(1).ToList()
            }
        };
    }

    public async Task<CategorizationConfirmationDto> ConfirmCategorizationAsync(
        Guid transactionId,
        Guid userId,
        string glCode,
        string departmentCode,
        bool acceptedSuggestion,
        CancellationToken cancellationToken = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(userId, transactionId);
        if (transaction == null)
        {
            return new CategorizationConfirmationDto
            {
                TransactionId = transactionId,
                Message = "Transaction not found"
            };
        }

        // Normalize description
        var normalizedResult = await _normalizationService.NormalizeAsync(
            transaction.Description,
            userId,
            cancellationToken);

        // Create verified embedding
        var embeddingCreated = false;
        try
        {
            await _embeddingService.CreateVerifiedEmbeddingAsync(
                normalizedResult.NormalizedDescription,
                glCode,
                departmentCode,
                userId,
                transactionId,
                cancellationToken: cancellationToken);
            embeddingCreated = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create verified embedding for transaction {TransactionId}", transactionId);
        }

        // Update vendor alias confirmation counts
        var vendorAliasUpdated = false;
        string? vendorAliasMessage = null;

        var vendorAlias = await _vendorAliasService.GetByVendorNameAsync(transaction.Description);
        if (vendorAlias != null)
        {
            // Increment GL confirmation count
            if (vendorAlias.DefaultGLCode == glCode)
            {
                vendorAlias.GLConfirmCount++;
            }

            // Increment department confirmation count
            if (vendorAlias.DefaultDepartment == departmentCode)
            {
                vendorAlias.DeptConfirmCount++;
            }

            // Update defaults if threshold reached
            if (vendorAlias.GLConfirmCount >= _vendorConfirmThreshold && vendorAlias.DefaultGLCode != glCode)
            {
                vendorAlias.DefaultGLCode = glCode;
                vendorAlias.GLConfirmCount = _vendorConfirmThreshold;
                vendorAliasUpdated = true;
                vendorAliasMessage = $"Vendor alias GL default updated to {glCode}";
            }

            if (vendorAlias.DeptConfirmCount >= _vendorConfirmThreshold && vendorAlias.DefaultDepartment != departmentCode)
            {
                vendorAlias.DefaultDepartment = departmentCode;
                vendorAlias.DeptConfirmCount = _vendorConfirmThreshold;
                vendorAliasUpdated = true;
                vendorAliasMessage = (vendorAliasMessage != null ? vendorAliasMessage + " and " : "") +
                                     $"department default updated to {departmentCode}";
            }

            await _vendorAliasService.UpdateAsync(vendorAlias);
        }

        _logger.LogInformation(
            "Confirmed categorization for transaction {TransactionId}: GL={GLCode}, Dept={Dept}, EmbeddingCreated={Created}",
            transactionId, glCode, departmentCode, embeddingCreated);

        return new CategorizationConfirmationDto
        {
            TransactionId = transactionId,
            GLCode = glCode,
            DepartmentCode = departmentCode,
            EmbeddingCreated = embeddingCreated,
            VendorAliasUpdated = vendorAliasUpdated,
            VendorAliasMessage = vendorAliasMessage,
            Message = "Categorization confirmed successfully"
        };
    }

    public Task<CategorizationSkipDto> SkipSuggestionAsync(
        Guid transactionId,
        Guid userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "User {UserId} skipped suggestion for transaction {TransactionId}: {Reason}",
            userId, transactionId, reason);

        return Task.FromResult(new CategorizationSkipDto
        {
            TransactionId = transactionId,
            Skipped = true,
            Message = "Suggestion skipped - manual categorization required"
        });
    }

    private async Task<CategorizationSuggestionDto?> GetGLSuggestionFromAIAsync(
        string description,
        CancellationToken cancellationToken)
    {
        // Get available GL accounts for context (FILTER TO EXPENSE ACCOUNTS ONLY)
        var allGLAccounts = await _referenceDataService.GetGLAccountsAsync();

        // Only include expense accounts (50000-69999 range) - excludes assets, liabilities, equity
        var expenseGLAccounts = allGLAccounts
            .Where(g => g.Code.StartsWith("5") || g.Code.StartsWith("6"))
            .OrderBy(g => g.Code)
            .ToList();

        var glContext = string.Join("\n", expenseGLAccounts.Take(100).Select(g => $"- {g.Code}: {g.Name}"));

        var prompt = $"""
            You are a financial categorization assistant. Given a transaction description, suggest the most appropriate GL (General Ledger) code.

            Available GL Codes:
            {glContext}

            Transaction: {description}

            Respond with ONLY the GL code (e.g., "6000") and nothing else.
            """;

        var response = await _chatCompletionService!.GetChatMessageContentAsync(
            prompt,
            cancellationToken: cancellationToken);

        var suggestedCode = response.Content?.Trim();
        if (string.IsNullOrEmpty(suggestedCode))
        {
            return null;
        }

        var glAccount = await _referenceDataService.GetGLAccountByCodeAsync(suggestedCode);

        return new CategorizationSuggestionDto
        {
            Code = suggestedCode,
            Name = glAccount?.Name ?? suggestedCode,
            Confidence = 0.70m, // Lower confidence for AI inference
            Tier = 3,
            Source = "ai_inference",
            Explanation = "Suggested by AI based on transaction description"
        };
    }

    private async Task<CategorizationSuggestionDto?> GetDepartmentSuggestionFromAIAsync(
        string description,
        CancellationToken cancellationToken)
    {
        var departments = await _referenceDataService.GetDepartmentsAsync();
        var deptContext = string.Join("\n", departments.Take(30).Select(d => $"- {d.Code}: {d.Name}"));

        var prompt = $"""
            You are a financial categorization assistant. Given a transaction description, suggest the most appropriate department code.

            Available Departments:
            {deptContext}

            Transaction: {description}

            Respond with ONLY the department code (e.g., "ADMIN") and nothing else.
            """;

        var response = await _chatCompletionService!.GetChatMessageContentAsync(
            prompt,
            cancellationToken: cancellationToken);

        var suggestedCode = response.Content?.Trim();
        if (string.IsNullOrEmpty(suggestedCode))
        {
            return null;
        }

        var department = await _referenceDataService.GetDepartmentByCodeAsync(suggestedCode);

        return new CategorizationSuggestionDto
        {
            Code = suggestedCode,
            Name = department?.Name ?? suggestedCode,
            Confidence = 0.70m,
            Tier = 3,
            Source = "ai_inference",
            Explanation = "Suggested by AI based on transaction description"
        };
    }
}
