using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DecisionTree.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDataEntryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DecisionTreeId",
                table: "decision_tree_data",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RowIndex",
                table: "decision_tree_data",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Update existing decision_tree_data rows with DecisionTreeId from their TableId
            migrationBuilder.Sql(
                @"UPDATE decision_tree_data dtd
                  SET DecisionTreeId = (
                    SELECT DecisionTreeId FROM decision_tree_table dtt 
                    WHERE dtt.Id = dtd.TableId
                  )
                  WHERE DecisionTreeId = 0");

            migrationBuilder.CreateTable(
                name: "column_value_mapping",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TableColumnId = table.Column<int>(type: "int", nullable: false),
                    OldPosition = table.Column<int>(type: "int", nullable: false),
                    NewPosition = table.Column<int>(type: "int", nullable: false),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_column_value_mapping", x => x.Id);
                    table.ForeignKey(
                        name: "FK_column_value_mapping_decision_tree_column_TableColumnId",
                        column: x => x.TableColumnId,
                        principalTable: "decision_tree_column",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "validation_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DecisionTreeId = table.Column<int>(type: "int", nullable: false),
                    TableId = table.Column<int>(type: "int", nullable: true),
                    RowIndex = table.Column<int>(type: "int", nullable: true),
                    ColumnName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorType = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LoggedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_validation_log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_validation_log_decision_tree_DecisionTreeId",
                        column: x => x.DecisionTreeId,
                        principalTable: "decision_tree",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_validation_log_decision_tree_table_TableId",
                        column: x => x.TableId,
                        principalTable: "decision_tree_table",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_decision_tree_data_DecisionTreeId",
                table: "decision_tree_data",
                column: "DecisionTreeId");

            migrationBuilder.CreateIndex(
                name: "IX_column_value_mapping_TableColumnId",
                table: "column_value_mapping",
                column: "TableColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_validation_log_DecisionTreeId",
                table: "validation_log",
                column: "DecisionTreeId");

            migrationBuilder.CreateIndex(
                name: "IX_validation_log_TableId",
                table: "validation_log",
                column: "TableId");

            migrationBuilder.AddForeignKey(
                name: "FK_decision_tree_data_decision_tree_DecisionTreeId",
                table: "decision_tree_data",
                column: "DecisionTreeId",
                principalTable: "decision_tree",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_decision_tree_data_decision_tree_DecisionTreeId",
                table: "decision_tree_data");

            migrationBuilder.DropTable(
                name: "column_value_mapping");

            migrationBuilder.DropTable(
                name: "validation_log");

            migrationBuilder.DropIndex(
                name: "IX_decision_tree_data_DecisionTreeId",
                table: "decision_tree_data");

            migrationBuilder.DropColumn(
                name: "DecisionTreeId",
                table: "decision_tree_data");

            migrationBuilder.DropColumn(
                name: "RowIndex",
                table: "decision_tree_data");

            migrationBuilder.AddColumn<string>(
                name: "RowCode",
                table: "decision_tree_data",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
