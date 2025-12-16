using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Adds GIN trigram index for vendor alias pattern matching performance.
    /// Requires pg_trgm extension for trigram-based similarity searches.
    /// </summary>
    public partial class AddVendorAliasTrigramIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enable pg_trgm extension for trigram-based pattern matching
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Drop existing B-tree index on alias_pattern (will be replaced with GIN)
            migrationBuilder.DropIndex(
                name: "ix_vendor_aliases_pattern",
                table: "vendor_aliases");

            // Create GIN trigram index for efficient ILIKE pattern matching
            // This dramatically improves queries like: WHERE @description ILIKE '%' || alias_pattern || '%'
            migrationBuilder.Sql(@"
                CREATE INDEX ix_vendor_aliases_pattern_gin
                ON vendor_aliases
                USING GIN (alias_pattern gin_trgm_ops);
            ");

            // Add index on confidence and match_count for ordering performance
            migrationBuilder.CreateIndex(
                name: "ix_vendor_aliases_confidence_matchcount",
                table: "vendor_aliases",
                columns: new[] { "confidence", "match_count" },
                descending: new[] { true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the composite index
            migrationBuilder.DropIndex(
                name: "ix_vendor_aliases_confidence_matchcount",
                table: "vendor_aliases");

            // Drop GIN trigram index
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_vendor_aliases_pattern_gin;");

            // Recreate original B-tree index
            migrationBuilder.CreateIndex(
                name: "ix_vendor_aliases_pattern",
                table: "vendor_aliases",
                column: "alias_pattern");

            // Note: We don't drop pg_trgm as other parts of the system may use it
        }
    }
}
