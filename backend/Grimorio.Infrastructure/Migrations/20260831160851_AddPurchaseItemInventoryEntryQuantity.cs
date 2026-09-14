using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseItemInventoryEntryQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InventoryQuantity",
                schema: "purchases",
                table: "PurchaseItems",
                type: "numeric(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InventoryUnitId",
                schema: "purchases",
                table: "PurchaseItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_InventoryUnitId",
                schema: "purchases",
                table: "PurchaseItems",
                column: "InventoryUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseItems_MeasurementUnits_InventoryUnitId",
                schema: "purchases",
                table: "PurchaseItems",
                column: "InventoryUnitId",
                principalSchema: "inv",
                principalTable: "MeasurementUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseItems_MeasurementUnits_InventoryUnitId",
                schema: "purchases",
                table: "PurchaseItems");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseItems_InventoryUnitId",
                schema: "purchases",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "InventoryQuantity",
                schema: "purchases",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "InventoryUnitId",
                schema: "purchases",
                table: "PurchaseItems");
        }
    }
}
