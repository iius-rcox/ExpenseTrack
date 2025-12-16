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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseFlowDbContext).Assembly);
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
