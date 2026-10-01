using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemSaleVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SaleVisibility",
                schema: "menu",
                table: "MenuItems",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "MenuAndPromotions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SaleVisibility",
                schema: "menu",
                table: "MenuItems");
        }
    }
}
