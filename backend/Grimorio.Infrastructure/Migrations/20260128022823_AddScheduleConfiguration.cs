using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduleConfigurations",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinHoursPerMonth = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 160m),
                    MaxHoursPerMonth = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 220m),
                    HoursMondayThursday = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 8.5m),
                    HoursFridaySaturday = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 12.5m),
                    HoursSunday = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false, defaultValue: 10m),
                    FreeDaysParrillero = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    FreeDaysOtherRoles = table.Column<int>(type: "integer", nullable: false, defaultValue: 6),
                    MinStaffCocina = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    MinStaffCaja = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    MinStaffMesas = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    MinStaffBar = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
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
                    table.PrimaryKey("PK_ScheduleConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleConfigurations_BranchId",
                schema: "scheduling",
                table: "ScheduleConfigurations",
                column: "BranchId",
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleConfigurations",
                schema: "scheduling");
        }
    }
}
