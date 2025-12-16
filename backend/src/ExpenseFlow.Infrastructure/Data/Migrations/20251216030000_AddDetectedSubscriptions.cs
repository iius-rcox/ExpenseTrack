using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDetectedSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create detected_subscriptions table
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
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_detected_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_detected_subscriptions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_detected_subscriptions_vendor_aliases_vendor_alias_id",
                        column: x => x.vendor_alias_id,
                        principalTable: "vendor_aliases",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create indexes
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "detected_subscriptions");
        }
    }
}
