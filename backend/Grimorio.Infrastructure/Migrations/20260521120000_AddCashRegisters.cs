using System;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260521120000_AddCashRegisters")]
    public partial class AddCashRegisters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashRegisters",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_CashRegisters", x => x.Id);
                });

            migrationBuilder.AddColumn<Guid>(
                name: "CashRegisterId",
                schema: "billing",
                table: "CashSessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                INSERT INTO billing."CashRegisters"
                    ("Id", "Name", "Code", "Description", "IsActive", "BranchId", "CreatedAt", "CreatedBy", "IsDeleted")
                SELECT gen_random_uuid(), 'Caja principal', 'CAJA-01', 'Caja creada automaticamente para sesiones existentes.', true,
                       s."BranchId", CURRENT_TIMESTAMP, '00000000-0000-0000-0000-000000000000', false
                FROM billing."CashSessions" s
                WHERE s."IsDeleted" = false
                GROUP BY s."BranchId"
                ON CONFLICT DO NOTHING;
            """);

            migrationBuilder.Sql("""
                UPDATE billing."CashSessions" s
                SET "CashRegisterId" = r."Id"
                FROM billing."CashRegisters" r
                WHERE r."BranchId" = s."BranchId"
                  AND r."Code" = 'CAJA-01'
                  AND s."CashRegisterId" IS NULL;
            """);

            migrationBuilder.Sql("""
                INSERT INTO billing."CashRegisters"
                    ("Id", "Name", "Code", "Description", "IsActive", "BranchId", "CreatedAt", "CreatedBy", "IsDeleted")
                SELECT gen_random_uuid(), 'Caja principal', 'CAJA-01', 'Caja principal de la sucursal.', true,
                       b."Id", CURRENT_TIMESTAMP, '00000000-0000-0000-0000-000000000000', false
                FROM organization."Branches" b
                WHERE NOT EXISTS (
                    SELECT 1 FROM billing."CashRegisters" r
                    WHERE r."BranchId" = b."Id" AND r."Code" = 'CAJA-01' AND r."IsDeleted" = false
                );
            """);

            migrationBuilder.AlterColumn<Guid>(
                name: "CashRegisterId",
                schema: "billing",
                table: "CashSessions",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_CashRegisterId_Status",
                schema: "billing",
                table: "CashSessions",
                columns: new[] { "CashRegisterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_BranchId_OpenedBy_Status",
                schema: "billing",
                table: "CashSessions",
                columns: new[] { "BranchId", "OpenedBy", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_BranchId",
                schema: "billing",
                table: "CashRegisters",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_BranchId_Code",
                schema: "billing",
                table: "CashRegisters",
                columns: new[] { "BranchId", "Code" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_BranchId_IsActive",
                schema: "billing",
                table: "CashRegisters",
                columns: new[] { "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_BranchId_IsDeleted",
                schema: "billing",
                table: "CashRegisters",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_IsDeleted",
                schema: "billing",
                table: "CashRegisters",
                column: "IsDeleted");

            migrationBuilder.AddForeignKey(
                name: "FK_CashSessions_CashRegisters_CashRegisterId",
                schema: "billing",
                table: "CashSessions",
                column: "CashRegisterId",
                principalSchema: "billing",
                principalTable: "CashRegisters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashSessions_CashRegisters_CashRegisterId",
                schema: "billing",
                table: "CashSessions");

            migrationBuilder.DropTable(
                name: "CashRegisters",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_CashRegisterId_Status",
                schema: "billing",
                table: "CashSessions");

            migrationBuilder.DropIndex(
                name: "IX_CashSessions_BranchId_OpenedBy_Status",
                schema: "billing",
                table: "CashSessions");

            migrationBuilder.DropColumn(
                name: "CashRegisterId",
                schema: "billing",
                table: "CashSessions");
        }
    }
}
