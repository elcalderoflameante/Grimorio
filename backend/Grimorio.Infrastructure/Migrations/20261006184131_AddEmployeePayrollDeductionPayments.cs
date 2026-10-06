using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeePayrollDeductionPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeConsumptions_BranchId_EmployeeId_Date",
                schema: "payroll",
                table: "EmployeeConsumptions");

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                schema: "billing",
                table: "PaymentMethodConfigs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentLineId",
                schema: "payroll",
                table: "EmployeeConsumptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayrollMonth",
                schema: "payroll",
                table: "EmployeeConsumptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PayrollYear",
                schema: "payroll",
                table: "EmployeeConsumptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE payroll."EmployeeConsumptions"
                SET "PayrollYear" = EXTRACT(YEAR FROM "Date")::integer,
                    "PayrollMonth" = EXTRACT(MONTH FROM "Date")::integer;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeConsumptions_BranchId_EmployeeId_PayrollYear_Payrol~",
                schema: "payroll",
                table: "EmployeeConsumptions",
                columns: new[] { "BranchId", "EmployeeId", "PayrollYear", "PayrollMonth" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeConsumptions_PaymentLineId",
                schema: "payroll",
                table: "EmployeeConsumptions",
                column: "PaymentLineId",
                unique: true,
                filter: "\"PaymentLineId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeConsumptions_PaymentLines_PaymentLineId",
                schema: "payroll",
                table: "EmployeeConsumptions",
                column: "PaymentLineId",
                principalSchema: "billing",
                principalTable: "PaymentLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeConsumptions_PaymentLines_PaymentLineId",
                schema: "payroll",
                table: "EmployeeConsumptions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeConsumptions_BranchId_EmployeeId_PayrollYear_Payrol~",
                schema: "payroll",
                table: "EmployeeConsumptions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeConsumptions_PaymentLineId",
                schema: "payroll",
                table: "EmployeeConsumptions");

            migrationBuilder.DropColumn(
                name: "Purpose",
                schema: "billing",
                table: "PaymentMethodConfigs");

            migrationBuilder.DropColumn(
                name: "PaymentLineId",
                schema: "payroll",
                table: "EmployeeConsumptions");

            migrationBuilder.DropColumn(
                name: "PayrollMonth",
                schema: "payroll",
                table: "EmployeeConsumptions");

            migrationBuilder.DropColumn(
                name: "PayrollYear",
                schema: "payroll",
                table: "EmployeeConsumptions");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeConsumptions_BranchId_EmployeeId_Date",
                schema: "payroll",
                table: "EmployeeConsumptions",
                columns: new[] { "BranchId", "EmployeeId", "Date" },
                filter: "\"IsDeleted\" = false");
        }
    }
}
