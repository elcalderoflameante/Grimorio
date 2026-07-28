using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuCategoryCostCenter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CostCenterId",
                schema: "menu",
                table: "MenuCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_BranchId_CostCenterId",
                schema: "menu",
                table: "MenuCategories",
                columns: new[] { "BranchId", "CostCenterId" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_CostCenterId",
                schema: "menu",
                table: "MenuCategories",
                column: "CostCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuCategories_CostCenters_CostCenterId",
                schema: "menu",
                table: "MenuCategories",
                column: "CostCenterId",
                principalSchema: "finance",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuCategories_CostCenters_CostCenterId",
                schema: "menu",
                table: "MenuCategories");

            migrationBuilder.DropIndex(
                name: "IX_MenuCategories_BranchId_CostCenterId",
                schema: "menu",
                table: "MenuCategories");

            migrationBuilder.DropIndex(
                name: "IX_MenuCategories_CostCenterId",
                schema: "menu",
                table: "MenuCategories");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                schema: "menu",
                table: "MenuCategories");
        }
    }
}
