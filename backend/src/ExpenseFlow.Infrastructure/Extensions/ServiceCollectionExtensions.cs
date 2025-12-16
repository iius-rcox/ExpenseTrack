using ExpenseFlow.Core.Interfaces;
using ExpenseFlow.Core.Services;
using ExpenseFlow.Infrastructure.Jobs;
using ExpenseFlow.Infrastructure.Repositories;
using ExpenseFlow.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
