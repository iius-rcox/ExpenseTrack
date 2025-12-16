using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Core.Services;
using ExpenseFlow.Infrastructure.Jobs;
using ExpenseFlow.Infrastructure.Repositories;
using ExpenseFlow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Polly;

namespace ExpenseFlow.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering ExpenseFlow services in DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all ExpenseFlow application services to the DI container.
    /// </summary>
    public static IServiceCollection AddExpenseFlowServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // User services (US1)
        services.AddScoped<IUserService, UserService>();

        // Cache services (US2)
        services.AddScoped<IDescriptionCacheService, DescriptionCacheService>();
        services.AddScoped<IVendorAliasService, VendorAliasService>();
        services.AddScoped<IStatementFingerprintService, StatementFingerprintService>();
        services.AddScoped<IExpenseEmbeddingService, ExpenseEmbeddingService>();
        services.AddScoped<ICacheStatsService, CacheStatsService>();

        // Background job services (US3)
        services.AddScoped<IBackgroundJobService, BackgroundJobService>();

        // Reference data services (US4)
        services.AddScoped<IReferenceDataService, ReferenceDataService>();
        services.AddScoped<IExternalDataSource, SqlServerDataSource>();

        // Sprint 3: Receipt Upload Pipeline
        services.AddScoped<IReceiptRepository, ReceiptRepository>();
        services.AddScoped<IReceiptService, ReceiptService>();
        services.AddScoped<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IHeicConversionService, HeicConversionService>();
        services.AddScoped<IDocumentIntelligenceService, DocumentIntelligenceService>();
        services.AddScoped<IThumbnailService, ThumbnailService>();
        services.AddScoped<IReceiptProcessingJob, ProcessReceiptJob>();

        // Sprint 4: Statement Import & Fingerprinting
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IStatementImportRepository, StatementImportRepository>();
        services.AddScoped<IStatementParsingService, StatementParsingService>();
        services.AddScoped<IColumnMappingInferenceService, ColumnMappingInferenceService>();
        services.AddSingleton<AnalysisSessionCache>();

        // Sprint 5: Matching Engine
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IMatchingService, MatchingService>();
        services.AddScoped<IFuzzyMatchingService, FuzzyMatchingService>();

        // Sprint 6: AI Categorization
        services.AddScoped<IDescriptionCacheRepository, DescriptionCacheRepository>();
        services.AddScoped<IExpenseEmbeddingRepository, ExpenseEmbeddingRepository>();
        services.AddScoped<ITierUsageRepository, TierUsageRepository>();
        services.AddScoped<ITierUsageService, TierUsageService>();
        services.AddScoped<IDescriptionNormalizationService, DescriptionNormalizationService>();
        services.AddScoped<IEmbeddingService, EmbeddingService>();
        services.AddScoped<ICategorizationService, CategorizationService>();

        // Sprint 6: Configure Azure OpenAI for Semantic Kernel
        var azureOpenAIEndpoint = configuration["AzureOpenAI:Endpoint"];
        var azureOpenAIApiKey = configuration["AzureOpenAI:ApiKey"];
        var embeddingDeployment = configuration["AzureOpenAI:EmbeddingDeployment"];
        var chatDeployment = configuration["AzureOpenAI:ChatDeployment"];

        if (!string.IsNullOrEmpty(azureOpenAIEndpoint) && !string.IsNullOrEmpty(azureOpenAIApiKey))
        {
            // Register embedding generation service
            services.AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: embeddingDeployment ?? "text-embedding-3-small",
                endpoint: azureOpenAIEndpoint,
                apiKey: azureOpenAIApiKey);

            // Register chat completion service for Tier 3 inference
            services.AddAzureOpenAIChatCompletion(
                deploymentName: chatDeployment ?? "gpt-4o-mini",
                endpoint: azureOpenAIEndpoint,
                apiKey: azureOpenAIApiKey);
        }

        // Sprint 6: Configure Polly resilience pipeline for AI calls
        services.AddResiliencePipeline("ai-calls", builder =>
        {
            builder
                .AddRetry(new Polly.Retry.RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(1),
                    BackoffType = Polly.DelayBackoffType.Exponential,
                    ShouldHandle = new Polly.PredicateBuilder().Handle<HttpRequestException>()
                })
                .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
                {
                    FailureRatio = 0.5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromSeconds(60),
                    MinimumThroughput = 5
                });
        });

        // Sprint 7: Advanced Features (Repositories)
        services.AddScoped<ITravelPeriodRepository, TravelPeriodRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<ISplitPatternRepository, SplitPatternRepository>();

        // Sprint 7: Advanced Features (Services)
        services.AddScoped<ITravelDetectionService, TravelDetectionService>();
        services.AddScoped<ISubscriptionDetectionService, SubscriptionDetectionService>();
        services.AddScoped<IExpenseSplittingService, ExpenseSplittingService>();

        return services;
    }
}
