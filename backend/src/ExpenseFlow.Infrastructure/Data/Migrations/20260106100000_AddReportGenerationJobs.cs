using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportGenerationJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "report_generation_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    total_lines = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    processed_lines = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_lines = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    error_details = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    hangfire_job_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    estimated_completion_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    generated_report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_report_generation_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_report_generation_jobs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_report_generation_jobs_expense_reports_generated_report_id",
                        column: x => x.generated_report_id,
                        principalTable: "expense_reports",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_jobs_user_status",
                table: "report_generation_jobs",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_jobs_completed_at",
                table: "report_generation_jobs",
                column: "completed_at",
                filter: "completed_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_report_generation_jobs_user_period_active",
                table: "report_generation_jobs",
                columns: new[] { "user_id", "period" },
                unique: true,
                filter: "status NOT IN (2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_report_generation_jobs_generated_report_id",
                table: "report_generation_jobs",
                column: "generated_report_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "report_generation_jobs");
        }
    }
}
