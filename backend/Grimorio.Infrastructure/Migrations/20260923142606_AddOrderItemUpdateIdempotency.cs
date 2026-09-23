using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemUpdateIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UpdateIdempotencyKey",
                schema: "pos",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_BranchId_OrderId_UpdateIdempotencyKey",
                schema: "pos",
                table: "OrderItems",
                columns: new[] { "BranchId", "OrderId", "UpdateIdempotencyKey" },
                filter: "\"UpdateIdempotencyKey\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderItems_BranchId_OrderId_UpdateIdempotencyKey",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "UpdateIdempotencyKey",
                schema: "pos",
                table: "OrderItems");
        }
    }
}
