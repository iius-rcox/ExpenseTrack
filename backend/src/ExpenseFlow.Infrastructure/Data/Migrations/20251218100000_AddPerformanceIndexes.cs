using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Adds performance optimization indexes based on query analysis.
    /// See docs/performance/query-analysis-report.md for details.
    /// </summary>
    public partial class AddPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HIGH-001: DetectedSubscription vendor name search (case-insensitive)
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_detected_subscriptions_vendor_upper
                ON detected_subscriptions (user_id, upper(vendor_name));
            ");

            // HIGH-002: ExpenseReport status filtering with partial index
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_expense_reports_user_status
                ON expense_reports (user_id, status, created_at DESC)
                WHERE NOT is_deleted;
            ");

            // HIGH-003: ExpenseEmbedding user filtering for vector similarity search
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_expense_embeddings_user_id
                ON expense_embeddings (user_id);
            ");

            // HIGH-004: KnownSubscriptionVendors active filter (partial index)
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_known_subscription_vendors_active
                ON known_subscription_vendors (display_name)
                WHERE is_active = true;
            ");

            // HIGH-005: TierUsageLogs tier 3 join optimization (partial index)
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_tier_usage_logs_tier3_join
                ON tier_usage_logs (created_at, tier_used, transaction_id)
                WHERE tier_used = 3 AND transaction_id IS NOT NULL;
            ");

            // Additional index for statement fingerprint header hash lookup
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_statement_fingerprints_header_hash
                ON statement_fingerprints (user_id, header_hash);
            ");

            // Index for transaction date range queries
            migrationBuilder.Sql(@"
                CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_transactions_user_date
                ON transactions (user_id, transaction_date DESC);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_detected_subscriptions_vendor_upper;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_expense_reports_user_status;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_expense_embeddings_user_id;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_known_subscription_vendors_active;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_tier_usage_logs_tier3_join;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_statement_fingerprints_header_hash;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_transactions_user_date;");
        }
    }
}
