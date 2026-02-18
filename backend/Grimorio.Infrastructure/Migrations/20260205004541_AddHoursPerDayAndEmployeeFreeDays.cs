using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHoursPerDayAndEmployeeFreeDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyHoursTarget",
                schema: "scheduling",
                table: "WorkRoles");

            migrationBuilder.DropColumn(
                name: "FreeDaysPerMonth",
                schema: "scheduling",
                table: "WorkRoles");

            migrationBuilder.AddColumn<decimal>(
                name: "HoursPerDay",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "FreeDaysPerMonth",
                schema: "organization",
                table: "Employees",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoursPerDay",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "FreeDaysPerMonth",
                schema: "organization",
                table: "Employees");

            migrationBuilder.AddColumn<decimal>(
                name: "DailyHoursTarget",
                schema: "scheduling",
                table: "WorkRoles",
                type: "numeric(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 8.0m);

            migrationBuilder.AddColumn<int>(
                name: "FreeDaysPerMonth",
                schema: "scheduling",
                table: "WorkRoles",
                type: "integer",
                nullable: false,
                defaultValue: 6);
        }
    }
}
