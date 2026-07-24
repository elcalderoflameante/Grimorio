using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementCosts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                schema: "inv",
                table: "StockMovements",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitCost",
                schema: "inv",
                table: "StockMovements",
                type: "numeric(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalCost",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "UnitCost",
                schema: "inv",
                table: "StockMovements");
        }
    }
}
