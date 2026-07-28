using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Expenses",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ExpenseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CostCenterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentMethodConfigId = table.Column<Guid>(type: "uuid", nullable: true),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DocumentNumber = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RegisteredBy = table.Column<Guid>(type: "uuid", nullable: false),
                    RegisteredByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalSchema: "billing",
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Expenses_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalSchema: "finance",
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseCategories_ExpenseCategoryId",
                        column: x => x.ExpenseCategoryId,
                        principalSchema: "finance",
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Expenses_PaymentMethodConfigs_PaymentMethodConfigId",
                        column: x => x.PaymentMethodConfigId,
                        principalSchema: "billing",
                        principalTable: "PaymentMethodConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BranchId",
                schema: "finance",
                table: "Expenses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BranchId_CostCenterId",
                schema: "finance",
                table: "Expenses",
                columns: new[] { "BranchId", "CostCenterId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BranchId_ExpenseCategoryId",
                schema: "finance",
                table: "Expenses",
                columns: new[] { "BranchId", "ExpenseCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BranchId_ExpenseDate",
                schema: "finance",
                table: "Expenses",
                columns: new[] { "BranchId", "ExpenseDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BranchId_IsDeleted",
                schema: "finance",
                table: "Expenses",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_BranchId_Status",
                schema: "finance",
                table: "Expenses",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CashSessionId",
                schema: "finance",
                table: "Expenses",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CostCenterId",
                schema: "finance",
                table: "Expenses",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseCategoryId",
                schema: "finance",
                table: "Expenses",
                column: "ExpenseCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_IsDeleted",
                schema: "finance",
                table: "Expenses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_PaymentMethodConfigId",
                schema: "finance",
                table: "Expenses",
                column: "PaymentMethodConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenses",
                schema: "finance");
        }
    }
}
