using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DecisionTree.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertEnumsToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Önce string değerleri int'e map et
            migrationBuilder.Sql(@"
                UPDATE decision_tree SET StatusCode = CASE StatusCode
                    WHEN 'Active' THEN 1
                    WHEN 'Passive' THEN 2
                    ELSE 1
                END WHERE StatusCode IN ('Active', 'Passive');

                UPDATE decision_tree_table SET 
                    StatusCode = CASE StatusCode
                        WHEN 'Active' THEN 1
                        WHEN 'Passive' THEN 2
                        ELSE 1
                    END,
                    Direction = CASE Direction
                        WHEN 'Input' THEN 1
                        WHEN 'Output' THEN 2
                        ELSE 1
                    END
                WHERE StatusCode IN ('Active', 'Passive') OR Direction IN ('Input', 'Output');

                UPDATE table_column SET 
                    StatusCode = CASE StatusCode
                        WHEN 'Active' THEN 1
                        WHEN 'Passive' THEN 2
                        ELSE 1
                    END,
                    DataType = CASE DataType
                        WHEN 'String' THEN 1
                        WHEN 'Int' THEN 2
                        WHEN 'Decimal' THEN 3
                        WHEN 'Date' THEN 4
                        WHEN 'Boolean' THEN 5
                        ELSE 1
                    END
                WHERE StatusCode IN ('Active', 'Passive') OR DataType IN ('String', 'Int', 'Decimal', 'Date', 'Boolean');
            ");

            // Sonra kolon tiplerini değiştir
            migrationBuilder.AlterColumn<int>(
                name: "StatusCode",
                table: "table_column",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "DataType",
                table: "table_column",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "StatusCode",
                table: "decision_tree_table",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "Direction",
                table: "decision_tree_table",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "StatusCode",
                table: "decision_tree",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(30)",
                oldMaxLength: 30)
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "StatusCode",
                table: "table_column",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "DataType",
                table: "table_column",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "StatusCode",
                table: "decision_tree_table",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Direction",
                table: "decision_tree_table",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "StatusCode",
                table: "decision_tree",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
