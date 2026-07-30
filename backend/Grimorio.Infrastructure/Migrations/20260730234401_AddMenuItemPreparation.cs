using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemPreparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuItemPreparations",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
                    Yield = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Temperature = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Presentation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemPreparations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemPreparations_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalSchema: "menu",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemPreparationSteps",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MenuItemPreparationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
                    Temperature = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    IsCritical = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemPreparationSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemPreparationSteps_MenuItemPreparations_MenuItemPrepa~",
                        column: x => x.MenuItemPreparationId,
                        principalSchema: "menu",
                        principalTable: "MenuItemPreparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparations_BranchId",
                schema: "menu",
                table: "MenuItemPreparations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparations_BranchId_IsDeleted",
                schema: "menu",
                table: "MenuItemPreparations",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparations_BranchId_MenuItemId",
                schema: "menu",
                table: "MenuItemPreparations",
                columns: new[] { "BranchId", "MenuItemId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparations_IsDeleted",
                schema: "menu",
                table: "MenuItemPreparations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparations_MenuItemId",
                schema: "menu",
                table: "MenuItemPreparations",
                column: "MenuItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparationSteps_BranchId",
                schema: "menu",
                table: "MenuItemPreparationSteps",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparationSteps_BranchId_IsDeleted",
                schema: "menu",
                table: "MenuItemPreparationSteps",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparationSteps_BranchId_MenuItemPreparationId_Ste~",
                schema: "menu",
                table: "MenuItemPreparationSteps",
                columns: new[] { "BranchId", "MenuItemPreparationId", "StepNumber" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparationSteps_IsDeleted",
                schema: "menu",
                table: "MenuItemPreparationSteps",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemPreparationSteps_MenuItemPreparationId",
                schema: "menu",
                table: "MenuItemPreparationSteps",
                column: "MenuItemPreparationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItemPreparationSteps",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "MenuItemPreparations",
                schema: "menu");
        }
    }
}
