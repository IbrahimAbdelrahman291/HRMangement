using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class FinalTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Borrow",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "BorrowBudget",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "Bouns",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "DatesOfHolidaies",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "Inventorydeficit",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "ReasonOfBouns",
                table: "MonthlyEmployeeData");

            migrationBuilder.CreateTable(
                name: "Borrows",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyEmployeeDataId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBorrow = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Borrows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Borrows_MonthlyEmployeeData_MonthlyEmployeeDataId",
                        column: x => x.MonthlyEmployeeDataId,
                        principalTable: "MonthlyEmployeeData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bounss",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyEmployeeDataId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBouns = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bounss", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bounss_MonthlyEmployeeData_MonthlyEmployeeDataId",
                        column: x => x.MonthlyEmployeeDataId,
                        principalTable: "MonthlyEmployeeData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Borrows_MonthlyEmployeeDataId",
                table: "Borrows",
                column: "MonthlyEmployeeDataId");

            migrationBuilder.CreateIndex(
                name: "IX_Bounss_MonthlyEmployeeDataId",
                table: "Bounss",
                column: "MonthlyEmployeeDataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Borrows");

            migrationBuilder.DropTable(
                name: "Bounss");

            migrationBuilder.AddColumn<double>(
                name: "Borrow",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BorrowBudget",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Bouns",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatesOfHolidaies",
                table: "MonthlyEmployeeData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Inventorydeficit",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonOfBouns",
                table: "MonthlyEmployeeData",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
