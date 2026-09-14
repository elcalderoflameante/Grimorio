using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseDocumentAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfFileUrl",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XmlFileUrl",
                schema: "purchases",
                table: "Purchases",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfFileUrl",
                schema: "purchases",
                table: "Purchases");

            migrationBuilder.DropColumn(
                name: "XmlFileUrl",
                schema: "purchases",
                table: "Purchases");
        }
    }
}
