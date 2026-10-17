using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderTableTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderTableTransfers",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToTableId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_OrderTableTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTableTransfers_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "pos",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderTableTransfers_RestaurantTables_FromTableId",
                        column: x => x.FromTableId,
                        principalSchema: "pos",
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderTableTransfers_RestaurantTables_ToTableId",
                        column: x => x.ToTableId,
                        principalSchema: "pos",
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTableTransfers_BranchId",
                schema: "pos",
                table: "OrderTableTransfers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTableTransfers_BranchId_IsDeleted",
                schema: "pos",
                table: "OrderTableTransfers",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderTableTransfers_BranchId_RequestId",
                schema: "pos",
                table: "OrderTableTransfers",
                columns: new[] { "BranchId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTableTransfers_FromTableId",
                schema: "pos",
                table: "OrderTableTransfers",
                column: "FromTableId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTableTransfers_IsDeleted",
                schema: "pos",
                table: "OrderTableTransfers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTableTransfers_OrderId",
                schema: "pos",
                table: "OrderTableTransfers",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTableTransfers_ToTableId",
                schema: "pos",
                table: "OrderTableTransfers",
                column: "ToTableId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderTableTransfers",
                schema: "pos");
        }
    }
}
