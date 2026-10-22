using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleLoyaltyMemberProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Members_BranchId_CustomerId",
                schema: "marketing",
                table: "Members");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                schema: "marketing",
                table: "Members",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<int>(
                name: "BirthDay",
                schema: "marketing",
                table: "Members",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BirthMonth",
                schema: "marketing",
                table: "Members",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                schema: "marketing",
                table: "Members",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarketingConsentUpdatedAt",
                schema: "marketing",
                table: "Members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "marketing",
                table: "Members",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                schema: "marketing",
                table: "Members",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE marketing."Members" AS m
                SET "Name" = c."Name", "Email" = c."Email"
                FROM billing."Customers" AS c
                WHERE m."CustomerId" = c."Id" AND m."BranchId" = c."BranchId";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Members_BranchId_CustomerId",
                schema: "marketing",
                table: "Members",
                columns: new[] { "BranchId", "CustomerId" },
                unique: true,
                filter: "\"CustomerId\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_Members_BranchId_Phone",
                schema: "marketing",
                table: "Members",
                columns: new[] { "BranchId", "Phone" },
                unique: true,
                filter: "\"Phone\" IS NOT NULL AND NOT \"IsDeleted\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_LoyaltyMembers_Birthday",
                schema: "marketing",
                table: "Members",
                sql: "(\"BirthDay\" IS NULL AND \"BirthMonth\" IS NULL) OR (\"BirthDay\" IS NOT NULL AND \"BirthMonth\" IS NOT NULL AND \"BirthMonth\" BETWEEN 1 AND 12 AND \"BirthDay\" BETWEEN 1 AND CASE \"BirthMonth\" WHEN 2 THEN 29 WHEN 4 THEN 30 WHEN 6 THEN 30 WHEN 9 THEN 30 WHEN 11 THEN 30 ELSE 31 END)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM marketing."Members" WHERE "CustomerId" IS NULL) THEN
                        RAISE EXCEPTION 'No se puede revertir: existen miembros sin ficha de facturacion.';
                    END IF;
                END $$;
                """);
            migrationBuilder.DropIndex(
                name: "IX_Members_BranchId_CustomerId",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropIndex(
                name: "IX_Members_BranchId_Phone",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropCheckConstraint(
                name: "CK_LoyaltyMembers_Birthday",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "BirthDay",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "BirthMonth",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Email",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "MarketingConsentUpdatedAt",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "Phone",
                schema: "marketing",
                table: "Members");

            migrationBuilder.AlterColumn<Guid>(
                name: "CustomerId",
                schema: "marketing",
                table: "Members",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_BranchId_CustomerId",
                schema: "marketing",
                table: "Members",
                columns: new[] { "BranchId", "CustomerId" },
                unique: true);
        }
    }
}
