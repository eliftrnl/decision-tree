using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DecisionTree.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveColumnCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. First update null ColumnName values and make column non-nullable
            migrationBuilder.UpdateData(
                table: "table_column",
                keyColumn: "ColumnName",
                keyValue: null,
                column: "ColumnName",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "ColumnName",
                table: "table_column",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // 2. Create new index before dropping old one
            migrationBuilder.CreateIndex(
                name: "IX_table_column_TableId_ColumnName",
                table: "table_column",
                columns: new[] { "TableId", "ColumnName" },
                unique: true);

            // 3. Drop old index
            migrationBuilder.DropIndex(
                name: "IX_table_column_TableId_ColumnCode",
                table: "table_column");

            // 4. Finally drop ColumnCode column
            migrationBuilder.DropColumn(
                name: "ColumnCode",
                table: "table_column");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_table_column_TableId_ColumnName",
                table: "table_column");

            migrationBuilder.AlterColumn<string>(
                name: "ColumnName",
                table: "table_column",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColumnCode",
                table: "table_column",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_table_column_TableId_ColumnCode",
                table: "table_column",
                columns: new[] { "TableId", "ColumnCode" },
                unique: true);
        }
    }
}
