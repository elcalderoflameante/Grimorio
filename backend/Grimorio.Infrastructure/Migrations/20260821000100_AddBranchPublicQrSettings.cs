using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260821000100_AddBranchPublicQrSettings")]
    public partial class AddBranchPublicQrSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PublicMenuEnabled",
                schema: "organization",
                table: "Branches",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "PublicOrderingEnabled",
                schema: "organization",
                table: "Branches",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicMenuEnabled",
                schema: "organization",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "PublicOrderingEnabled",
                schema: "organization",
                table: "Branches");
        }
    }
}
