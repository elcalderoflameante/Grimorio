using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestoreProductionOrderIngredientBaseUnit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE inv."ProductionOrderIngredients"
                    ADD COLUMN IF NOT EXISTS "BaseUnitId" uuid;

                UPDATE inv."ProductionOrderIngredients" AS ingredient
                SET "BaseUnitId" = article."BaseUnitId"
                FROM inv."InventoryArticles" AS article
                WHERE ingredient."ArticleId" = article."Id"
                  AND ingredient."BaseUnitId" IS NULL;

                ALTER TABLE inv."ProductionOrderIngredients"
                    ALTER COLUMN "BaseUnitId" SET NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La columna pertenecia a la migracion original, pero faltaba en el modelo EF.
        }
    }
}
