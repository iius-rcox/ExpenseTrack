using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations;

/// <summary>
/// Creates the extraction_corrections table for storing training feedback.
/// User corrections to AI-extracted receipt fields are recorded here for model improvement.
/// Retained indefinitely as permanent training corpus.
/// </summary>
public partial class AddExtractionCorrections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create extraction_corrections table
        migrationBuilder.CreateTable(
            name: "extraction_corrections",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                field_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                original_value = table.Column<string>(type: "text", nullable: true),
                corrected_value = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_extraction_corrections", x => x.id);
                table.ForeignKey(
                    name: "fk_extraction_corrections_receipt",
                    column: x => x.receipt_id,
                    principalTable: "receipts",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_extraction_corrections_user",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.CheckConstraint(
                    name: "ck_extraction_corrections_field_name",
                    sql: "field_name IN ('vendor', 'amount', 'date', 'tax', 'currency', 'line_item')");
            });

        // Create indexes for efficient querying
        migrationBuilder.CreateIndex(
            name: "ix_extraction_corrections_receipt_id",
            table: "extraction_corrections",
            column: "receipt_id");

        migrationBuilder.CreateIndex(
            name: "ix_extraction_corrections_user_id",
            table: "extraction_corrections",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_extraction_corrections_created_at",
            table: "extraction_corrections",
            column: "created_at",
            descending: new[] { true });

        migrationBuilder.CreateIndex(
            name: "ix_extraction_corrections_field_name",
            table: "extraction_corrections",
            column: "field_name");

        // Add table comment for documentation
        migrationBuilder.Sql(
            "COMMENT ON TABLE extraction_corrections IS 'Training feedback: user corrections to AI-extracted receipt fields. Retained indefinitely.';");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "extraction_corrections");
    }
}
