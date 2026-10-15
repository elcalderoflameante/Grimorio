using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExplicitInvoiceAmountSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalSubtotal",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Tax",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalBeforeDiscount",
                schema: "billing",
                table: "ElectronicDocuments",
                type: "numeric(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Discount",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "OriginalSubtotal",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "Tax",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "SubtotalBeforeDiscount",
                schema: "billing",
                table: "ElectronicDocuments");
        }
    }
}
