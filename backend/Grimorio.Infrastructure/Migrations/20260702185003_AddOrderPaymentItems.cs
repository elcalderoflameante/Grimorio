using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderPaymentItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderPaymentItems",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_OrderPaymentItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPaymentItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "pos",
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderPaymentItems_OrderPayments_OrderPaymentId",
                        column: x => x.OrderPaymentId,
                        principalSchema: "billing",
                        principalTable: "OrderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentItems_BranchId",
                schema: "billing",
                table: "OrderPaymentItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentItems_BranchId_IsDeleted",
                schema: "billing",
                table: "OrderPaymentItems",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentItems_BranchId_OrderItemId",
                schema: "billing",
                table: "OrderPaymentItems",
                columns: new[] { "BranchId", "OrderItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentItems_IsDeleted",
                schema: "billing",
                table: "OrderPaymentItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentItems_OrderItemId",
                schema: "billing",
                table: "OrderPaymentItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPaymentItems_OrderPaymentId",
                schema: "billing",
                table: "OrderPaymentItems",
                column: "OrderPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderPaymentItems",
                schema: "billing");
        }
    }
}
