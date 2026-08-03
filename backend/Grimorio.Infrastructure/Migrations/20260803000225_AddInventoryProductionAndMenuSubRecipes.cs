using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    public partial class AddInventoryProductionAndMenuSubRecipes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecipeIngredients_MenuItemId_ArticleId",
                schema: "menu",
                table: "RecipeIngredients");

            migrationBuilder.AlterColumn<Guid>(
                name: "ArticleId",
                schema: "menu",
                table: "RecipeIngredients",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "SubRecipeId",
                schema: "menu",
                table: "RecipeIngredients",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                schema: "menu",
                table: "RecipeIngredients",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Article");

            CreateSubRecipeTables(migrationBuilder);
            CreateProductionTables(migrationBuilder);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_MenuItemId_ArticleId",
                schema: "menu",
                table: "RecipeIngredients",
                columns: new[] { "MenuItemId", "ArticleId" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"ArticleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_MenuItemId_SubRecipeId",
                schema: "menu",
                table: "RecipeIngredients",
                columns: new[] { "MenuItemId", "SubRecipeId" },
                unique: true,
                filter: "\"IsDeleted\" = false AND \"SubRecipeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_SubRecipeId",
                schema: "menu",
                table: "RecipeIngredients",
                column: "SubRecipeId");

            migrationBuilder.AddForeignKey(
                name: "FK_RecipeIngredients_SubRecipes_SubRecipeId",
                schema: "menu",
                table: "RecipeIngredients",
                column: "SubRecipeId",
                principalSchema: "menu",
                principalTable: "SubRecipes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecipeIngredients_SubRecipes_SubRecipeId",
                schema: "menu",
                table: "RecipeIngredients");

            migrationBuilder.DropTable(name: "ProductionOrderIngredients", schema: "inv");
            migrationBuilder.DropTable(name: "ProductionOrderMovements", schema: "inv");
            migrationBuilder.DropTable(name: "ProductionRecipeIngredients", schema: "inv");
            migrationBuilder.DropTable(name: "SubRecipeIngredients", schema: "menu");
            migrationBuilder.DropTable(name: "ProductionOrders", schema: "inv");
            migrationBuilder.DropTable(name: "ProductionRecipes", schema: "inv");
            migrationBuilder.DropTable(name: "SubRecipes", schema: "menu");

            migrationBuilder.DropIndex(name: "IX_RecipeIngredients_MenuItemId_ArticleId", schema: "menu", table: "RecipeIngredients");
            migrationBuilder.DropIndex(name: "IX_RecipeIngredients_MenuItemId_SubRecipeId", schema: "menu", table: "RecipeIngredients");
            migrationBuilder.DropIndex(name: "IX_RecipeIngredients_SubRecipeId", schema: "menu", table: "RecipeIngredients");

            migrationBuilder.DropColumn(name: "SubRecipeId", schema: "menu", table: "RecipeIngredients");
            migrationBuilder.DropColumn(name: "Type", schema: "menu", table: "RecipeIngredients");

            migrationBuilder.AlterColumn<Guid>(
                name: "ArticleId",
                schema: "menu",
                table: "RecipeIngredients",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_MenuItemId_ArticleId",
                schema: "menu",
                table: "RecipeIngredients",
                columns: new[] { "MenuItemId", "ArticleId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        private static void CreateSubRecipeTables(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubRecipes",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OutputQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OutputUnitId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_SubRecipes", x => x.Id);
                    table.ForeignKey("FK_SubRecipes_MeasurementUnits_OutputUnitId", x => x.OutputUnitId, principalSchema: "inv", principalTable: "MeasurementUnits", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubRecipeIngredients",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    SubRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_SubRecipeIngredients", x => x.Id);
                    table.ForeignKey("FK_SubRecipeIngredients_InventoryArticles_ArticleId", x => x.ArticleId, principalSchema: "inv", principalTable: "InventoryArticles", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SubRecipeIngredients_MeasurementUnits_UnitId", x => x.UnitId, principalSchema: "inv", principalTable: "MeasurementUnits", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_SubRecipeIngredients_SubRecipes_SubRecipeId", x => x.SubRecipeId, principalSchema: "menu", principalTable: "SubRecipes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex("IX_SubRecipes_BranchId_IsDeleted", "SubRecipes", "BranchId", "menu");
            migrationBuilder.CreateIndex("IX_SubRecipes_BranchId_IsActive", "SubRecipes", new[] { "BranchId", "IsActive" }, "menu");
            migrationBuilder.CreateIndex("IX_SubRecipes_BranchId_Name", "SubRecipes", new[] { "BranchId", "Name" }, "menu", unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_SubRecipes_OutputUnitId", "SubRecipes", "OutputUnitId", "menu");
            migrationBuilder.CreateIndex("IX_SubRecipeIngredients_ArticleId", "SubRecipeIngredients", "ArticleId", "menu");
            migrationBuilder.CreateIndex("IX_SubRecipeIngredients_BranchId_IsDeleted", "SubRecipeIngredients", "BranchId", "menu");
            migrationBuilder.CreateIndex("IX_SubRecipeIngredients_BranchId_SubRecipeId_ArticleId", "SubRecipeIngredients", new[] { "BranchId", "SubRecipeId", "ArticleId" }, "menu", unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_SubRecipeIngredients_SubRecipeId", "SubRecipeIngredients", "SubRecipeId", "menu");
            migrationBuilder.CreateIndex("IX_SubRecipeIngredients_UnitId", "SubRecipeIngredients", "UnitId", "menu");
        }

        private static void CreateProductionTables(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionRecipes",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OutputArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OutputUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_ProductionRecipes", x => x.Id);
                    table.ForeignKey("FK_ProductionRecipes_InventoryArticles_OutputArticleId", x => x.OutputArticleId, principalSchema: "inv", principalTable: "InventoryArticles", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionRecipes_MeasurementUnits_OutputUnitId", x => x.OutputUnitId, principalSchema: "inv", principalTable: "MeasurementUnits", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProductionRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OutputUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    OutputBaseQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProducedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.ForeignKey("FK_ProductionOrders_InventoryArticles_OutputArticleId", x => x.OutputArticleId, principalSchema: "inv", principalTable: "InventoryArticles", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionOrders_MeasurementUnits_OutputUnitId", x => x.OutputUnitId, principalSchema: "inv", principalTable: "MeasurementUnits", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionOrders_ProductionRecipes_ProductionRecipeId", x => x.ProductionRecipeId, principalSchema: "inv", principalTable: "ProductionRecipes", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionOrders_Warehouses_DestinationWarehouseId", x => x.DestinationWarehouseId, principalSchema: "inv", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionOrders_Warehouses_SourceWarehouseId", x => x.SourceWarehouseId, principalSchema: "inv", principalTable: "Warehouses", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductionRecipeIngredients",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductionRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_ProductionRecipeIngredients", x => x.Id);
                    table.ForeignKey("FK_ProductionRecipeIngredients_InventoryArticles_ArticleId", x => x.ArticleId, principalSchema: "inv", principalTable: "InventoryArticles", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionRecipeIngredients_MeasurementUnits_UnitId", x => x.UnitId, principalSchema: "inv", principalTable: "MeasurementUnits", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionRecipeIngredients_ProductionRecipes_ProductionRecipeId", x => x.ProductionRecipeId, principalSchema: "inv", principalTable: "ProductionRecipes", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderIngredients",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BaseUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
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
                    table.PrimaryKey("PK_ProductionOrderIngredients", x => x.Id);
                    table.ForeignKey("FK_ProductionOrderIngredients_InventoryArticles_ArticleId", x => x.ArticleId, principalSchema: "inv", principalTable: "InventoryArticles", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionOrderIngredients_MeasurementUnits_UnitId", x => x.UnitId, principalSchema: "inv", principalTable: "MeasurementUnits", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_ProductionOrderIngredients_ProductionOrders_ProductionOrderId", x => x.ProductionOrderId, principalSchema: "inv", principalTable: "ProductionOrders", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrderMovements",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductionOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockMovementId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_ProductionOrderMovements", x => x.Id);
                    table.ForeignKey("FK_ProductionOrderMovements_ProductionOrders_ProductionOrderId", x => x.ProductionOrderId, principalSchema: "inv", principalTable: "ProductionOrders", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_ProductionOrderMovements_StockMovements_StockMovementId", x => x.StockMovementId, principalSchema: "inv", principalTable: "StockMovements", principalColumn: "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex("IX_ProductionRecipes_BranchId_OutputArticleId", "ProductionRecipes", new[] { "BranchId", "OutputArticleId" }, "inv", unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_ProductionRecipes_OutputArticleId", "ProductionRecipes", "OutputArticleId", "inv", unique: true);
            migrationBuilder.CreateIndex("IX_ProductionRecipes_OutputUnitId", "ProductionRecipes", "OutputUnitId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionRecipeIngredients_ArticleId", "ProductionRecipeIngredients", "ArticleId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionRecipeIngredients_BranchId_ProductionRecipeId_ArticleId", "ProductionRecipeIngredients", new[] { "BranchId", "ProductionRecipeId", "ArticleId" }, "inv", filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_ProductionRecipeIngredients_ProductionRecipeId", "ProductionRecipeIngredients", "ProductionRecipeId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionRecipeIngredients_UnitId", "ProductionRecipeIngredients", "UnitId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrders_BranchId_Number", "ProductionOrders", new[] { "BranchId", "Number" }, "inv", unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_ProductionOrders_BranchId_CreatedAt", "ProductionOrders", new[] { "BranchId", "CreatedAt" }, "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrders_BranchId_OutputArticleId_CreatedAt", "ProductionOrders", new[] { "BranchId", "OutputArticleId", "CreatedAt" }, "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrders_DestinationWarehouseId", "ProductionOrders", "DestinationWarehouseId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrders_OutputArticleId", "ProductionOrders", "OutputArticleId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrders_OutputUnitId", "ProductionOrders", "OutputUnitId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrders_ProductionRecipeId", "ProductionOrders", "ProductionRecipeId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrders_SourceWarehouseId", "ProductionOrders", "SourceWarehouseId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrderIngredients_ArticleId", "ProductionOrderIngredients", "ArticleId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrderIngredients_BranchId_ArticleId", "ProductionOrderIngredients", new[] { "BranchId", "ArticleId" }, "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrderIngredients_BranchId_ProductionOrderId", "ProductionOrderIngredients", new[] { "BranchId", "ProductionOrderId" }, "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrderIngredients_ProductionOrderId", "ProductionOrderIngredients", "ProductionOrderId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrderIngredients_UnitId", "ProductionOrderIngredients", "UnitId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrderMovements_BranchId_ProductionOrderId", "ProductionOrderMovements", new[] { "BranchId", "ProductionOrderId" }, "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrderMovements_BranchId_StockMovementId", "ProductionOrderMovements", new[] { "BranchId", "StockMovementId" }, "inv", unique: true, filter: "\"IsDeleted\" = false");
            migrationBuilder.CreateIndex("IX_ProductionOrderMovements_ProductionOrderId", "ProductionOrderMovements", "ProductionOrderId", "inv");
            migrationBuilder.CreateIndex("IX_ProductionOrderMovements_StockMovementId", "ProductionOrderMovements", "StockMovementId", "inv");
        }
    }
}
