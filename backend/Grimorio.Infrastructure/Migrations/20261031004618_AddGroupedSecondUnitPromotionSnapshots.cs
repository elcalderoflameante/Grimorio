using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupedSecondUnitPromotionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PromotionCardDiscountAmount",
                schema: "pos",
                table: "OrderItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PromotionCashDiscountAmount",
                schema: "pos",
                table: "OrderItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PromotionGroupId",
                schema: "pos",
                table: "OrderItems",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PromotionCardDiscountAmount",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionCashDiscountAmount",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionGroupId",
                schema: "pos",
                table: "OrderItems");
        }
    }
}
