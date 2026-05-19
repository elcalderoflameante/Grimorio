using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSecuencialInicialAndCleanDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SecuencialInicial",
                schema: "billing",
                table: "BranchTaxConfigs",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);

            // Inicializar SecuencialInicial=1 en registros existentes y resetear el contador
            migrationBuilder.Sql(@"UPDATE billing.""BranchTaxConfigs"" SET ""SecuencialInicial"" = 1, ""Secuencial"" = 0;");

            // Limpiar documentos electrónicos de prueba para poder iniciar fresh
            migrationBuilder.Sql(@"DELETE FROM billing.""ElectronicDocuments"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecuencialInicial",
                schema: "billing",
                table: "BranchTaxConfigs");
        }
    }
}
