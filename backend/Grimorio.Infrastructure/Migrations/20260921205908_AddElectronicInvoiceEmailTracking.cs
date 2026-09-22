using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddElectronicInvoiceEmailTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailErrorMessage",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailRecipient",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailRetryCount",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmailSentAt",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailStatus",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Unknown");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailErrorMessage",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "EmailRecipient",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "EmailRetryCount",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "EmailSentAt",
                schema: "billing",
                table: "ElectronicDocuments");

            migrationBuilder.DropColumn(
                name: "EmailStatus",
                schema: "billing",
                table: "ElectronicDocuments");
        }
    }
}
