using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DecisionTree.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIdentifierColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsUniqueIdentifier",
                table: "decision_tree_column",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsUniqueIdentifier",
                table: "decision_tree_column");
        }
    }
}
