using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations;

/// <summary>
/// Adds ReceiptUrl and ReceiptDismissed columns to transactions table
/// for the Missing Receipts UI feature (Feature 026).
/// </summary>
public partial class AddMissingReceiptFieldsToTransaction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add receipt_url column for storing external receipt retrieval URLs
        migrationBuilder.AddColumn<string>(
            name: "receipt_url",
            table: "transactions",
            type: "text",
            nullable: true);

        // Add receipt_dismissed column for soft-dismissing from missing receipts list
        migrationBuilder.AddColumn<bool>(
            name: "receipt_dismissed",
            table: "transactions",
            type: "boolean",
            nullable: true);

        // Index for efficient missing receipts query
        // Finds transactions without receipts that aren't dismissed
        migrationBuilder.CreateIndex(
            name: "ix_transactions_user_missing_receipt",
            table: "transactions",
            columns: new[] { "user_id", "matched_receipt_id", "receipt_dismissed" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_transactions_user_missing_receipt",
            table: "transactions");

        migrationBuilder.DropColumn(
            name: "receipt_dismissed",
            table: "transactions");

        migrationBuilder.DropColumn(
            name: "receipt_url",
            table: "transactions");
    }
}
