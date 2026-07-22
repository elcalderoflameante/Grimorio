using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveVariableIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItemIngredientChoices",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "RecipeIngredientAlternatives",
                schema: "menu");

            migrationBuilder.DropColumn(
                name: "IsVariable",
                schema: "menu",
                table: "RecipeIngredients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsVariable",
                schema: "menu",
                table: "RecipeIngredients",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "OrderItemIngredientChoices",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChosenArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeIngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItemIngredientChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItemIngredientChoices_InventoryArticles_ChosenArticleId",
                        column: x => x.ChosenArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemIngredientChoices_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "pos",
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItemIngredientChoices_RecipeIngredients_RecipeIngredie~",
                        column: x => x.RecipeIngredientId,
                        principalSchema: "menu",
                        principalTable: "RecipeIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredientAlternatives",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipeIngredientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeIngredientAlternatives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredientAlternatives_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeIngredientAlternatives_RecipeIngredients_RecipeIngred~",
                        column: x => x.RecipeIngredientId,
                        principalSchema: "menu",
                        principalTable: "RecipeIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemIngredientChoices_ChosenArticleId",
                schema: "pos",
                table: "OrderItemIngredientChoices",
                column: "ChosenArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemIngredientChoices_OrderItemId",
                schema: "pos",
                table: "OrderItemIngredientChoices",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemIngredientChoices_RecipeIngredientId",
                schema: "pos",
                table: "OrderItemIngredientChoices",
                column: "RecipeIngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientAlternatives_ArticleId",
                schema: "menu",
                table: "RecipeIngredientAlternatives",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientAlternatives_BranchId",
                schema: "menu",
                table: "RecipeIngredientAlternatives",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientAlternatives_BranchId_IsDeleted",
                schema: "menu",
                table: "RecipeIngredientAlternatives",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientAlternatives_IsDeleted",
                schema: "menu",
                table: "RecipeIngredientAlternatives",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredientAlternatives_RecipeIngredientId_ArticleId",
                schema: "menu",
                table: "RecipeIngredientAlternatives",
                columns: new[] { "RecipeIngredientId", "ArticleId" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }
    }
}
