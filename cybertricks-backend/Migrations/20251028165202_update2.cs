using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ct.backend.Migrations
{
    /// <inheritdoc />
    public partial class update2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Stores",
                type: "decimal(12,9)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Stores",
                type: "decimal(12,9)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(9,6)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Rooms",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GridCols",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridH",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridRows",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridW",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridX",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GridY",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ColIndex",
                table: "Machines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                table: "Machines",
                type: "longtext",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RowIndex",
                table: "Machines",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GridCols",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GridH",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GridRows",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GridW",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GridX",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "GridY",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "ColIndex",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Label",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "RowIndex",
                table: "Machines");

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "Stores",
                type: "decimal(9,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,9)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "Stores",
                type: "decimal(9,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(12,9)",
                oldNullable: true);
        }
    }
}
