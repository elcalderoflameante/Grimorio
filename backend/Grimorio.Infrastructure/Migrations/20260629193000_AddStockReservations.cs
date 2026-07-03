using System;
using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(GrimorioDbContext))]
    [Migration("20260629193000_AddStockReservations")]
    public partial class AddStockReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StockReservations",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReservedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReleasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockReservations_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReservations_MeasurementUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockReservations_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_ArticleId",
                schema: "inv",
                table: "StockReservations",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId",
                schema: "inv",
                table: "StockReservations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId_ArticleId_WarehouseId_Status",
                schema: "inv",
                table: "StockReservations",
                columns: new[] { "BranchId", "ArticleId", "WarehouseId", "Status" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId_IsDeleted",
                schema: "inv",
                table: "StockReservations",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId_OrderId",
                schema: "inv",
                table: "StockReservations",
                columns: new[] { "BranchId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId_OrderItemId",
                schema: "inv",
                table: "StockReservations",
                columns: new[] { "BranchId", "OrderItemId" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_BranchId_OrderItemId_ArticleId_WarehouseId_Status",
                schema: "inv",
                table: "StockReservations",
                columns: new[] { "BranchId", "OrderItemId", "ArticleId", "WarehouseId", "Status" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_IsDeleted",
                schema: "inv",
                table: "StockReservations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_UnitId",
                schema: "inv",
                table: "StockReservations",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockReservations_WarehouseId",
                schema: "inv",
                table: "StockReservations",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockReservations",
                schema: "inv");
        }
    }
}
