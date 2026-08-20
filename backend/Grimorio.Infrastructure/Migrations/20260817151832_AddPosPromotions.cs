using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPosPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PromotionId",
                schema: "pos",
                table: "OrderItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PromotionName",
                schema: "pos",
                table: "OrderItems",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Promotions",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    StartsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndsAt = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    DaysOfWeekMask = table.Column<int>(type: "integer", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FixedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BuyQuantity = table.Column<int>(type: "integer", nullable: true),
                    PayQuantity = table.Column<int>(type: "integer", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_Promotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotionMenuCategories",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PromotionMenuCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionMenuCategories_MenuCategories_MenuCategoryId",
                        column: x => x.MenuCategoryId,
                        principalSchema: "menu",
                        principalTable: "MenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionMenuCategories_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "pos",
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionMenuItems",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_PromotionMenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionMenuItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalSchema: "menu",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionMenuItems_Promotions_PromotionId",
                        column: x => x.PromotionId,
                        principalSchema: "pos",
                        principalTable: "Promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_PromotionId",
                schema: "pos",
                table: "OrderItems",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionMenuCategories_BranchId_PromotionId_MenuCategoryId",
                schema: "pos",
                table: "PromotionMenuCategories",
                columns: new[] { "BranchId", "PromotionId", "MenuCategoryId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionMenuCategories_MenuCategoryId",
                schema: "pos",
                table: "PromotionMenuCategories",
                column: "MenuCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionMenuCategories_PromotionId",
                schema: "pos",
                table: "PromotionMenuCategories",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionMenuItems_BranchId_PromotionId_MenuItemId",
                schema: "pos",
                table: "PromotionMenuItems",
                columns: new[] { "BranchId", "PromotionId", "MenuItemId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionMenuItems_MenuItemId",
                schema: "pos",
                table: "PromotionMenuItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionMenuItems_PromotionId",
                schema: "pos",
                table: "PromotionMenuItems",
                column: "PromotionId");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_BranchId_IsActive_Priority",
                schema: "pos",
                table: "Promotions",
                columns: new[] { "BranchId", "IsActive", "Priority" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Promotions_BranchId_Name",
                schema: "pos",
                table: "Promotions",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Promotions_PromotionId",
                schema: "pos",
                table: "OrderItems",
                column: "PromotionId",
                principalSchema: "pos",
                principalTable: "Promotions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Promotions_PromotionId",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "PromotionMenuCategories",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PromotionMenuItems",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "Promotions",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_PromotionId",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionId",
                schema: "pos",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "PromotionName",
                schema: "pos",
                table: "OrderItems");
        }
    }
}
