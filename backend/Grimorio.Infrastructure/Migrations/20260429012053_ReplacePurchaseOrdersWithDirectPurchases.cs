using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePurchaseOrdersWithDirectPurchases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseOrderItems",
                schema: "purchases");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "purchases");

            migrationBuilder.CreateTable(
                name: "Purchases",
                schema: "purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    DocumentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableBase15 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableBase0 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableBaseExempt = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Iva15 = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Ice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchases",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseItems",
                schema: "purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DiscountPct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxRateId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_MeasurementUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_Purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalSchema: "purchases",
                        principalTable: "Purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseItems_TaxRates_TaxRateId",
                        column: x => x.TaxRateId,
                        principalSchema: "billing",
                        principalTable: "TaxRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_ArticleId",
                schema: "purchases",
                table: "PurchaseItems",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_PurchaseId",
                schema: "purchases",
                table: "PurchaseItems",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_TaxRateId",
                schema: "purchases",
                table: "PurchaseItems",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseItems_UnitId",
                schema: "purchases",
                table: "PurchaseItems",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_BranchId_DocumentDate",
                schema: "purchases",
                table: "Purchases",
                columns: new[] { "BranchId", "DocumentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_BranchId_Status",
                schema: "purchases",
                table: "Purchases",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_BranchId_SupplierId",
                schema: "purchases",
                table: "Purchases",
                columns: new[] { "BranchId", "SupplierId" });

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_SupplierId",
                schema: "purchases",
                table: "Purchases",
                column: "SupplierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PurchaseItems",
                schema: "purchases");

            migrationBuilder.DropTable(
                name: "Purchases",
                schema: "purchases");

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscountTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "purchases",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                schema: "purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxRateId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountPct = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    QuantityOrdered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_MeasurementUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "purchases",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_TaxRates_TaxRateId",
                        column: x => x.TaxRateId,
                        principalSchema: "billing",
                        principalTable: "TaxRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ArticleId",
                schema: "purchases",
                table: "PurchaseOrderItems",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                schema: "purchases",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_TaxRateId",
                schema: "purchases",
                table: "PurchaseOrderItems",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_UnitId",
                schema: "purchases",
                table: "PurchaseOrderItems",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BranchId_OrderNumber",
                schema: "purchases",
                table: "PurchaseOrders",
                columns: new[] { "BranchId", "OrderNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BranchId_Status",
                schema: "purchases",
                table: "PurchaseOrders",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "purchases",
                table: "PurchaseOrders",
                column: "SupplierId");
        }
    }
}
