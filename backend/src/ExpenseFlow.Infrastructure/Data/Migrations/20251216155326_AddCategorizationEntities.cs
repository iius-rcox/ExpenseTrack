using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorizationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_receipts_users_UserId",
                table: "receipts");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "receipts",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "receipts",
                newName: "currency");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "receipts",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "VendorExtracted",
                table: "receipts",
                newName: "vendor_extracted");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "receipts",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "ThumbnailUrl",
                table: "receipts",
                newName: "thumbnail_url");

            migrationBuilder.RenameColumn(
                name: "TaxExtracted",
                table: "receipts",
                newName: "tax_extracted");

            migrationBuilder.RenameColumn(
                name: "RetryCount",
                table: "receipts",
                newName: "retry_count");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "receipts",
                newName: "processed_at");

            migrationBuilder.RenameColumn(
                name: "PageCount",
                table: "receipts",
                newName: "page_count");

            migrationBuilder.RenameColumn(
                name: "OriginalFilename",
                table: "receipts",
                newName: "original_filename");

            migrationBuilder.RenameColumn(
                name: "LineItems",
                table: "receipts",
                newName: "line_items");

            migrationBuilder.RenameColumn(
                name: "FileSize",
                table: "receipts",
                newName: "file_size");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "receipts",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "DateExtracted",
                table: "receipts",
                newName: "date_extracted");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "receipts",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "ContentType",
                table: "receipts",
                newName: "content_type");

            migrationBuilder.RenameColumn(
                name: "ConfidenceScores",
                table: "receipts",
                newName: "confidence_scores");

            migrationBuilder.RenameColumn(
                name: "BlobUrl",
                table: "receipts",
                newName: "blob_url");

            migrationBuilder.RenameColumn(
                name: "AmountExtracted",
                table: "receipts",
                newName: "amount_extracted");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_Status",
                table: "receipts",
                newName: "IX_receipts_status");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_UserId_Status",
                table: "receipts",
                newName: "IX_receipts_user_id_status");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_UserId",
                table: "receipts",
                newName: "IX_receipts_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_CreatedAt",
                table: "receipts",
                newName: "IX_receipts_created_at");

            migrationBuilder.AddColumn<int>(
                name: "dept_confirm_count",
                table: "vendor_aliases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "gl_confirm_count",
                table: "vendor_aliases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

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

            migrationBuilder.AlterColumn<string>(
                name: "currency",
                table: "receipts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldNullable: true,
                oldDefaultValue: "USD");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "date_extracted",
                table: "receipts",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<short>(
                name: "match_status",
                table: "receipts",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<Guid>(
                name: "matched_transaction_id",
                table: "receipts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "expires_at",
                table: "expense_embeddings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "transaction_id",
                table: "expense_embeddings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "expense_embeddings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAccessedAt",
                table: "description_cache",
                type: "timestamp with time zone",
                nullable: true);

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
                        name: "FK_statement_imports_statement_fingerprints_fingerprint_id",
                        column: x => x.fingerprint_id,
                        principalTable: "statement_fingerprints",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_statement_imports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    duplicate_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    matched_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    match_status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_statement_imports_import_id",
                        column: x => x.import_id,
                        principalTable: "statement_imports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transactions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    confirmed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receipt_transaction_matches", x => x.id);
                    table.CheckConstraint("chk_confidence_range", "confidence_score >= 0 AND confidence_score <= 100");
                    table.CheckConstraint("chk_status_valid", "status IN (0, 1, 2)");
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
                        name: "FK_receipt_transaction_matches_users_confirmed_by_user_id",
                        column: x => x.confirmed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_receipt_transaction_matches_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_receipt_transaction_matches_vendor_aliases_matched_vendor_a~",
                        column: x => x.matched_vendor_alias_id,
                        principalTable: "vendor_aliases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tier_usage_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operation_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    tier_used = table.Column<int>(type: "integer", nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: true),
                    response_time_ms = table.Column<int>(type: "integer", nullable: true),
                    cache_hit = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tier_usage_logs", x => x.id);
                    table.CheckConstraint("ck_tier_usage_logs_tier", "tier_used BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_tier_usage_logs_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tier_usage_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_receipts_match_status",
                table: "receipts",
                columns: new[] { "user_id", "match_status" });

            migrationBuilder.CreateIndex(
                name: "IX_receipts_matched_transaction_id",
                table: "receipts",
                column: "matched_transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_expense_embeddings_expires",
                table: "expense_embeddings",
                column: "expires_at",
                filter: "expires_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_expense_embeddings_transaction_id",
                table: "expense_embeddings",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_embeddings_user_id",
                table: "expense_embeddings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_embeddings_verified_user",
                table: "expense_embeddings",
                columns: new[] { "verified", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_receipt_transaction_matches_confirmed_by_user_id",
                table: "receipt_transaction_matches",
                column: "confirmed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_receipt_transaction_matches_matched_vendor_alias_id",
                table: "receipt_transaction_matches",
                column: "matched_vendor_alias_id");

            migrationBuilder.CreateIndex(
                name: "ix_rtm_receipt_confirmed",
                table: "receipt_transaction_matches",
                column: "receipt_id",
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_rtm_transaction_confirmed",
                table: "receipt_transaction_matches",
                column: "transaction_id",
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "ix_rtm_user_status",
                table: "receipt_transaction_matches",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_statement_imports_created_at",
                table: "statement_imports",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_statement_imports_fingerprint_id",
                table: "statement_imports",
                column: "fingerprint_id");

            migrationBuilder.CreateIndex(
                name: "ix_statement_imports_user_id",
                table: "statement_imports",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_tier_usage_logs_transaction_id",
                table: "tier_usage_logs",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_tier_usage_logs_type_tier",
                table: "tier_usage_logs",
                columns: new[] { "operation_type", "tier_used", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_tier_usage_logs_user_date",
                table: "tier_usage_logs",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_import_id",
                table: "transactions",
                column: "import_id");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_match_status",
                table: "transactions",
                columns: new[] { "user_id", "match_status" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_date",
                table: "transactions",
                columns: new[] { "user_id", "transaction_date" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_duplicate_hash",
                table: "transactions",
                columns: new[] { "user_id", "duplicate_hash" });

            migrationBuilder.CreateIndex(
                name: "ix_transactions_user_id",
                table: "transactions",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_expense_embeddings_transactions_transaction_id",
                table: "expense_embeddings",
                column: "transaction_id",
                principalTable: "transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_expense_embeddings_users_user_id",
                table: "expense_embeddings",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_receipts_transactions_matched_transaction_id",
                table: "receipts",
                column: "matched_transaction_id",
                principalTable: "transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_receipts_users_user_id",
                table: "receipts",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_expense_embeddings_transactions_transaction_id",
                table: "expense_embeddings");

            migrationBuilder.DropForeignKey(
                name: "FK_expense_embeddings_users_user_id",
                table: "expense_embeddings");

            migrationBuilder.DropForeignKey(
                name: "FK_receipts_transactions_matched_transaction_id",
                table: "receipts");

            migrationBuilder.DropForeignKey(
                name: "FK_receipts_users_user_id",
                table: "receipts");

            migrationBuilder.DropTable(
                name: "receipt_transaction_matches");

            migrationBuilder.DropTable(
                name: "tier_usage_logs");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "statement_imports");

            migrationBuilder.DropIndex(
                name: "ix_receipts_match_status",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "IX_receipts_matched_transaction_id",
                table: "receipts");

            migrationBuilder.DropIndex(
                name: "ix_expense_embeddings_expires",
                table: "expense_embeddings");

            migrationBuilder.DropIndex(
                name: "IX_expense_embeddings_transaction_id",
                table: "expense_embeddings");

            migrationBuilder.DropIndex(
                name: "IX_expense_embeddings_user_id",
                table: "expense_embeddings");

            migrationBuilder.DropIndex(
                name: "ix_expense_embeddings_verified_user",
                table: "expense_embeddings");

            migrationBuilder.DropColumn(
                name: "dept_confirm_count",
                table: "vendor_aliases");

            migrationBuilder.DropColumn(
                name: "gl_confirm_count",
                table: "vendor_aliases");

            migrationBuilder.DropColumn(
                name: "hit_count",
                table: "statement_fingerprints");

            migrationBuilder.DropColumn(
                name: "last_used_at",
                table: "statement_fingerprints");

            migrationBuilder.DropColumn(
                name: "match_status",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "matched_transaction_id",
                table: "receipts");

            migrationBuilder.DropColumn(
                name: "expires_at",
                table: "expense_embeddings");

            migrationBuilder.DropColumn(
                name: "transaction_id",
                table: "expense_embeddings");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "expense_embeddings");

            migrationBuilder.DropColumn(
                name: "LastAccessedAt",
                table: "description_cache");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "receipts",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "currency",
                table: "receipts",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "receipts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "vendor_extracted",
                table: "receipts",
                newName: "VendorExtracted");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "receipts",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "thumbnail_url",
                table: "receipts",
                newName: "ThumbnailUrl");

            migrationBuilder.RenameColumn(
                name: "tax_extracted",
                table: "receipts",
                newName: "TaxExtracted");

            migrationBuilder.RenameColumn(
                name: "retry_count",
                table: "receipts",
                newName: "RetryCount");

            migrationBuilder.RenameColumn(
                name: "processed_at",
                table: "receipts",
                newName: "ProcessedAt");

            migrationBuilder.RenameColumn(
                name: "page_count",
                table: "receipts",
                newName: "PageCount");

            migrationBuilder.RenameColumn(
                name: "original_filename",
                table: "receipts",
                newName: "OriginalFilename");

            migrationBuilder.RenameColumn(
                name: "line_items",
                table: "receipts",
                newName: "LineItems");

            migrationBuilder.RenameColumn(
                name: "file_size",
                table: "receipts",
                newName: "FileSize");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "receipts",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "date_extracted",
                table: "receipts",
                newName: "DateExtracted");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "receipts",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "content_type",
                table: "receipts",
                newName: "ContentType");

            migrationBuilder.RenameColumn(
                name: "confidence_scores",
                table: "receipts",
                newName: "ConfidenceScores");

            migrationBuilder.RenameColumn(
                name: "blob_url",
                table: "receipts",
                newName: "BlobUrl");

            migrationBuilder.RenameColumn(
                name: "amount_extracted",
                table: "receipts",
                newName: "AmountExtracted");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_status",
                table: "receipts",
                newName: "IX_receipts_Status");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_user_id_status",
                table: "receipts",
                newName: "IX_receipts_UserId_Status");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_user_id",
                table: "receipts",
                newName: "IX_receipts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_receipts_created_at",
                table: "receipts",
                newName: "IX_receipts_CreatedAt");

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "statement_fingerprints",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "receipts",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldDefaultValue: "USD");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DateExtracted",
                table: "receipts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_receipts_users_UserId",
                table: "receipts",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
