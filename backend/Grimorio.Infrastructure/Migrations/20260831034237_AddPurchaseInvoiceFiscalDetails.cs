using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseInvoiceFiscalDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessKey",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(49)",
                maxLength: 49,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AuthorizationDate",
                schema: "purchases",
                table: "Purchases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationNumber",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmissionType",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Irbpnr",
                schema: "purchases",
                table: "Purchases",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaymentAmount",
                schema: "purchases",
                table: "Purchases",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodName",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethodSriCode",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierBranchAddress",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierCommercialName",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierMatrixAddress",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SupplierObligatedAccounting",
                schema: "purchases",
                table: "Purchases",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierSpecialTaxpayerNumber",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxableBaseNotSubject",
                schema: "purchases",
                table: "Purchases",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Tip",
                schema: "purchases",
                table: "Purchases",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalDetail",
                schema: "purchases",
                table: "PurchaseItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierAuxCode",
                schema: "purchases",
                table: "PurchaseItems",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierDescription",
                schema: "purchases",
                table: "PurchaseItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplierMainCode",
                schema: "purchases",
                table: "PurchaseItems",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_BranchId_AccessKey",
                schema: "purchases",
                table: "Purchases",
                columns: new[] { "BranchId", "AccessKey" },
                unique: true,
                filter: "\"AccessKey\" IS NOT NULL AND \"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Purchases_BranchId_AccessKey",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "AccessKey",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "AuthorizationDate",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "AuthorizationNumber",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "EmissionType",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "Environment",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "Irbpnr",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaymentAmount",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaymentMethodName",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "PaymentMethodSriCode",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SupplierBranchAddress",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SupplierCommercialName",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SupplierMatrixAddress",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SupplierObligatedAccounting",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "SupplierSpecialTaxpayerNumber",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "TaxableBaseNotSubject",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "Tip",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "AdditionalDetail",
                schema: "purchases",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "SupplierAuxCode",
                schema: "purchases",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "SupplierDescription",
                schema: "purchases",
                table: "PurchaseItems");

            migrationBuilder.DropColumn(
                name: "SupplierMainCode",
                schema: "purchases",
                table: "PurchaseItems");
        }
    }
}
