using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemModifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuItemModifierGroups",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MinSelections = table.Column<int>(type: "integer", nullable: false),
                    MaxSelections = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowDuplicates = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_MenuItemModifierGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemModifierGroups_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalSchema: "menu",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItemModifierOptions",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ModifierGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PriceDelta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_MenuItemModifierOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemModifierOptions_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItemModifierOptions_MeasurementUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItemModifierOptions_MenuItemModifierGroups_ModifierGrou~",
                        column: x => x.ModifierGroupId,
                        principalSchema: "menu",
                        principalTable: "MenuItemModifierGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemModifierSelections",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifierGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifierOptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OptionName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPriceDelta = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    InventoryQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_OrderItemModifierSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemModifierSelections_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemModifierSelections_MeasurementUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemModifierSelections_MenuItemModifierGroups_Modifier~",
                        column: x => x.ModifierGroupId,
                        principalSchema: "menu",
                        principalTable: "MenuItemModifierGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemModifierSelections_MenuItemModifierOptions_Modifie~",
                        column: x => x.ModifierOptionId,
                        principalSchema: "menu",
                        principalTable: "MenuItemModifierOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemModifierSelections_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "pos",
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierGroups_BranchId",
                schema: "menu",
                table: "MenuItemModifierGroups",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierGroups_BranchId_IsDeleted",
                schema: "menu",
                table: "MenuItemModifierGroups",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierGroups_BranchId_MenuItemId_DisplayOrder",
                schema: "menu",
                table: "MenuItemModifierGroups",
                columns: new[] { "BranchId", "MenuItemId", "DisplayOrder" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierGroups_IsDeleted",
                schema: "menu",
                table: "MenuItemModifierGroups",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierGroups_MenuItemId",
                schema: "menu",
                table: "MenuItemModifierGroups",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierOptions_ArticleId",
                schema: "menu",
                table: "MenuItemModifierOptions",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierOptions_BranchId",
                schema: "menu",
                table: "MenuItemModifierOptions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierOptions_BranchId_IsDeleted",
                schema: "menu",
                table: "MenuItemModifierOptions",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierOptions_BranchId_ModifierGroupId_DisplayOrd~",
                schema: "menu",
                table: "MenuItemModifierOptions",
                columns: new[] { "BranchId", "ModifierGroupId", "DisplayOrder" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierOptions_IsDeleted",
                schema: "menu",
                table: "MenuItemModifierOptions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierOptions_ModifierGroupId",
                schema: "menu",
                table: "MenuItemModifierOptions",
                column: "ModifierGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemModifierOptions_UnitId",
                schema: "menu",
                table: "MenuItemModifierOptions",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemModifierSelections_ArticleId",
                schema: "pos",
                table: "OrderItemModifierSelections",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemModifierSelections_BranchId_ModifierGroupId",
                schema: "pos",
                table: "OrderItemModifierSelections",
                columns: new[] { "BranchId", "ModifierGroupId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemModifierSelections_ModifierGroupId",
                schema: "pos",
                table: "OrderItemModifierSelections",
                column: "ModifierGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemModifierSelections_ModifierOptionId",
                schema: "pos",
                table: "OrderItemModifierSelections",
                column: "ModifierOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemModifierSelections_OrderItemId",
                schema: "pos",
                table: "OrderItemModifierSelections",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemModifierSelections_UnitId",
                schema: "pos",
                table: "OrderItemModifierSelections",
                column: "UnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemModifierSelections",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "MenuItemModifierOptions",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "MenuItemModifierGroups",
                schema: "menu");
        }
    }
}
