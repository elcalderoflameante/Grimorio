using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseNonInventoryLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                schema: "purchases",
                table: "PurchaseItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "ArticleId",
                schema: "purchases",
                table: "PurchaseItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedCost",
                schema: "purchases",
                table: "PurchaseItems",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CostTreatment",
                schema: "purchases",
                table: "PurchaseItems",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM purchases."PurchaseItems"
                WHERE "ArticleId" IS NULL OR "UnitId" IS NULL;
                """);

            migrationBuilder.DropColumn(
                name: "AllocatedCost",
                schema: "purchases",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "CostTreatment",
                schema: "purchases",
                table: "PurchaseItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "UnitId",
                schema: "purchases",
                table: "PurchaseItems",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "ArticleId",
                schema: "purchases",
                table: "PurchaseItems",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
