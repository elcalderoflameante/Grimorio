using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFreeDaysFromScheduleConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreeDaysOtherRoles",
                schema: "scheduling",
                table: "ScheduleConfigurations");

            migrationBuilder.DropColumn(
                name: "FreeDaysParrillero",
                schema: "scheduling",
                table: "ScheduleConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FreeDaysOtherRoles",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<int>(
                name: "FreeDaysParrillero",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }
    }
}
