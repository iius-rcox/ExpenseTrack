using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReportPreferencesFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "employee_id",
                table: "user_preferences",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "supervisor_name",
                table: "user_preferences",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "department_name",
                table: "user_preferences",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "employee_id",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "supervisor_name",
                table: "user_preferences");

            migrationBuilder.DropColumn(
                name: "department_name",
                table: "user_preferences");
        }
    }
}
