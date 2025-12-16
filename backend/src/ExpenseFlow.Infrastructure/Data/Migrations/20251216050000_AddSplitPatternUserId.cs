using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSplitPatternUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add user_id column to split_patterns (nullable initially for existing data)
            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "split_patterns",
                type: "uuid",
                nullable: true);

            // If there are existing split patterns without user_id, we need to handle them
            // For now, we'll just delete any orphaned patterns (none should exist in a fresh system)
            migrationBuilder.Sql(@"
                DELETE FROM split_patterns WHERE user_id IS NULL;
            ");

            // Make user_id required
            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "split_patterns",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "fk_split_patterns_users_user_id",
                table: "split_patterns",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "ix_split_patterns_user_id",
                table: "split_patterns",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_split_patterns_user_vendor",
                table: "split_patterns",
                columns: new[] { "user_id", "vendor_alias_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes
            migrationBuilder.DropIndex(
                name: "ix_split_patterns_user_vendor",
                table: "split_patterns");

            migrationBuilder.DropIndex(
                name: "ix_split_patterns_user_id",
                table: "split_patterns");

            // Drop foreign key
            migrationBuilder.DropForeignKey(
                name: "fk_split_patterns_users_user_id",
                table: "split_patterns");

            // Drop column
            migrationBuilder.DropColumn(
                name: "user_id",
                table: "split_patterns");
        }
    }
}
