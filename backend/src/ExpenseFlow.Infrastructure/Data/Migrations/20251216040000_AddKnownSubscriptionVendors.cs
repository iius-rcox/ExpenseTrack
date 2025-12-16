using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnownSubscriptionVendors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create known_subscription_vendors table
            migrationBuilder.CreateTable(
                name: "known_subscription_vendors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    vendor_pattern = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    typical_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_known_subscription_vendors", x => x.id);
                });

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_known_subscription_vendors_pattern",
                table: "known_subscription_vendors",
                column: "vendor_pattern",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_known_subscription_vendors_active",
                table: "known_subscription_vendors",
                column: "is_active");

            // Seed known subscription vendors
            migrationBuilder.InsertData(
                table: "known_subscription_vendors",
                columns: new[] { "id", "vendor_pattern", "display_name", "category", "typical_amount", "is_active", "created_at" },
                values: new object[,]
                {
                    // Software subscriptions
                    { Guid.Parse("30000000-0000-0000-0000-000000000001"), "OPENAI", "OpenAI", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000002"), "CLAUDE", "Claude.AI", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000003"), "CURSOR", "Cursor", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000004"), "GITHUB", "GitHub", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000005"), "MICROSOFT 365", "Microsoft 365", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000006"), "ADOBE", "Adobe Creative Cloud", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000007"), "ZOOM", "Zoom", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000008"), "SLACK", "Slack", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000009"), "NOTION", "Notion", "Software", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000010"), "FIGMA", "Figma", "Software", null, true, DateTime.UtcNow },

                    // Media subscriptions
                    { Guid.Parse("30000000-0000-0000-0000-000000000011"), "SPOTIFY", "Spotify", "Media", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000012"), "NETFLIX", "Netflix", "Media", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000013"), "AMAZON PRIME", "Amazon Prime", "Media", null, true, DateTime.UtcNow },

                    // Cloud subscriptions
                    { Guid.Parse("30000000-0000-0000-0000-000000000014"), "AWS", "Amazon Web Services", "Cloud", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000015"), "AZURE", "Microsoft Azure", "Cloud", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000016"), "GOOGLE CLOUD", "Google Cloud", "Cloud", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000017"), "DIGITALOCEAN", "DigitalOcean", "Cloud", null, true, DateTime.UtcNow },
                    { Guid.Parse("30000000-0000-0000-0000-000000000018"), "HEROKU", "Heroku", "Cloud", null, true, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "known_subscription_vendors");
        }
    }
}
