using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260605120000_UseAreaForRestaurantTableUniqueness")]
    public partial class UseAreaForRestaurantTableUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_BranchId_Code",
                schema: "pos",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "pos",
                table: "RestaurantTables");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_BranchId_Code_Area",
                schema: "pos",
                table: "RestaurantTables",
                columns: new[] { "BranchId", "Code", "Area" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"Area\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_BranchId_Code_NoArea",
                schema: "pos",
                table: "RestaurantTables",
                columns: new[] { "BranchId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"Area\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_BranchId_Code_Area",
                schema: "pos",
                table: "RestaurantTables");

            migrationBuilder.DropIndex(
                name: "IX_RestaurantTables_BranchId_Code_NoArea",
                schema: "pos",
                table: "RestaurantTables");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "pos",
                table: "RestaurantTables",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RestaurantTables_BranchId_Code",
                schema: "pos",
                table: "RestaurantTables",
                columns: new[] { "BranchId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
