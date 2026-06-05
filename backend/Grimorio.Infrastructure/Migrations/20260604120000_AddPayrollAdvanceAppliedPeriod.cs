using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260604120000_AddPayrollAdvanceAppliedPeriod")]
    public partial class AddPayrollAdvanceAppliedPeriod : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollAdvances_BranchId_EmployeeId_Date",
                schema: "payroll",
                table: "PayrollAdvances");

            migrationBuilder.AddColumn<int>(
                name: "PayrollMonth",
                schema: "payroll",
                table: "PayrollAdvances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PayrollYear",
                schema: "payroll",
                table: "PayrollAdvances",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE payroll."PayrollAdvances"
                SET "PayrollYear" = EXTRACT(YEAR FROM "Date")::integer,
                    "PayrollMonth" = EXTRACT(MONTH FROM "Date")::integer
                WHERE "PayrollYear" = 0 OR "PayrollMonth" = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvances_BranchId_EmployeeId_PayrollYear_PayrollMonth",
                schema: "payroll",
                table: "PayrollAdvances",
                columns: new[] { "BranchId", "EmployeeId", "PayrollYear", "PayrollMonth" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PayrollAdvances_BranchId_EmployeeId_PayrollYear_PayrollMonth",
                schema: "payroll",
                table: "PayrollAdvances");

            migrationBuilder.DropColumn(
                name: "PayrollMonth",
                schema: "payroll",
                table: "PayrollAdvances");

            migrationBuilder.DropColumn(
                name: "PayrollYear",
                schema: "payroll",
                table: "PayrollAdvances");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvances_BranchId_EmployeeId_Date",
                schema: "payroll",
                table: "PayrollAdvances",
                columns: new[] { "BranchId", "EmployeeId", "Date" },
                filter: "\"IsDeleted\" = false");
        }
    }
}
