using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenamePurchasesSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "purchases");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                schema: "compras",
                newName: "Suppliers",
                newSchema: "purchases");

            migrationBuilder.RenameTable(
                name: "PurchaseOrders",
                schema: "compras",
                newName: "PurchaseOrders",
                newSchema: "purchases");

            migrationBuilder.RenameTable(
                name: "PurchaseOrderItems",
                schema: "compras",
                newName: "PurchaseOrderItems",
                newSchema: "purchases");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "compras");

            migrationBuilder.RenameTable(
                name: "Suppliers",
                schema: "purchases",
                newName: "Suppliers",
                newSchema: "compras");

            migrationBuilder.RenameTable(
                name: "PurchaseOrders",
                schema: "purchases",
                newName: "PurchaseOrders",
                newSchema: "compras");

            migrationBuilder.RenameTable(
                name: "PurchaseOrderItems",
                schema: "purchases",
                newName: "PurchaseOrderItems",
                newSchema: "compras");
        }
    }
}
