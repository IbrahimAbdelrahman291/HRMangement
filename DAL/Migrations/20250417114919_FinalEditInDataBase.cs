using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class FinalEditInDataBase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Target",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalBorrows",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalBouns",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalDiscounts",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "month",
                table: "Discounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "year",
                table: "Discounts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "month",
                table: "Bounss",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "year",
                table: "Bounss",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "Borrows",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "year",
                table: "Borrows",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Target",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "TotalBorrows",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "TotalBouns",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "TotalDiscounts",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "month",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "year",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "month",
                table: "Bounss");

            migrationBuilder.DropColumn(
                name: "year",
                table: "Bounss");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "Borrows");

            migrationBuilder.DropColumn(
                name: "year",
                table: "Borrows");
        }
    }
}
