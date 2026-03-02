using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdentificationNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdentificationNumber",
                schema: "organization",
                table: "Branches",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentificationNumber",
                schema: "organization",
                table: "Branches");
        }
    }
}
