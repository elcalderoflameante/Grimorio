using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSplitPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderPayments_OrderId",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "AmountPaid",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "Change",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.RenameColumn(
                name: "OrderTotal",
                schema: "billing",
                table: "OrderPayments",
                newName: "OrderAmount");

            migrationBuilder.RenameColumn(
                name: "Method",
                schema: "billing",
                table: "OrderPayments",
                newName: "DocumentType");

            migrationBuilder.CreateTable(
                name: "PaymentLines",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    AmountTendered = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Change = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                    table.PrimaryKey("PK_PaymentLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentLines_OrderPayments_OrderPaymentId",
                        column: x => x.OrderPaymentId,
                        principalSchema: "billing",
                        principalTable: "OrderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_OrderId",
                schema: "billing",
                table: "OrderPayments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLines_BranchId",
                schema: "billing",
                table: "PaymentLines",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLines_BranchId_IsDeleted",
                schema: "billing",
                table: "PaymentLines",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLines_IsDeleted",
                schema: "billing",
                table: "PaymentLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLines_OrderPaymentId",
                schema: "billing",
                table: "PaymentLines",
                column: "OrderPaymentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentLines",
                schema: "billing");

            migrationBuilder.DropIndex(
                name: "IX_OrderPayments_OrderId",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.RenameColumn(
                name: "OrderAmount",
                schema: "billing",
                table: "OrderPayments",
                newName: "OrderTotal");

            migrationBuilder.RenameColumn(
                name: "DocumentType",
                schema: "billing",
                table: "OrderPayments",
                newName: "Method");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountPaid",
                schema: "billing",
                table: "OrderPayments",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Change",
                schema: "billing",
                table: "OrderPayments",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_OrderId",
                schema: "billing",
                table: "OrderPayments",
                column: "OrderId",
                unique: true);
        }
    }
}
