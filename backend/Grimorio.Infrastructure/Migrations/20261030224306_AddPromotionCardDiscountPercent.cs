using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPromotionCardDiscountPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CardDiscountPercent",
                schema: "pos",
                table: "Promotions",
                type: "numeric(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardDiscountPercent",
                schema: "pos",
                table: "Promotions");
        }
    }
}
