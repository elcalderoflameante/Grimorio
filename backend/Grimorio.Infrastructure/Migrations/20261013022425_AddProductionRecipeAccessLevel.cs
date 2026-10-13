using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionRecipeAccessLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessLevel",
                schema: "inv",
                table: "ProductionRecipes",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Restricted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                schema: "inv",
                table: "ProductionRecipes");
        }
    }
}
