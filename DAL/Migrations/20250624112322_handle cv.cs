using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DAL.Migrations
{
    public partial class handlecv : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employeeBranches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employeeBranches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employeeBranches_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluationCriterias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluationCriterias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "quarterlyEvaluations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Quarter = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quarterlyEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_quarterlyEvaluations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "evaluationResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuarterlyEvaluationId = table.Column<int>(type: "int", nullable: false),
                    EvaluationCriteriaId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_evaluationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_evaluationResults_evaluationCriterias_EvaluationCriteriaId",
                        column: x => x.EvaluationCriteriaId,
                        principalTable: "evaluationCriterias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_evaluationResults_quarterlyEvaluations_QuarterlyEvaluationId",
                        column: x => x.QuarterlyEvaluationId,
                        principalTable: "quarterlyEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employeeBranches_EmployeeId",
                table: "employeeBranches",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluationResults_EvaluationCriteriaId",
                table: "evaluationResults",
                column: "EvaluationCriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_evaluationResults_QuarterlyEvaluationId",
                table: "evaluationResults",
                column: "QuarterlyEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_quarterlyEvaluations_EmployeeId",
                table: "quarterlyEvaluations",
                column: "EmployeeId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employeeBranches");

            migrationBuilder.DropTable(
                name: "evaluationResults");

            migrationBuilder.DropTable(
                name: "evaluationCriterias");

            migrationBuilder.DropTable(
                name: "quarterlyEvaluations");
        }
    }
}
