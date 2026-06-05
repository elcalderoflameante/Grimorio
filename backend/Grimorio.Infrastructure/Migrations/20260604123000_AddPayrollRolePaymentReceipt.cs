using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260604123000_AddPayrollRolePaymentReceipt")]
    public partial class AddPayrollRolePaymentReceipt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PaymentReceiptContent",
                schema: "payroll",
                table: "PayrollRoleHeaders",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReceiptContentType",
                schema: "payroll",
                table: "PayrollRoleHeaders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReceiptFileName",
                schema: "payroll",
                table: "PayrollRoleHeaders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentReceiptContent",
                schema: "payroll",
                table: "PayrollRoleHeaders");

            migrationBuilder.DropColumn(
                name: "PaymentReceiptContentType",
                schema: "payroll",
                table: "PayrollRoleHeaders");

            migrationBuilder.DropColumn(
                name: "PaymentReceiptFileName",
                schema: "payroll",
                table: "PayrollRoleHeaders");
        }
    }
}
