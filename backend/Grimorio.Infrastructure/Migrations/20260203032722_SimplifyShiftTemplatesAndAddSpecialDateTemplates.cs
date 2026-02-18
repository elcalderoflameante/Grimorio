using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyShiftTemplatesAndAddSpecialDateTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.CreateTable(
                name: "SpecialDateTemplates",
                schema: "scheduling",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SpecialDateId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    BreakDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    LunchDuration = table.Column<TimeSpan>(type: "interval", nullable: true),
                    WorkAreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequiredCount = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SpecialDateTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpecialDateTemplates_SpecialDates_SpecialDateId",
                        column: x => x.SpecialDateId,
                        principalSchema: "scheduling",
                        principalTable: "SpecialDates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpecialDateTemplates_WorkAreas_WorkAreaId",
                        column: x => x.WorkAreaId,
                        principalSchema: "scheduling",
                        principalTable: "WorkAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpecialDateTemplates_WorkRoles_WorkRoleId",
                        column: x => x.WorkRoleId,
                        principalSchema: "scheduling",
                        principalTable: "WorkRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDateTemplates_SpecialDateId",
                schema: "scheduling",
                table: "SpecialDateTemplates",
                column: "SpecialDateId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDateTemplates_WorkAreaId",
                schema: "scheduling",
                table: "SpecialDateTemplates",
                column: "WorkAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_SpecialDateTemplates_WorkRoleId",
                schema: "scheduling",
                table: "SpecialDateTemplates",
                column: "WorkRoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpecialDateTemplates",
                schema: "scheduling");

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
    }
}
