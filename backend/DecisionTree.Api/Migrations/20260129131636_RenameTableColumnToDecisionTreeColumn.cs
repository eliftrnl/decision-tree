using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DecisionTree.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTableColumnToDecisionTreeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_table_column_decision_tree_table_TableId",
                table: "table_column");

            migrationBuilder.DropPrimaryKey(
                name: "PK_table_column",
                table: "table_column");

            migrationBuilder.RenameTable(
                name: "table_column",
                newName: "decision_tree_column");

            migrationBuilder.RenameIndex(
                name: "IX_table_column_TableId_ColumnName",
                table: "decision_tree_column",
                newName: "IX_decision_tree_column_TableId_ColumnName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_decision_tree_column",
                table: "decision_tree_column",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_decision_tree_column_decision_tree_table_TableId",
                table: "decision_tree_column",
                column: "TableId",
                principalTable: "decision_tree_table",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_decision_tree_column_decision_tree_table_TableId",
                table: "decision_tree_column");

            migrationBuilder.DropPrimaryKey(
                name: "PK_decision_tree_column",
                table: "decision_tree_column");

            migrationBuilder.RenameTable(
                name: "decision_tree_column",
                newName: "table_column");

            migrationBuilder.RenameIndex(
                name: "IX_decision_tree_column_TableId_ColumnName",
                table: "table_column",
                newName: "IX_table_column_TableId_ColumnName");

            migrationBuilder.AddPrimaryKey(
                name: "PK_table_column",
                table: "table_column",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_table_column_decision_tree_table_TableId",
                table: "table_column",
                column: "TableId",
                principalTable: "decision_tree_table",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
