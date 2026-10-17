using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderItemCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderItemCorrections",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OriginalItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReplacementItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Disposition = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: false),
                    OriginalStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_OrderItemCorrections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemCorrections_OrderItems_OriginalItemId",
                        column: x => x.OriginalItemId,
                        principalSchema: "pos",
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemCorrections_OrderItems_ReplacementItemId",
                        column: x => x.ReplacementItemId,
                        principalSchema: "pos",
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCorrections_BranchId",
                schema: "pos",
                table: "OrderItemCorrections",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCorrections_BranchId_IsDeleted",
                schema: "pos",
                table: "OrderItemCorrections",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCorrections_BranchId_RequestId",
                schema: "pos",
                table: "OrderItemCorrections",
                columns: new[] { "BranchId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCorrections_IsDeleted",
                schema: "pos",
                table: "OrderItemCorrections",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCorrections_OriginalItemId",
                schema: "pos",
                table: "OrderItemCorrections",
                column: "OriginalItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemCorrections_ReplacementItemId",
                schema: "pos",
                table: "OrderItemCorrections",
                column: "ReplacementItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemCorrections",
                schema: "pos");
        }
    }
}
