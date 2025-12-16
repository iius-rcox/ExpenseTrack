using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVendorCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add Category column to vendor_aliases with default Standard (0)
            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "vendor_aliases",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Create index for filtering by Category (for travel detection queries)
            migrationBuilder.CreateIndex(
                name: "ix_vendor_aliases_category",
                table: "vendor_aliases",
                column: "category");

            // Seed airline vendor aliases
            migrationBuilder.InsertData(
                table: "vendor_aliases",
                columns: new[] { "id", "canonical_name", "alias_pattern", "display_name", "default_gl_code", "category", "confidence", "created_at" },
                values: new object[,]
                {
                    { Guid.Parse("10000000-0000-0000-0000-000000000001"), "DELTA", "DELTA", "Delta Airlines", "66300", 1, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("10000000-0000-0000-0000-000000000002"), "UNITED", "UNITED", "United Airlines", "66300", 1, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("10000000-0000-0000-0000-000000000003"), "AMERICAN", "AMERICAN AIR", "American Airlines", "66300", 1, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("10000000-0000-0000-0000-000000000004"), "SOUTHWEST", "SOUTHWEST", "Southwest Airlines", "66300", 1, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("10000000-0000-0000-0000-000000000005"), "ALASKA", "ALASKA AIR", "Alaska Airlines", "66300", 1, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("10000000-0000-0000-0000-000000000006"), "JETBLUE", "JETBLUE", "JetBlue Airways", "66300", 1, 1.00m, DateTime.UtcNow }
                });

            // Seed hotel vendor aliases
            migrationBuilder.InsertData(
                table: "vendor_aliases",
                columns: new[] { "id", "canonical_name", "alias_pattern", "display_name", "default_gl_code", "category", "confidence", "created_at" },
                values: new object[,]
                {
                    { Guid.Parse("20000000-0000-0000-0000-000000000001"), "MARRIOTT", "MARRIOTT", "Marriott Hotels", "66300", 2, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("20000000-0000-0000-0000-000000000002"), "HILTON", "HILTON", "Hilton Hotels", "66300", 2, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("20000000-0000-0000-0000-000000000003"), "HYATT", "HYATT", "Hyatt Hotels", "66300", 2, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("20000000-0000-0000-0000-000000000004"), "IHG", "IHG", "IHG Hotels", "66300", 2, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("20000000-0000-0000-0000-000000000005"), "HOLIDAY INN", "HOLIDAY INN", "Holiday Inn", "66300", 2, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("20000000-0000-0000-0000-000000000006"), "AIRBNB", "AIRBNB", "Airbnb", "66300", 2, 1.00m, DateTime.UtcNow },
                    { Guid.Parse("20000000-0000-0000-0000-000000000007"), "VRBO", "VRBO", "VRBO", "66300", 2, 1.00m, DateTime.UtcNow }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove seeded hotel aliases
            migrationBuilder.DeleteData(
                table: "vendor_aliases",
                keyColumn: "id",
                keyValues: new object[]
                {
                    Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    Guid.Parse("20000000-0000-0000-0000-000000000003"),
                    Guid.Parse("20000000-0000-0000-0000-000000000004"),
                    Guid.Parse("20000000-0000-0000-0000-000000000005"),
                    Guid.Parse("20000000-0000-0000-0000-000000000006"),
                    Guid.Parse("20000000-0000-0000-0000-000000000007")
                });

            // Remove seeded airline aliases
            migrationBuilder.DeleteData(
                table: "vendor_aliases",
                keyColumn: "id",
                keyValues: new object[]
                {
                    Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Guid.Parse("10000000-0000-0000-0000-000000000003"),
                    Guid.Parse("10000000-0000-0000-0000-000000000004"),
                    Guid.Parse("10000000-0000-0000-0000-000000000005"),
                    Guid.Parse("10000000-0000-0000-0000-000000000006")
                });

            // Drop index
            migrationBuilder.DropIndex(
                name: "ix_vendor_aliases_category",
                table: "vendor_aliases");

            // Drop column
            migrationBuilder.DropColumn(
                name: "category",
                table: "vendor_aliases");
        }
    }
}
