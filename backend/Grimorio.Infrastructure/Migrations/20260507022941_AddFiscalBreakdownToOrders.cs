using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFiscalBreakdownToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Ice",
                schema: "pos",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Iva15",
                schema: "pos",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableBase0",
                schema: "pos",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableBase15",
                schema: "pos",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableBaseExempt",
                schema: "pos",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ice",
                schema: "pos",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Iva15",
                schema: "pos",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxableBase0",
                schema: "pos",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxableBase15",
                schema: "pos",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxableBaseExempt",
                schema: "pos",
                table: "Orders");
        }
    }
}
