using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSizeDependentModifiersAndComboChoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuantityRulesJson",
                schema: "menu",
                table: "MenuItemModifierGroups",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<Guid>(
                name: "QuantitySourceGroupId",
                schema: "menu",
                table: "MenuItemModifierGroups",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectableModifierGroupIdsJson",
                schema: "menu",
                table: "MenuComboComponents",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuantityRulesJson",
                schema: "menu",
                table: "MenuItemModifierGroups");

            migrationBuilder.DropColumn(
                name: "QuantitySourceGroupId",
                schema: "menu",
                table: "MenuItemModifierGroups");

            migrationBuilder.DropColumn(
                name: "SelectableModifierGroupIdsJson",
                schema: "menu",
                table: "MenuComboComponents");
        }
    }
}
