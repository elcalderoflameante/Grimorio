using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyShiftTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSpecialDay",
                schema: "scheduling",
                table: "ShiftTemplates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Month",
                schema: "scheduling",
                table: "ShiftTemplates",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Year",
                schema: "scheduling",
                table: "ShiftTemplates",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSpecialDay",
                schema: "scheduling",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "Month",
                schema: "scheduling",
                table: "ShiftTemplates");

            migrationBuilder.DropColumn(
                name: "Year",
                schema: "scheduling",
                table: "ShiftTemplates");
        }
    }
}
