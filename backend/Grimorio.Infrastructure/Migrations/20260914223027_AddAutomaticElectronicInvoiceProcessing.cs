using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomaticElectronicInvoiceProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ElectronicDocuments_OrderPaymentId",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "numeric(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)");

            migrationBuilder.AddColumn<string>(
                name: "ItemCode",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ItemName",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRatePercentage",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "numeric(8,4)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxRateSriCode",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "timestamp with time zone",
                nullable: true);

            // Versiones anteriores permitian varios intentos para un mismo cobro.
            // Conservar el documento mas relevante y mantener los demas para auditoria como eliminados logicamente.
            migrationBuilder.Sql("""
                WITH ranked_documents AS (
                    SELECT "Id",
                           ROW_NUMBER() OVER (
                               PARTITION BY "OrderPaymentId"
                               ORDER BY CASE "Status"
                                            WHEN 3 THEN 0
                                            WHEN 2 THEN 1
                                            WHEN 1 THEN 2
                                            WHEN 4 THEN 3
                                            ELSE 4
                                        END,
                                        "CreatedAt" DESC
                           ) AS row_number
                    FROM billing."ElectronicDocuments"
                    WHERE "IsDeleted" = false
                )
                UPDATE billing."ElectronicDocuments" AS document
                SET "IsDeleted" = true,
                    "UpdatedAt" = NOW()
                FROM ranked_documents
                WHERE document."Id" = ranked_documents."Id"
                  AND ranked_documents.row_number > 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_OrderPaymentId",
                schema: "billing",
                table: "ElectronicDocuments",
                column: "OrderPaymentId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_Status_ProcessingStartedAt",
                schema: "billing",
                table: "ElectronicDocuments",
                columns: new[] { "Status", "ProcessingStartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ElectronicDocuments_OrderPaymentId",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ElectronicDocuments_Status_ProcessingStartedAt",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "ItemCode",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "ItemName",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "TaxRatePercentage",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "TaxRateSriCode",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "numeric(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_OrderPaymentId",
                schema: "billing",
                table: "ElectronicDocuments",
                column: "OrderPaymentId");
        }
    }
}
