using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260514160000_OptimizeRestaurantHotPaths")]
    public partial class OptimizeRestaurantHotPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId_PaidAt_Status_CreatedAt",
                schema: "pos",
                table: "Orders",
                columns: new[] { "BranchId", "PaidAt", "Status", "CreatedAt" },
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_BranchId_PaidAt_Status_CreatedAt",
                schema: "pos",
                table: "Orders");
        }
    }
}
