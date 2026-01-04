using Microsoft.EntityFrameworkCore;
using ExpenseFlow.Core.Entities;
using ExpenseFlow.Core.Interfaces;

namespace ExpenseFlow.Infrastructure.Data;

/// <summary>
/// Entity Framework Core DbContext for ExpenseFlow application.
/// </summary>
public class ExpenseFlowDbContext : DbContext
{
    public ExpenseFlowDbContext(DbContextOptions<ExpenseFlowDbContext> options)
        : base(options)
    {
    }

    // User Story 1: Authentication
    public DbSet<User> Users => Set<User>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    // User Story 2: Cache Tables
    public DbSet<DescriptionCache> DescriptionCaches => Set<DescriptionCache>();
    public DbSet<VendorAlias> VendorAliases => Set<VendorAlias>();
    public DbSet<StatementFingerprint> StatementFingerprints => Set<StatementFingerprint>();
    public DbSet<SplitPattern> SplitPatterns => Set<SplitPattern>();
    public DbSet<ExpenseEmbedding> ExpenseEmbeddings => Set<ExpenseEmbedding>();

    // User Story 4: Reference Data
    public DbSet<GLAccount> GLAccounts => Set<GLAccount>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Project> Projects => Set<Project>();

    // Sprint 3: Receipt Upload Pipeline
    public DbSet<Receipt> Receipts => Set<Receipt>();

    // Sprint 4: Statement Import & Fingerprinting
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<StatementImport> StatementImports => Set<StatementImport>();

    // Sprint 5: Matching Engine
    public DbSet<ReceiptTransactionMatch> ReceiptTransactionMatches => Set<ReceiptTransactionMatch>();

    // Sprint 6: AI Categorization
    public DbSet<TierUsageLog> TierUsageLogs => Set<TierUsageLog>();

    // Sprint 7: Advanced Features
    public DbSet<TravelPeriod> TravelPeriods => Set<TravelPeriod>();
    public DbSet<DetectedSubscription> DetectedSubscriptions => Set<DetectedSubscription>();
    public DbSet<KnownSubscriptionVendor> KnownSubscriptionVendors => Set<KnownSubscriptionVendor>();

    // Sprint 8: Draft Report Generation
    public DbSet<ExpenseReport> ExpenseReports => Set<ExpenseReport>();
    public DbSet<ExpenseLine> ExpenseLines => Set<ExpenseLine>();

    // Sprint 10: Cache Warming
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();

    // Feature 023: Expense Prediction
    public DbSet<ExpensePattern> ExpensePatterns => Set<ExpensePattern>();
    public DbSet<TransactionPrediction> TransactionPredictions => Set<TransactionPrediction>();
    public DbSet<PredictionFeedback> PredictionFeedback => Set<PredictionFeedback>();

    // Feature 024: Extraction Editor Training
    public DbSet<ExtractionCorrection> ExtractionCorrections => Set<ExtractionCorrection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var isNpgsql = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";

        // Enable pgvector extension only when using Npgsql (not InMemory)
        if (isNpgsql)
        {
            modelBuilder.HasPostgresExtension("vector");
        }

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseFlowDbContext).Assembly);

        // For InMemory provider, ignore properties that require PostgreSQL-specific types
        if (!isNpgsql)
        {
            // Ignore Vector type (pgvector)
            modelBuilder.Entity<ExpenseEmbedding>().Ignore(e => e.Embedding);

            // Ignore complex types that use jsonb column type
            modelBuilder.Entity<Receipt>().Ignore(r => r.ConfidenceScores);
            modelBuilder.Entity<Receipt>().Ignore(r => r.LineItems);
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auto-set CreatedAt for new entities
        var entries = ChangeTracker.Entries<IAuditable>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
