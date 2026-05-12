using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSriElectronicInvoicing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContribuyenteEspecial",
                schema: "billing",
                table: "BranchTaxConfigs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ObligadoContabilidad",
                schema: "billing",
                table: "BranchTaxConfigs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "Secuencial",
                schema: "billing",
                table: "BranchTaxConfigs",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ElectronicDocuments",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaveAcceso = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: false),
                    NumeroFactura = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: false),
                    Secuencial = table.Column<long>(type: "bigint", nullable: false),
                    Environment = table.Column<string>(type: "character varying(1)", maxLength: 1, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalSinImpuestos = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalDescuento = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalIva = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ImporteTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    XmlSigned = table.Column<string>(type: "text", nullable: true),
                    XmlAuthorized = table.Column<string>(type: "text", nullable: true),
                    NumeroAutorizacion = table.Column<string>(type: "character varying(49)", maxLength: 49, nullable: true),
                    FechaAutorizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RidePdf = table.Column<byte[]>(type: "bytea", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ElectronicDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ElectronicDocuments_OrderPayments_OrderPaymentId",
                        column: x => x.OrderPaymentId,
                        principalSchema: "billing",
                        principalTable: "OrderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SriCertificates",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    FileName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CertificateEncrypted = table.Column<byte[]>(type: "bytea", nullable: false),
                    PasswordEncrypted = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_SriCertificates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_BranchId",
                schema: "billing",
                table: "ElectronicDocuments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_BranchId_IsDeleted",
                schema: "billing",
                table: "ElectronicDocuments",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_BranchId_Status",
                schema: "billing",
                table: "ElectronicDocuments",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_ClaveAcceso",
                schema: "billing",
                table: "ElectronicDocuments",
                column: "ClaveAcceso",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_IsDeleted",
                schema: "billing",
                table: "ElectronicDocuments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicDocuments_OrderPaymentId",
                schema: "billing",
                table: "ElectronicDocuments",
                column: "OrderPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_SriCertificates_BranchId",
                schema: "billing",
                table: "SriCertificates",
                column: "BranchId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_SriCertificates_BranchId_IsDeleted",
                schema: "billing",
                table: "SriCertificates",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_SriCertificates_IsDeleted",
                schema: "billing",
                table: "SriCertificates",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ElectronicDocuments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "SriCertificates",
                schema: "billing");

            migrationBuilder.DropColumn(
                name: "ContribuyenteEspecial",
                schema: "billing",
                table: "BranchTaxConfigs");

            migrationBuilder.DropColumn(
                name: "ObligadoContabilidad",
                schema: "billing",
                table: "BranchTaxConfigs");

            migrationBuilder.DropColumn(
                name: "Secuencial",
                schema: "billing",
                table: "BranchTaxConfigs");
        }
    }
}
