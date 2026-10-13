using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionRecipePreparationSteps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductionRecipePreparationSteps",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ProductionRecipeId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepNumber = table.Column<int>(type: "integer", nullable: false),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
                    Temperature = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
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
                    table.PrimaryKey("PK_ProductionRecipePreparationSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionRecipePreparationSteps_ProductionRecipes_Producti~",
                        column: x => x.ProductionRecipeId,
                        principalSchema: "inv",
                        principalTable: "ProductionRecipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipePreparationSteps_BranchId",
                schema: "inv",
                table: "ProductionRecipePreparationSteps",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipePreparationSteps_BranchId_IsDeleted",
                schema: "inv",
                table: "ProductionRecipePreparationSteps",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipePreparationSteps_BranchId_ProductionRecipeI~",
                schema: "inv",
                table: "ProductionRecipePreparationSteps",
                columns: new[] { "BranchId", "ProductionRecipeId", "StepNumber" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipePreparationSteps_IsDeleted",
                schema: "inv",
                table: "ProductionRecipePreparationSteps",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionRecipePreparationSteps_ProductionRecipeId",
                schema: "inv",
                table: "ProductionRecipePreparationSteps",
                column: "ProductionRecipeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductionRecipePreparationSteps",
                schema: "inv");
        }
    }
}
