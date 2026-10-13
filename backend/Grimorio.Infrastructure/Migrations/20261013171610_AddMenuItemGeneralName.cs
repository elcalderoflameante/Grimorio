using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemGeneralName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GeneralName",
                schema: "menu",
                table: "MenuItems",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE menu."MenuItems"
                SET "GeneralName" = "Name"
                WHERE "GeneralName" = '';

                UPDATE menu."MenuItems"
                SET "InternalCode" = 'MENU-' || UPPER(SUBSTRING(REPLACE(CAST("Id" AS text), '-', '') FROM 1 FOR 8))
                WHERE "InternalCode" IS NULL OR BTRIM("InternalCode") = '';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GeneralName",
                schema: "menu",
                table: "MenuItems");
        }
    }
}
