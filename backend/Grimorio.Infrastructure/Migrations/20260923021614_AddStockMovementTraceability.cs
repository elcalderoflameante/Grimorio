using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMovementTraceability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderPaymentItemId",
                schema: "inv",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseItemId",
                schema: "inv",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StockReservationId",
                schema: "inv",
                table: "StockMovements",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_OrderPaymentItemId",
                schema: "inv",
                table: "StockMovements",
                column: "OrderPaymentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PurchaseItemId",
                schema: "inv",
                table: "StockMovements",
                column: "PurchaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_StockReservationId",
                schema: "inv",
                table: "StockMovements",
                column: "StockReservationId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_OrderPaymentItems_OrderPaymentItemId",
                schema: "inv",
                table: "StockMovements",
                column: "OrderPaymentItemId",
                principalSchema: "billing",
                principalTable: "OrderPaymentItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_PurchaseItems_PurchaseItemId",
                schema: "inv",
                table: "StockMovements",
                column: "PurchaseItemId",
                principalSchema: "purchases",
                principalTable: "PurchaseItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_StockReservations_StockReservationId",
                schema: "inv",
                table: "StockMovements",
                column: "StockReservationId",
                principalSchema: "inv",
                principalTable: "StockReservations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_OrderPaymentItems_OrderPaymentItemId",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_PurchaseItems_PurchaseItemId",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_StockReservations_StockReservationId",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_OrderPaymentItemId",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_PurchaseItemId",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_StockReservationId",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "OrderPaymentItemId",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "PurchaseItemId",
                schema: "inv",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "StockReservationId",
                schema: "inv",
                table: "StockMovements");
        }
    }
}
