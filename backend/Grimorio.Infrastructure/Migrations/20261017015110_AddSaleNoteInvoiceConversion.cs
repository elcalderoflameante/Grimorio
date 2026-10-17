using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleNoteInvoiceConversion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NoteConversionReason",
                schema: "billing",
                table: "OrderPayments",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NoteConvertedAt",
                schema: "billing",
                table: "OrderPayments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NoteConvertedBy",
                schema: "billing",
                table: "OrderPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "NoteOriginalCustomerId",
                schema: "billing",
                table: "OrderPayments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NoteConversionReason",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "NoteConvertedAt",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "NoteConvertedBy",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "NoteOriginalCustomerId",
                schema: "billing",
                table: "OrderPayments");
        }
    }
}
