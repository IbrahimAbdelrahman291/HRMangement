using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class EditDiscounts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discount",
                table: "MonthlyEmployeeData");

            migrationBuilder.DropColumn(
                name: "ReasonOfDiscount",
                table: "MonthlyEmployeeData");

            migrationBuilder.CreateTable(
                name: "Discounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyEmployeeDataId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReasonOfDiscount = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Discounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Discounts_MonthlyEmployeeData_MonthlyEmployeeDataId",
                        column: x => x.MonthlyEmployeeDataId,
                        principalTable: "MonthlyEmployeeData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Discounts_MonthlyEmployeeDataId",
                table: "Discounts",
                column: "MonthlyEmployeeDataId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Discounts");

            migrationBuilder.AddColumn<double>(
                name: "Discount",
                table: "MonthlyEmployeeData",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonOfDiscount",
                table: "MonthlyEmployeeData",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
