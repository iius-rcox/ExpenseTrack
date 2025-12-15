using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatementImportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update statement_fingerprints: make user_id nullable and add new columns
            migrationBuilder.DropForeignKey(
                name: "FK_statement_fingerprints_users_user_id",
                table: "statement_fingerprints");

            migrationBuilder.DropIndex(
                name: "ix_statement_fingerprints_user_hash",
                table: "statement_fingerprints");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "statement_fingerprints",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "hit_count",
                table: "statement_fingerprints",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_used_at",
                table: "statement_fingerprints",
                type: "timestamp with time zone",
                nullable: true);

            // Create statement_imports table
            migrationBuilder.CreateTable(
                name: "statement_imports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fingerprint_id = table.Column<Guid>(type: "uuid", nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    tier_used = table.Column<int>(type: "integer", nullable: false),
                    transaction_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    skipped_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    duplicate_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statement_imports", x => x.id);
                    table.ForeignKey(
                        name: "FK_statement_imports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_statement_imports_statement_fingerprints_fingerprint_id",
                        column: x => x.fingerprint_id,
                        principalTable: "statement_fingerprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create transactions table
            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    import_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_date = table.Column<DateOnly>(type: "date", nullable: false),
                    post_date = table.Column<DateOnly>(type: "date", nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    original_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    memo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    reference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    duplicate_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    matched_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transactions_statement_imports_import_id",
                        column: x => x.import_id,
                        principalTable: "statement_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transactions_receipts_matched_receipt_id",
                        column: x => x.matched_receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes for statement_fingerprints
            migrationBuilder.CreateIndex(
                name: "ix_statement_fingerprints_user_hash",
                table: "statement_fingerprints",
                columns: new[] { "user_id", "header_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_statement_fingerprints_header_hash",
                table: "statement_fingerprints",
                column: "header_hash");

            // Create indexes for statement_imports
            migrationBuilder.CreateIndex(
                name: "ix_statement_imports_user_id",
                table: "statement_imports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_statement_imports_fingerprint_id",
                table: "statement_imports",
                column: "fingerprint_id");

            migrationBuilder.CreateIndex(
                name: "ix_statement_imports_created_at",
                table: "statement_imports",
                column: "created_at",
                descending: new[] { true });

            // Create indexes for transactions
            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_id",
                table: "transactions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_import_id",
                table: "transactions",
                column: "import_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_duplicate_hash",
                table: "transactions",
                columns: new[] { "user_id", "duplicate_hash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_date",
                table: "transactions",
                columns: new[] { "user_id", "transaction_date" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_matched_receipt_id",
                table: "transactions",
                column: "matched_receipt_id");

            // Re-add foreign key for statement_fingerprints with nullable user_id
            migrationBuilder.AddForeignKey(
                name: "FK_statement_fingerprints_users_user_id",
                table: "statement_fingerprints",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Seed system fingerprints for Chase and American Express
            migrationBuilder.InsertData(
                table: "statement_fingerprints",
                columns: new[] { "id", "user_id", "source_name", "header_hash", "column_mapping", "date_format", "amount_sign", "hit_count", "created_at" },
                values: new object[,]
                {
                    {
                        Guid.Parse("00000000-0000-0000-0000-000000000001"),
                        null, // System fingerprint (no user)
                        "Chase Business Card",
                        "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd", // Will be computed from actual headers
                        "{\"Transaction Date\":\"date\",\"Post Date\":\"post_date\",\"Description\":\"description\",\"Amount\":\"amount\",\"Category\":\"category\"}",
                        "MM/dd/yyyy",
                        "negative_charges",
                        0,
                        DateTime.UtcNow
                    },
                    {
                        Guid.Parse("00000000-0000-0000-0000-000000000002"),
                        null, // System fingerprint (no user)
                        "American Express",
                        "b2c3d4e5f678901234567890123456789012345678901234567890123456bcde", // Will be computed from actual headers
                        "{\"Date\":\"date\",\"Description\":\"description\",\"Amount\":\"amount\",\"Extended Details\":\"memo\",\"Category\":\"category\"}",
                        "MM/dd/yyyy",
                        "positive_charges",
                        0,
                        DateTime.UtcNow
                    }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeded data
            migrationBuilder.DeleteData(
                table: "statement_fingerprints",
                keyColumn: "id",
                keyValue: Guid.Parse("00000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "statement_fingerprints",
                keyColumn: "id",
                keyValue: Guid.Parse("00000000-0000-0000-0000-000000000002"));

            // Drop transactions table
            migrationBuilder.DropTable(
                name: "transactions");

            // Drop statement_imports table
            migrationBuilder.DropTable(
                name: "statement_imports");

            // Remove added columns from statement_fingerprints
            migrationBuilder.DropColumn(
                name: "hit_count",
                table: "statement_fingerprints");

            migrationBuilder.DropColumn(
                name: "last_used_at",
                table: "statement_fingerprints");

            // Revert user_id to non-nullable
            migrationBuilder.DropForeignKey(
                name: "FK_statement_fingerprints_users_user_id",
                table: "statement_fingerprints");

            migrationBuilder.DropIndex(
                name: "ix_statement_fingerprints_user_hash",
                table: "statement_fingerprints");

            migrationBuilder.DropIndex(
                name: "ix_statement_fingerprints_header_hash",
                table: "statement_fingerprints");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "statement_fingerprints",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_statement_fingerprints_user_hash",
                table: "statement_fingerprints",
                columns: new[] { "user_id", "header_hash" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_statement_fingerprints_users_user_id",
                table: "statement_fingerprints",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
