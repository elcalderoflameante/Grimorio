using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260817165000_AddPromotionPaymentPolicy")]
    public partial class AddPromotionPaymentPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CardPrice",
                schema: "pos",
                table: "Promotions",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentPolicy",
                schema: "pos",
                table: "Promotions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "AnyPayment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardPrice",
                schema: "pos",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "PaymentPolicy",
                schema: "pos",
                table: "Promotions");
        }
    }
}
