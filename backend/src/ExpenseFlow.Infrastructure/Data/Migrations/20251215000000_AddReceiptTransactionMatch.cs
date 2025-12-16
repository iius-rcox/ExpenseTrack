using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptTransactionMatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add match status columns to receipts
            migrationBuilder.AddColumn<Guid>(
                name: "matched_transaction_id",
                table: "receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "match_status",
                table: "receipts",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            // Add match status column to transactions
            migrationBuilder.AddColumn<short>(
                name: "match_status",
                table: "transactions",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            // Create receipt_transaction_matches table
            migrationBuilder.CreateTable(
                name: "receipt_transaction_matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    confidence_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.00m),
                    amount_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.00m),
                    date_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.00m),
                    vendor_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.00m),
                    match_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    matched_vendor_alias_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_manual_match = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_transaction_matches", x => x.id);
                    table.ForeignKey(
                        name: "FK_receipt_transaction_matches_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_receipt_transaction_matches_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_receipt_transaction_matches_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_receipt_transaction_matches_vendor_aliases_matched_vendor_alias_id",
                        column: x => x.matched_vendor_alias_id,
                        principalTable: "vendor_aliases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_receipt_transaction_matches_users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.CheckConstraint(
                        name: "chk_confidence_range",
                        sql: "confidence_score >= 0 AND confidence_score <= 100");
                    table.CheckConstraint(
                        name: "chk_status_valid",
                        sql: "status IN (0, 1, 2)");
                });

            // Indexes for receipt_transaction_matches
            migrationBuilder.CreateIndex(
                name: "ix_rtm_user_status",
                table: "receipt_transaction_matches",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_rtm_receipt",
                table: "receipt_transaction_matches",
                column: "receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_rtm_transaction",
                table: "receipt_transaction_matches",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_rtm_matched_vendor_alias",
                table: "receipt_transaction_matches",
                column: "matched_vendor_alias_id");

            migrationBuilder.CreateIndex(
                name: "ix_rtm_confirmed_by_user",
                table: "receipt_transaction_matches",
                column: "confirmed_by_user_id");

            // Partial unique index: one receipt can only have one confirmed match
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_rtm_receipt_confirmed
                ON receipt_transaction_matches (receipt_id)
                WHERE status = 1;
            ");

            // Partial unique index: one transaction can only have one confirmed match
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_rtm_transaction_confirmed
                ON receipt_transaction_matches (transaction_id)
                WHERE status = 1;
            ");

            // Index for receipts match status
            migrationBuilder.CreateIndex(
                name: "ix_receipts_match_status",
                table: "receipts",
                columns: new[] { "user_id", "match_status" });

            // Index for transactions match status
            migrationBuilder.CreateIndex(
                name: "ix_transactions_match_status",
                table: "transactions",
                columns: new[] { "user_id", "match_status" });

            // Foreign key for receipts.matched_transaction_id
            migrationBuilder.AddForeignKey(
                name: "FK_receipts_transactions_matched_transaction_id",
                table: "receipts",
                column: "matched_transaction_id",
                principalTable: "transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.CreateIndex(
                name: "ix_receipts_matched_transaction_id",
                table: "receipts",
                column: "matched_transaction_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign key from receipts
            migrationBuilder.DropForeignKey(
                name: "FK_receipts_transactions_matched_transaction_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "ix_receipts_matched_transaction_id",
                table: "receipts");

            // Drop receipt_transaction_matches table (will drop all indexes and constraints)
            migrationBuilder.DropTable(
                name: "receipt_transaction_matches");

            // Drop indexes from receipts and transactions
            migrationBuilder.DropIndex(
                name: "ix_receipts_match_status",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "ix_transactions_match_status",
                table: "transactions");

            // Drop columns from receipts
            migrationBuilder.DropColumn(
                name: "matched_transaction_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "match_status",
                table: "receipts");

            // Drop column from transactions
            migrationBuilder.DropColumn(
                name: "match_status",
                table: "transactions");
        }
    }
}
