using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ShortenAutomaticMenuItemCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE menu."MenuItems"
                SET "InternalCode" = 'M-' || UPPER(SUBSTRING(REPLACE(CAST("Id" AS text), '-', '') FROM 1 FOR 6))
                WHERE "InternalCode" = 'MENU-' || UPPER(SUBSTRING(REPLACE(CAST("Id" AS text), '-', '') FROM 1 FOR 8));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE menu."MenuItems"
                SET "InternalCode" = 'MENU-' || UPPER(SUBSTRING(REPLACE(CAST("Id" AS text), '-', '') FROM 1 FOR 8))
                WHERE "InternalCode" = 'M-' || UPPER(SUBSTRING(REPLACE(CAST("Id" AS text), '-', '') FROM 1 FOR 6));
                """);
        }
    }
}
