using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DecisionTree.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTableCodeAndColumnType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Önce ColumnType'ı sil (bağımlılık yok)
            migrationBuilder.DropColumn(
                name: "ColumnType",
                table: "table_column");

            // 2. TableName'i zorunlu yap ve boş olanları doldur
            migrationBuilder.Sql("UPDATE decision_tree_table SET TableName = TableCode WHERE TableName IS NULL OR TableName = ''");
            
            migrationBuilder.AlterColumn<string>(
                name: "TableName",
                table: "decision_tree_table",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // 3. Yeni index oluştur
            migrationBuilder.CreateIndex(
                name: "IX_decision_tree_table_DecisionTreeId_TableName",
                table: "decision_tree_table",
                columns: new[] { "DecisionTreeId", "TableName" },
                unique: true);

            // 4. Eski index ve kolonu sil
            migrationBuilder.DropIndex(
                name: "IX_decision_tree_table_DecisionTreeId_TableCode",
                table: "decision_tree_table");

            migrationBuilder.DropColumn(
                name: "TableCode",
                table: "decision_tree_table");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_decision_tree_table_DecisionTreeId_TableName",
                table: "decision_tree_table");

            migrationBuilder.AddColumn<string>(
                name: "ColumnType",
                table: "table_column",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "TableName",
                table: "decision_tree_table",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TableCode",
                table: "decision_tree_table",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_decision_tree_table_DecisionTreeId_TableCode",
                table: "decision_tree_table",
                columns: new[] { "DecisionTreeId", "TableCode" },
                unique: true);
        }
    }
}
