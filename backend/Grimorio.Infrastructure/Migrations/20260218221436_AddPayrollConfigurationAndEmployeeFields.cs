using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollConfigurationAndEmployeeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payroll");

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                schema: "organization",
                table: "Employees",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BaseSalary",
                schema: "organization",
                table: "Employees",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "DecimoFourthMonthly",
                schema: "organization",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "DecimoThirdMonthly",
                schema: "organization",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReserveFundMonthly",
                schema: "organization",
                table: "Employees",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "EmployeeConsumptions",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeConsumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeConsumptions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAdjustments",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Hours = table.Column<decimal>(type: "numeric(6,2)", precision: 6, scale: 2, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAdvances",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdvances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAdvances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollConfigurations",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    IessEmployeeRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IessEmployerRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    IncomeTaxRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    OvertimeRate50 = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    OvertimeRate100 = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DecimoThirdRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    DecimoFourthRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ReserveFundRate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MonthlyHours = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeConsumptions_BranchId_EmployeeId_Date",
                schema: "payroll",
                table: "EmployeeConsumptions",
                columns: new[] { "BranchId", "EmployeeId", "Date" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeConsumptions_EmployeeId",
                schema: "payroll",
                table: "EmployeeConsumptions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_BranchId_EmployeeId_Date",
                schema: "payroll",
                table: "PayrollAdjustments",
                columns: new[] { "BranchId", "EmployeeId", "Date" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_EmployeeId",
                schema: "payroll",
                table: "PayrollAdjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvances_BranchId_EmployeeId_Date",
                schema: "payroll",
                table: "PayrollAdvances",
                columns: new[] { "BranchId", "EmployeeId", "Date" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdvances_EmployeeId",
                schema: "payroll",
                table: "PayrollAdvances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollConfigurations_BranchId",
                schema: "payroll",
                table: "PayrollConfigurations",
                column: "BranchId",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeConsumptions",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollAdjustments",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollAdvances",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollConfigurations",
                schema: "payroll");

            migrationBuilder.DropColumn(
                name: "BankAccount",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BaseSalary",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DecimoFourthMonthly",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DecimoThirdMonthly",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "ReserveFundMonthly",
                schema: "organization",
                table: "Employees");
        }
    }
}
