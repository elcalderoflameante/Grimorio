using System;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260829000100_AddElectronicInvoiceContingencyEmission")]
    public partial class AddElectronicInvoiceContingencyEmission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmissionDate",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OriginalEmissionDate",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsContingencyEmission",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContingencyReason",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ContingencyUserId",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContingencyUserName",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE billing."ElectronicDocuments" d
                SET "EmissionDate" = p."PaidAt",
                    "OriginalEmissionDate" = p."PaidAt"
                FROM billing."OrderPayments" p
                WHERE d."OrderPaymentId" = p."Id"
                  AND d."EmissionDate" IS NULL
                """);

            migrationBuilder.Sql("""
                UPDATE billing."ElectronicDocuments"
                SET "EmissionDate" = "CreatedAt",
                    "OriginalEmissionDate" = "CreatedAt"
                WHERE "EmissionDate" IS NULL
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "EmissionDate",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "OriginalEmissionDate",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContingencyReason",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "ContingencyUserId",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "ContingencyUserName",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "EmissionDate",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "IsContingencyEmission",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "OriginalEmissionDate",
                schema: "billing",
                table: "ElectronicDocuments");
        }
    }
}
