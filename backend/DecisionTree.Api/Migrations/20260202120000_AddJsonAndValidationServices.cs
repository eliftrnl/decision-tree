#nullable disable
using Microsoft.EntityFrameworkCore.Migrations;

namespace DecisionTree.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddJsonBuilderAndValidationServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ColumnDirection",
                table: "decision_tree_column",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Migration note:
            // This migration adds support for JsonBuilderService and ValidationService
            // No database schema changes required - services work with existing data
            // SchemaVersion continues to track metadata changes
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnDirection",
                table: "decision_tree_column");
        }
    }
}
