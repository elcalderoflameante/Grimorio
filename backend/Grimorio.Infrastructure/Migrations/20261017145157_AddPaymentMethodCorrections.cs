using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OriginalPaymentLinesJson",
                schema: "billing",
                table: "OrderPayments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PaymentMethodCorrections",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BeforeJson = table.Column<string>(type: "jsonb", nullable: false),
                    AfterJson = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_PaymentMethodCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentMethodCorrections_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalSchema: "billing",
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentMethodCorrections_OrderPayments_OrderPaymentId",
                        column: x => x.OrderPaymentId,
                        principalSchema: "billing",
                        principalTable: "OrderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentMethodCorrections_PaymentLines_PaymentLineId",
                        column: x => x.PaymentLineId,
                        principalSchema: "billing",
                        principalTable: "PaymentLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodCorrections_BranchId",
                schema: "billing",
                table: "PaymentMethodCorrections",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodCorrections_BranchId_IsDeleted",
                schema: "billing",
                table: "PaymentMethodCorrections",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodCorrections_BranchId_RequestId",
                schema: "billing",
                table: "PaymentMethodCorrections",
                columns: new[] { "BranchId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodCorrections_CashSessionId",
                schema: "billing",
                table: "PaymentMethodCorrections",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodCorrections_IsDeleted",
                schema: "billing",
                table: "PaymentMethodCorrections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodCorrections_OrderPaymentId_CreatedAt",
                schema: "billing",
                table: "PaymentMethodCorrections",
                columns: new[] { "OrderPaymentId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodCorrections_PaymentLineId",
                schema: "billing",
                table: "PaymentMethodCorrections",
                column: "PaymentLineId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentMethodCorrections",
                schema: "billing");

            migrationBuilder.DropColumn(
                name: "OriginalPaymentLinesJson",
                schema: "billing",
                table: "OrderPayments");
        }
    }
}
