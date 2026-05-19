using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CleanElectronicDocumentsAndResetSecuencial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Borrar todos los documentos electrónicos de prueba
            migrationBuilder.Sql(@"DELETE FROM billing.""ElectronicDocuments"";");

            // Resetear el contador al valor de SecuencialInicial configurado por el usuario
            migrationBuilder.Sql(@"UPDATE billing.""BranchTaxConfigs"" SET ""Secuencial"" = ""SecuencialInicial"" - 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
