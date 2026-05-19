using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260514173000_AddCardPaymentDetails")]
    public partial class AddCardPaymentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCard",
                schema: "billing",
                table: "PaymentMethodConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AuthorizationNumber",
                schema: "billing",
                table: "PaymentLines",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CardBankId",
                schema: "billing",
                table: "PaymentLines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardBankName",
                schema: "billing",
                table: "PaymentLines",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardBrand",
                schema: "billing",
                table: "PaymentLines",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardPaymentType",
                schema: "billing",
                table: "PaymentLines",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CardBanks",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardBanks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLines_CardBankId",
                schema: "billing",
                table: "PaymentLines",
                column: "CardBankId");

            migrationBuilder.CreateIndex(
                name: "IX_CardBanks_BranchId",
                schema: "billing",
                table: "CardBanks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CardBanks_BranchId_IsDeleted",
                schema: "billing",
                table: "CardBanks",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CardBanks_BranchId_Name",
                schema: "billing",
                table: "CardBanks",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CardBanks_BranchId_SortOrder",
                schema: "billing",
                table: "CardBanks",
                columns: new[] { "BranchId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CardBanks_IsDeleted",
                schema: "billing",
                table: "CardBanks",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentLines_CardBanks_CardBankId",
                schema: "billing",
                table: "PaymentLines",
                column: "CardBankId",
                principalSchema: "billing",
                principalTable: "CardBanks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentLines_CardBanks_CardBankId",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.DropTable(
                name: "CardBanks",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "IX_PaymentLines_CardBankId",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.DropColumn(
                name: "IsCard",
                schema: "billing",
                table: "PaymentMethodConfigs");

            migrationBuilder.DropColumn(
                name: "AuthorizationNumber",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.DropColumn(
                name: "CardBankId",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.DropColumn(
                name: "CardBankName",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.DropColumn(
                name: "CardBrand",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.DropColumn(
                name: "CardPaymentType",
                schema: "billing",
                table: "PaymentLines");
        }
    }
}
