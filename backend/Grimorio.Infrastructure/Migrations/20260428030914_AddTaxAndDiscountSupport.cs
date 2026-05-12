using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxAndDiscountSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DiscountTotal",
                schema: "purchases",
                table: "PurchaseOrders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "purchases",
                table: "PurchaseOrders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "purchases",
                table: "PurchaseOrderItems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPct",
                schema: "purchases",
                table: "PurchaseOrderItems",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "purchases",
                table: "PurchaseOrderItems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxRateId",
                schema: "purchases",
                table: "PurchaseOrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountTotal",
                schema: "pos",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "pos",
                table: "Orders",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                schema: "pos",
                table: "OrderItems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPct",
                schema: "pos",
                table: "OrderItems",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                schema: "pos",
                table: "OrderItems",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxRateId",
                schema: "pos",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TaxRateId",
                schema: "menu",
                table: "MenuItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BranchTaxConfigs",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ruc = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    NombreComercial = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CodigoEstablecimiento = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PuntoEmision = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Ambiente = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchTaxConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    SriCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_TaxRateId",
                schema: "purchases",
                table: "PurchaseOrderItems",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_TaxRateId",
                schema: "pos",
                table: "OrderItems",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_TaxRateId",
                schema: "menu",
                table: "MenuItems",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_BranchTaxConfigs_BranchId",
                schema: "billing",
                table: "BranchTaxConfigs",
                column: "BranchId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_BranchTaxConfigs_BranchId_IsDeleted",
                schema: "billing",
                table: "BranchTaxConfigs",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_BranchTaxConfigs_IsDeleted",
                schema: "billing",
                table: "BranchTaxConfigs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_BranchId",
                schema: "billing",
                table: "TaxRates",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_BranchId_IsDefault",
                schema: "billing",
                table: "TaxRates",
                columns: new[] { "BranchId", "IsDefault" },
                filter: "\"IsDeleted\" = false AND \"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_BranchId_IsDeleted",
                schema: "billing",
                table: "TaxRates",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_BranchId_SriCode",
                schema: "billing",
                table: "TaxRates",
                columns: new[] { "BranchId", "SriCode" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_TaxRates_IsDeleted",
                schema: "billing",
                table: "TaxRates",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_MenuItems_TaxRates_TaxRateId",
                schema: "menu",
                table: "MenuItems",
                column: "TaxRateId",
                principalSchema: "billing",
                principalTable: "TaxRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_TaxRates_TaxRateId",
                schema: "pos",
                table: "OrderItems",
                column: "TaxRateId",
                principalSchema: "billing",
                principalTable: "TaxRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrderItems_TaxRates_TaxRateId",
                schema: "purchases",
                table: "PurchaseOrderItems",
                column: "TaxRateId",
                principalSchema: "billing",
                principalTable: "TaxRates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MenuItems_TaxRates_TaxRateId",
                schema: "menu",
                table: "MenuItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_TaxRates_TaxRateId",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrderItems_TaxRates_TaxRateId",
                schema: "purchases",
                table: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "BranchTaxConfigs",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "TaxRates",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrderItems_TaxRateId",
                schema: "purchases",
                table: "PurchaseOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_TaxRateId",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_MenuItems_TaxRateId",
                schema: "menu",
                table: "MenuItems");

            migrationBuilder.DropColumn(
                name: "DiscountTotal",
                schema: "purchases",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "purchases",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "purchases",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "DiscountPct",
                schema: "purchases",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "purchases",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                schema: "purchases",
                table: "PurchaseOrderItems");

            migrationBuilder.DropColumn(
                name: "DiscountTotal",
                schema: "pos",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "pos",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "DiscountPct",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "TaxRateId",
                schema: "menu",
                table: "MenuItems");
        }
    }
}
