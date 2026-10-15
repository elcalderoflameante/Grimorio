using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceSavingsSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OriginalTotal",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromotionName",
                schema: "billing",
                table: "OrderPaymentItems",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalTotal",
                schema: "billing",
                table: "OrderPaymentItems");

            migrationBuilder.DropColumn(
                name: "PromotionName",
                schema: "billing",
                table: "OrderPaymentItems");
        }
    }
}
