using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportJobTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "vendor_aliases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "split_patterns",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "split_patterns",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "split_patterns",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "detected_subscriptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vendor_alias_id = table.Column<Guid>(type: "uuid", nullable: true),
                    vendor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    average_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    occurrence_months = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    last_seen_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_next_date = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    detection_source = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_detected_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_detected_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_detected_subscriptions_vendor_aliases_vendor_alias_id",
                        column: x => x.vendor_alias_id,
                        principalTable: "vendor_aliases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "expense_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    status = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0.00m),
                    line_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    missing_receipt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tier1_hit_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tier2_hit_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tier3_hit_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_reports", x => x.id);
                    table.CheckConstraint("chk_period_format", "period ~ '^\\d{4}-(0[1-9]|1[0-2])$'");
                    table.CheckConstraint("chk_report_status_valid", "status >= 0 AND status <= 3");
                    table.ForeignKey(
                        name: "FK_expense_reports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "import_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    blob_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    processed_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cached_descriptions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_aliases = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    generated_embeddings = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    skipped_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_log = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_import_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_import_jobs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "known_subscription_vendors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    vendor_pattern = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    typical_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_known_subscription_vendors", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "travel_periods",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    destination = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    source = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    source_receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requires_ai_review = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_travel_periods", x => x.id);
                    table.ForeignKey(
                        name: "FK_travel_periods_receipts_source_receipt_id",
                        column: x => x.source_receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_travel_periods_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "expense_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    line_order = table.Column<int>(type: "integer", nullable: false),
                    expense_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    original_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    normalized_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    vendor_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    gl_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    gl_code_suggested = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    gl_code_tier = table.Column<int>(type: "integer", nullable: true),
                    gl_code_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    department_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    department_suggested = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    department_tier = table.Column<int>(type: "integer", nullable: true),
                    department_source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    has_receipt = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    missing_receipt_justification = table.Column<short>(type: "smallint", nullable: true),
                    justification_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_user_edited = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_lines", x => x.id);
                    table.CheckConstraint("chk_dept_tier_valid", "department_tier IS NULL OR department_tier IN (1, 2, 3)");
                    table.CheckConstraint("chk_expense_line_has_source", "receipt_id IS NOT NULL OR transaction_id IS NOT NULL");
                    table.CheckConstraint("chk_gl_tier_valid", "gl_code_tier IS NULL OR gl_code_tier IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_expense_lines_expense_reports_report_id",
                        column: x => x.report_id,
                        principalTable: "expense_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_expense_lines_receipts_receipt_id",
                        column: x => x.receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_expense_lines_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_vendor_aliases_category",
                table: "vendor_aliases",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_split_patterns_user_id",
                table: "split_patterns",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_split_patterns_user_vendor",
                table: "split_patterns",
                columns: new[] { "user_id", "vendor_alias_id" });

            migrationBuilder.CreateIndex(
                name: "ix_detected_subscriptions_user_id",
                table: "detected_subscriptions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_detected_subscriptions_user_status",
                table: "detected_subscriptions",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_detected_subscriptions_vendor_alias_id",
                table: "detected_subscriptions",
                column: "vendor_alias_id");

            migrationBuilder.CreateIndex(
                name: "IX_expense_lines_receipt_id",
                table: "expense_lines",
                column: "receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_lines_report_order",
                table: "expense_lines",
                columns: new[] { "report_id", "line_order" });

            migrationBuilder.CreateIndex(
                name: "ix_expense_lines_transaction",
                table: "expense_lines",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_reports_user_created",
                table: "expense_reports",
                columns: new[] { "user_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_expense_reports_user_period",
                table: "expense_reports",
                columns: new[] { "user_id", "period" },
                unique: true,
                filter: "NOT is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_started_at",
                table: "import_jobs",
                column: "started_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_import_jobs_user_id_status",
                table: "import_jobs",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_known_subscription_vendors_active",
                table: "known_subscription_vendors",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_known_subscription_vendors_pattern",
                table: "known_subscription_vendors",
                column: "vendor_pattern",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_travel_periods_source_receipt_id",
                table: "travel_periods",
                column: "source_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_travel_periods_user_dates",
                table: "travel_periods",
                columns: new[] { "user_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_travel_periods_user_id",
                table: "travel_periods",
                column: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_split_patterns_users_user_id",
                table: "split_patterns",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_split_patterns_users_user_id",
                table: "split_patterns");

            migrationBuilder.DropTable(
                name: "detected_subscriptions");

            migrationBuilder.DropTable(
                name: "expense_lines");

            migrationBuilder.DropTable(
                name: "import_jobs");

            migrationBuilder.DropTable(
                name: "known_subscription_vendors");

            migrationBuilder.DropTable(
                name: "travel_periods");

            migrationBuilder.DropTable(
                name: "expense_reports");

            migrationBuilder.DropIndex(
                name: "ix_vendor_aliases_category",
                table: "vendor_aliases");

            migrationBuilder.DropIndex(
                name: "ix_split_patterns_user_id",
                table: "split_patterns");

            migrationBuilder.DropIndex(
                name: "ix_split_patterns_user_vendor",
                table: "split_patterns");

            migrationBuilder.DropColumn(
                name: "category",
                table: "vendor_aliases");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "split_patterns");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "split_patterns");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "split_patterns");
        }
    }
}
