using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "IdempotencyKey",
                schema: "billing",
                table: "OrderPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_BranchId_IdempotencyKey",
                schema: "billing",
                table: "OrderPayments",
                columns: new[] { "BranchId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderPayments_BranchId_IdempotencyKey",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "billing",
                table: "OrderPayments");
        }
    }
}
