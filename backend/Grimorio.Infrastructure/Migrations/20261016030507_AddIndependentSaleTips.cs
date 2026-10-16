using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndependentSaleTips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SaleTips",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Method = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegisteredByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UpdatedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
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
                    table.PrimaryKey("PK_SaleTips", x => x.Id);
                    table.CheckConstraint("CK_SaleTips_Amount", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_SaleTips_OrderPayments_OrderPaymentId",
                        column: x => x.OrderPaymentId,
                        principalSchema: "billing",
                        principalTable: "OrderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SaleTips_BranchId",
                schema: "finance",
                table: "SaleTips",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleTips_BranchId_IsDeleted",
                schema: "finance",
                table: "SaleTips",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleTips_BranchId_OrderPaymentId",
                schema: "finance",
                table: "SaleTips",
                columns: new[] { "BranchId", "OrderPaymentId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SaleTips_BranchId_RegisteredAt",
                schema: "finance",
                table: "SaleTips",
                columns: new[] { "BranchId", "RegisteredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleTips_IsDeleted",
                schema: "finance",
                table: "SaleTips",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_SaleTips_OrderPaymentId",
                schema: "finance",
                table: "SaleTips",
                column: "OrderPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SaleTips",
                schema: "finance");
        }
    }
}
