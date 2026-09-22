using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RestoreProductionOrderProducedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE inv."ProductionOrders"
                    ADD COLUMN IF NOT EXISTS "ProducedAt" timestamp with time zone;

                UPDATE inv."ProductionOrders"
                SET "ProducedAt" = "CreatedAt"
                WHERE "ProducedAt" IS NULL;

                ALTER TABLE inv."ProductionOrders"
                    ALTER COLUMN "ProducedAt" SET DEFAULT CURRENT_TIMESTAMP,
                    ALTER COLUMN "ProducedAt" SET NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La columna pertenecia a la migracion original, pero faltaba en el modelo EF.
            migrationBuilder.Sql("""
                ALTER TABLE inv."ProductionOrders"
                    ALTER COLUMN "ProducedAt" DROP DEFAULT;
                """);
        }
    }
}
