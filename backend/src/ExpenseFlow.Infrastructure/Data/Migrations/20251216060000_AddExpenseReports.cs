using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create expense_reports table
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_reports", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_reports_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.CheckConstraint(
                        name: "chk_period_format",
                        sql: "period ~ '^\\d{4}-(0[1-9]|1[0-2])$'");
                    table.CheckConstraint(
                        name: "chk_report_status_valid",
                        sql: "status >= 0 AND status <= 3");
                });

            // Create expense_lines table
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_lines", x => x.id);
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
                    table.CheckConstraint(
                        name: "chk_expense_line_has_source",
                        sql: "receipt_id IS NOT NULL OR transaction_id IS NOT NULL");
                    table.CheckConstraint(
                        name: "chk_gl_tier_valid",
                        sql: "gl_code_tier IS NULL OR gl_code_tier IN (1, 2, 3)");
                    table.CheckConstraint(
                        name: "chk_dept_tier_valid",
                        sql: "department_tier IS NULL OR department_tier IN (1, 2, 3)");
                });

            // Indexes for expense_reports
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_expense_reports_user_period
                ON expense_reports (user_id, period)
                WHERE NOT is_deleted;
            ");

            migrationBuilder.CreateIndex(
                name: "ix_expense_reports_user_created",
                table: "expense_reports",
                columns: new[] { "user_id", "created_at" });

            // Indexes for expense_lines
            migrationBuilder.CreateIndex(
                name: "ix_expense_lines_report_order",
                table: "expense_lines",
                columns: new[] { "report_id", "line_order" });

            migrationBuilder.CreateIndex(
                name: "ix_expense_lines_transaction",
                table: "expense_lines",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_lines_receipt",
                table: "expense_lines",
                column: "receipt_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop expense_lines table (will drop all indexes and constraints)
            migrationBuilder.DropTable(
                name: "expense_lines");

            // Drop expense_reports table (will drop all indexes and constraints)
            migrationBuilder.DropTable(
                name: "expense_reports");
        }
    }
}
