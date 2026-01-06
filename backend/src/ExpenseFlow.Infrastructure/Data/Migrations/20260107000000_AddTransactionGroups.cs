using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create transaction_groups table
            migrationBuilder.CreateTable(
                name: "transaction_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_date_overridden = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    combined_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false),
                    matched_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    match_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_groups", x => x.id);
                    table.ForeignKey(
                        name: "FK_transaction_groups_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transaction_groups_receipts_matched_receipt_id",
                        column: x => x.matched_receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Add group_id FK column to transactions table
            migrationBuilder.AddColumn<Guid>(
                name: "group_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            // Create indexes for transaction_groups
            migrationBuilder.CreateIndex(
                name: "ix_transaction_groups_user_id",
                table: "transaction_groups",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_groups_user_match_status",
                table: "transaction_groups",
                columns: new[] { "user_id", "match_status" });

            migrationBuilder.CreateIndex(
                name: "ix_transaction_groups_user_date",
                table: "transaction_groups",
                columns: new[] { "user_id", "display_date" });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_groups_matched_receipt_id",
                table: "transaction_groups",
                column: "matched_receipt_id");

            // Create index for group_id on transactions
            migrationBuilder.CreateIndex(
                name: "ix_transactions_group_id",
                table: "transactions",
                column: "group_id");

            // Create FK constraint from transactions.group_id to transaction_groups.id
            migrationBuilder.AddForeignKey(
                name: "FK_transactions_transaction_groups_group_id",
                table: "transactions",
                column: "group_id",
                principalTable: "transaction_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Add transaction_group_id column to receipt_transaction_matches
            migrationBuilder.AddColumn<Guid>(
                name: "transaction_group_id",
                table: "receipt_transaction_matches",
                type: "uuid",
                nullable: true);

            // Make transaction_id nullable (requires dropping and recreating constraints)
            migrationBuilder.AlterColumn<Guid>(
                name: "transaction_id",
                table: "receipt_transaction_matches",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // Create index for transaction_group_id
            migrationBuilder.CreateIndex(
                name: "ix_rtm_transaction_group",
                table: "receipt_transaction_matches",
                column: "transaction_group_id");

            // Create FK constraint for transaction_group_id
            migrationBuilder.AddForeignKey(
                name: "FK_rtm_transaction_groups_transaction_group_id",
                table: "receipt_transaction_matches",
                column: "transaction_group_id",
                principalTable: "transaction_groups",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Create partial unique index for transaction groups
            migrationBuilder.CreateIndex(
                name: "ix_rtm_transaction_group_confirmed",
                table: "receipt_transaction_matches",
                column: "transaction_group_id",
                unique: true,
                filter: "status = 1 AND transaction_group_id IS NOT NULL");

            // Update existing transaction confirmed index to handle nullable
            migrationBuilder.DropIndex(
                name: "ix_rtm_transaction_confirmed",
                table: "receipt_transaction_matches");

            migrationBuilder.CreateIndex(
                name: "ix_rtm_transaction_confirmed",
                table: "receipt_transaction_matches",
                column: "transaction_id",
                unique: true,
                filter: "status = 1 AND transaction_id IS NOT NULL");

            // Add check constraint: exactly one of transaction_id or transaction_group_id must be set
            migrationBuilder.Sql(
                @"ALTER TABLE receipt_transaction_matches
                  ADD CONSTRAINT chk_transaction_or_group
                  CHECK ((transaction_id IS NOT NULL AND transaction_group_id IS NULL)
                      OR (transaction_id IS NULL AND transaction_group_id IS NOT NULL))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop check constraint on receipt_transaction_matches
            migrationBuilder.Sql(
                "ALTER TABLE receipt_transaction_matches DROP CONSTRAINT IF EXISTS chk_transaction_or_group");

            // Drop transaction group confirmed index
            migrationBuilder.DropIndex(
                name: "ix_rtm_transaction_group_confirmed",
                table: "receipt_transaction_matches");

            // Restore original transaction confirmed index
            migrationBuilder.DropIndex(
                name: "ix_rtm_transaction_confirmed",
                table: "receipt_transaction_matches");

            migrationBuilder.CreateIndex(
                name: "ix_rtm_transaction_confirmed",
                table: "receipt_transaction_matches",
                column: "transaction_id",
                unique: true,
                filter: "status = 1");

            // Drop FK constraint for transaction_group_id
            migrationBuilder.DropForeignKey(
                name: "FK_rtm_transaction_groups_transaction_group_id",
                table: "receipt_transaction_matches");

            // Drop transaction_group_id index
            migrationBuilder.DropIndex(
                name: "ix_rtm_transaction_group",
                table: "receipt_transaction_matches");

            // Make transaction_id required again
            migrationBuilder.AlterColumn<Guid>(
                name: "transaction_id",
                table: "receipt_transaction_matches",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Drop transaction_group_id column
            migrationBuilder.DropColumn(
                name: "transaction_group_id",
                table: "receipt_transaction_matches");

            // Remove FK constraint for transactions.group_id
            migrationBuilder.DropForeignKey(
                name: "FK_transactions_transaction_groups_group_id",
                table: "transactions");

            // Drop index on transactions.group_id
            migrationBuilder.DropIndex(
                name: "ix_transactions_group_id",
                table: "transactions");

            // Remove group_id column from transactions
            migrationBuilder.DropColumn(
                name: "group_id",
                table: "transactions");

            // Drop transaction_groups table (cascades indexes)
            migrationBuilder.DropTable(
                name: "transaction_groups");
        }
    }
}
