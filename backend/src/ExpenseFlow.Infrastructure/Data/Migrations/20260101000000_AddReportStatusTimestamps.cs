using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportStatusTimestamps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add generated_at column for tracking when report was finalized
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "generated_at",
                table: "expense_reports",
                type: "timestamp with time zone",
                nullable: true);

            // Add submitted_at column for tracking when report was submitted
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "submitted_at",
                table: "expense_reports",
                type: "timestamp with time zone",
                nullable: true);

            // Add index for querying reports by status and generated date
            migrationBuilder.CreateIndex(
                name: "ix_expense_reports_status_generated",
                table: "expense_reports",
                columns: new[] { "status", "generated_at" },
                filter: "generated_at IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop index first
            migrationBuilder.DropIndex(
                name: "ix_expense_reports_status_generated",
                table: "expense_reports");

            // Remove submitted_at column
            migrationBuilder.DropColumn(
                name: "submitted_at",
                table: "expense_reports");

            // Remove generated_at column
            migrationBuilder.DropColumn(
                name: "generated_at",
                table: "expense_reports");
        }
    }
}
