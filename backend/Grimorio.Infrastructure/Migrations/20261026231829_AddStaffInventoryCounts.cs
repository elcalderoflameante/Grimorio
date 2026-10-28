using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffInventoryCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryCountAreas",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                    table.PrimaryKey("PK_InventoryCountAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StaffInventoryCounts",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    AreaName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CounterUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CounterName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RecountOfId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewerName = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_StaffInventoryCounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountMembers",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_InventoryCountMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountMembers_InventoryCountAreas_AreaId",
                        column: x => x.AreaId,
                        principalSchema: "inv",
                        principalTable: "InventoryCountAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCountRules",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WeekDay = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_InventoryCountRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryCountRules_InventoryCountAreas_AreaId",
                        column: x => x.AreaId,
                        principalSchema: "inv",
                        principalTable: "InventoryCountAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StaffInventoryCountLines",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    WarehouseName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitSymbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    ExpectedQuantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    CountedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CountedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_StaffInventoryCountLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffInventoryCountLines_StaffInventoryCounts_CountId",
                        column: x => x.CountId,
                        principalSchema: "inv",
                        principalTable: "StaffInventoryCounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountAreas_BranchId",
                schema: "inv",
                table: "InventoryCountAreas",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountAreas_BranchId_IsDeleted",
                schema: "inv",
                table: "InventoryCountAreas",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountAreas_BranchId_Name",
                schema: "inv",
                table: "InventoryCountAreas",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountAreas_IsDeleted",
                schema: "inv",
                table: "InventoryCountAreas",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountMembers_AreaId_UserId",
                schema: "inv",
                table: "InventoryCountMembers",
                columns: new[] { "AreaId", "UserId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountMembers_BranchId",
                schema: "inv",
                table: "InventoryCountMembers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountMembers_BranchId_IsDeleted",
                schema: "inv",
                table: "InventoryCountMembers",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountMembers_IsDeleted",
                schema: "inv",
                table: "InventoryCountMembers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountRules_AreaId",
                schema: "inv",
                table: "InventoryCountRules",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountRules_BranchId",
                schema: "inv",
                table: "InventoryCountRules",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountRules_BranchId_ArticleId_WarehouseId",
                schema: "inv",
                table: "InventoryCountRules",
                columns: new[] { "BranchId", "ArticleId", "WarehouseId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountRules_BranchId_IsDeleted",
                schema: "inv",
                table: "InventoryCountRules",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCountRules_IsDeleted",
                schema: "inv",
                table: "InventoryCountRules",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCountLines_BranchId",
                schema: "inv",
                table: "StaffInventoryCountLines",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCountLines_BranchId_IsDeleted",
                schema: "inv",
                table: "StaffInventoryCountLines",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCountLines_CountId_ArticleId_WarehouseId",
                schema: "inv",
                table: "StaffInventoryCountLines",
                columns: new[] { "CountId", "ArticleId", "WarehouseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCountLines_IsDeleted",
                schema: "inv",
                table: "StaffInventoryCountLines",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCounts_BranchId",
                schema: "inv",
                table: "StaffInventoryCounts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCounts_BranchId_AreaId",
                schema: "inv",
                table: "StaffInventoryCounts",
                columns: new[] { "BranchId", "AreaId" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"Status\" = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCounts_BranchId_AreaId_BusinessDate",
                schema: "inv",
                table: "StaffInventoryCounts",
                columns: new[] { "BranchId", "AreaId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCounts_BranchId_IsDeleted",
                schema: "inv",
                table: "StaffInventoryCounts",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StaffInventoryCounts_IsDeleted",
                schema: "inv",
                table: "StaffInventoryCounts",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryCountMembers",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "InventoryCountRules",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StaffInventoryCountLines",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "InventoryCountAreas",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StaffInventoryCounts",
                schema: "inv");
        }
    }
}
