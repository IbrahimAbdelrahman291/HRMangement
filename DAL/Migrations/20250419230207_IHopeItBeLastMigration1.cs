using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class IHopeItBeLastMigration1 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RequestForForgetCloseShift_Employees_EmployeeId",
                table: "RequestForForgetCloseShift");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RequestForForgetCloseShift",
                table: "RequestForForgetCloseShift");

            migrationBuilder.RenameTable(
                name: "RequestForForgetCloseShift",
                newName: "requests");

            migrationBuilder.RenameIndex(
                name: "IX_RequestForForgetCloseShift_EmployeeId",
                table: "requests",
                newName: "IX_requests_EmployeeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_requests",
                table: "requests",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_requests_Employees_EmployeeId",
                table: "requests",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_requests_Employees_EmployeeId",
                table: "requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_requests",
                table: "requests");

            migrationBuilder.RenameTable(
                name: "requests",
                newName: "RequestForForgetCloseShift");

            migrationBuilder.RenameIndex(
                name: "IX_requests_EmployeeId",
                table: "RequestForForgetCloseShift",
                newName: "IX_RequestForForgetCloseShift_EmployeeId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RequestForForgetCloseShift",
                table: "RequestForForgetCloseShift",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RequestForForgetCloseShift_Employees_EmployeeId",
                table: "RequestForForgetCloseShift",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
