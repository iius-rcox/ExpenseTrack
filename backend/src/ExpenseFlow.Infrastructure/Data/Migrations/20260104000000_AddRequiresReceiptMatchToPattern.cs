using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations;

/// <summary>
/// Adds RequiresReceiptMatch column to expense_patterns table.
/// When enabled, predictions are only generated for transactions with confirmed receipt matches.
/// </summary>
public partial class AddRequiresReceiptMatchToPattern : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "requires_receipt_match",
            table: "expense_patterns",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "requires_receipt_match",
            table: "expense_patterns");
    }
}
