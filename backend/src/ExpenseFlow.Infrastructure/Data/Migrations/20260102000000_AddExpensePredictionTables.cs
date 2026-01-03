using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExpenseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddExpensePredictionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create expense_patterns table
            migrationBuilder.CreateTable(
                name: "expense_patterns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normalized_vendor = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    average_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    min_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    max_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    default_gl_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    default_department = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    confirm_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    reject_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_suppressed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expense_patterns", x => x.id);
                    table.ForeignKey(
                        name: "FK_expense_patterns_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for expense_patterns
            migrationBuilder.CreateIndex(
                name: "ix_expense_patterns_user_id",
                table: "expense_patterns",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_expense_patterns_user_vendor",
                table: "expense_patterns",
                columns: new[] { "user_id", "normalized_vendor" },
                unique: true);

            // Create transaction_predictions table
            migrationBuilder.CreateTable(
                name: "transaction_predictions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    pattern_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confidence_score = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    confidence_level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_predictions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transaction_predictions_expense_patterns_pattern_id",
                        column: x => x.pattern_id,
                        principalTable: "expense_patterns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transaction_predictions_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_transaction_predictions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for transaction_predictions
            migrationBuilder.CreateIndex(
                name: "ix_transaction_predictions_transaction",
                table: "transaction_predictions",
                column: "transaction_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transaction_predictions_user_id",
                table: "transaction_predictions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_transaction_predictions_user_status",
                table: "transaction_predictions",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_transaction_predictions_pattern_id",
                table: "transaction_predictions",
                column: "pattern_id");

            // Create prediction_feedback table
            migrationBuilder.CreateTable(
                name: "prediction_feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    prediction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    feedback_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prediction_feedback", x => x.id);
                    table.ForeignKey(
                        name: "FK_prediction_feedback_transaction_predictions_prediction_id",
                        column: x => x.prediction_id,
                        principalTable: "transaction_predictions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_prediction_feedback_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for prediction_feedback
            migrationBuilder.CreateIndex(
                name: "ix_prediction_feedback_user_created",
                table: "prediction_feedback",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_prediction_feedback_prediction_id",
                table: "prediction_feedback",
                column: "prediction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "prediction_feedback");

            migrationBuilder.DropTable(
                name: "transaction_predictions");

            migrationBuilder.DropTable(
                name: "expense_patterns");
        }
    }
}
