using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTravelPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create travel_periods table
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_travel_periods", x => x.id);
                    table.ForeignKey(
                        name: "fk_travel_periods_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_travel_periods_receipts_source_receipt_id",
                        column: x => x.source_receipt_id,
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_travel_periods_user_id",
                table: "travel_periods",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_travel_periods_user_dates",
                table: "travel_periods",
                columns: new[] { "user_id", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_travel_periods_source_receipt_id",
                table: "travel_periods",
                column: "source_receipt_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "travel_periods");
        }
    }
}
