using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations;

/// <summary>
/// Adds IsManualOverride column and makes PatternId nullable to support manual transaction marking.
/// Manual overrides allow users to directly mark transactions as reimbursable/not-reimbursable
/// without requiring a learned pattern.
/// </summary>
public partial class AddManualOverrideToTransactionPrediction : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add is_manual_override column
        migrationBuilder.AddColumn<bool>(
            name: "is_manual_override",
            table: "transaction_predictions",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        // Make pattern_id nullable to support manual overrides (no pattern)
        migrationBuilder.AlterColumn<Guid>(
            name: "pattern_id",
            table: "transaction_predictions",
            type: "uuid",
            nullable: true,
            oldClrType: typeof(Guid),
            oldType: "uuid");

        // Index for efficient manual override queries
        migrationBuilder.CreateIndex(
            name: "ix_transaction_predictions_manual_override",
            table: "transaction_predictions",
            column: "is_manual_override",
            filter: "is_manual_override = true");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove the manual override index
        migrationBuilder.DropIndex(
            name: "ix_transaction_predictions_manual_override",
            table: "transaction_predictions");

        // Make pattern_id required again (will fail if nulls exist)
        migrationBuilder.AlterColumn<Guid>(
            name: "pattern_id",
            table: "transaction_predictions",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true);

        // Remove is_manual_override column
        migrationBuilder.DropColumn(
            name: "is_manual_override",
            table: "transaction_predictions");
    }
}
