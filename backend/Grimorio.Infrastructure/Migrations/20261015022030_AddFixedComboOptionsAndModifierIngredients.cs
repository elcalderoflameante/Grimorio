using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFixedComboOptionsAndModifierIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OptionsSummary",
                schema: "pos",
                table: "OrderItemPreparationTasks",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalIngredientsJson",
                schema: "pos",
                table: "OrderItemModifierSelections",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalIngredientsJson",
                schema: "menu",
                table: "MenuItemModifierOptions",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "ModifierSelectionsJson",
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
                name: "OptionsSummary",
                schema: "pos",
                table: "OrderItemPreparationTasks");

            migrationBuilder.DropColumn(
                name: "AdditionalIngredientsJson",
                schema: "pos",
                table: "OrderItemModifierSelections");

            migrationBuilder.DropColumn(
                name: "AdditionalIngredientsJson",
                schema: "menu",
                table: "MenuItemModifierOptions");

            migrationBuilder.DropColumn(
                name: "ModifierSelectionsJson",
                schema: "menu",
                table: "MenuComboComponents");
        }
    }
}
