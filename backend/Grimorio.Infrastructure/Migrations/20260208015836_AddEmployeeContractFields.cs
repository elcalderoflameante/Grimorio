using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeContractFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxHoursPerMonth",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "MinHoursPerMonth",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.AlterColumn<int>(
                name: "FreeDaysPerMonth",
                schema: "organization",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 6,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ContractType",
                schema: "organization",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyMaxHours",
                schema: "organization",
                table: "Employees",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 40m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyMinHours",
                schema: "organization",
                table: "Employees",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 40m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContractType",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WeeklyMaxHours",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "WeeklyMinHours",
                schema: "organization",
                table: "Employees");

            migrationBuilder.AddColumn<decimal>(
                name: "MaxHoursPerMonth",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 220m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinHoursPerMonth",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 160m);

            migrationBuilder.AlterColumn<int>(
                name: "FreeDaysPerMonth",
                schema: "organization",
                table: "Employees",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 6);
        }
    }
}
