using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedScheduleConfigurationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HoursMondayThursday",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "HoursFridaySaturday",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "HoursSunday",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "MinStaffCocina",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "MinStaffCaja",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "MinStaffMesas",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "MinStaffBar",
                schema: "scheduling",
                table: "ScheduleConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "HoursMondayThursday",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "numeric",
                nullable: false,
                defaultValue: 8.5m);

            migrationBuilder.AddColumn<decimal>(
                name: "HoursFridaySaturday",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "numeric",
                nullable: false,
                defaultValue: 12.5m);

            migrationBuilder.AddColumn<decimal>(
                name: "HoursSunday",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "numeric",
                nullable: false,
                defaultValue: 10m);

            migrationBuilder.AddColumn<int>(
                name: "MinStaffCocina",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "MinStaffCaja",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "MinStaffMesas",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "MinStaffBar",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }
    }
}
