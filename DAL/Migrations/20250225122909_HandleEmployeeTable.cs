using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class HandleEmployeeTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Borrow",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BorrowBudget",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Bouns",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ContractDiscount",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Hours",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Insurances",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Inventorydeficit",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "NetSalary",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Penalties",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ReasonOfDiscount",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "SalaryPerHour",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TotalSalary",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "Pharmacy",
                table: "Employees",
                newName: "BranchName");

            migrationBuilder.CreateTable(
                name: "MonthlyEmployeeData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Hours = table.Column<double>(type: "float", nullable: true),
                    HoursOverTime = table.Column<double>(type: "float", nullable: true),
                    SalaryPerHour = table.Column<double>(type: "float", nullable: true),
                    TotalSalary = table.Column<double>(type: "float", nullable: true),
                    Borrow = table.Column<double>(type: "float", nullable: true),
                    Inventorydeficit = table.Column<double>(type: "float", nullable: true),
                    BorrowBudget = table.Column<double>(type: "float", nullable: true),
                    Discount = table.Column<double>(type: "float", nullable: true),
                    ReasonOfDiscount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Bouns = table.Column<double>(type: "float", nullable: true),
                    ReasonOfBouns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Holidaies = table.Column<int>(type: "int", nullable: true),
                    DatesOfHolidaies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NetSalary = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyEmployeeData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyEmployeeData_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyEmployeeData_EmployeeId",
                table: "MonthlyEmployeeData",
                column: "EmployeeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyEmployeeData");

            migrationBuilder.RenameColumn(
                name: "BranchName",
                table: "Employees",
                newName: "Pharmacy");

            migrationBuilder.AddColumn<double>(
                name: "Borrow",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BorrowBudget",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Bouns",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ContractDiscount",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Hours",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Insurances",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Inventorydeficit",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NetSalary",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Penalties",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonOfDiscount",
                table: "Employees",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SalaryPerHour",
                table: "Employees",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TotalSalary",
                table: "Employees",
                type: "float",
                nullable: true);
        }
    }
}
