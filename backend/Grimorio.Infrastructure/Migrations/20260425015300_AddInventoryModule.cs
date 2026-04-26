using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inv");

            migrationBuilder.CreateTable(
                name: "Bodegas",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EsActiva = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Bodegas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasInventario",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_CategoriasInventario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesMedida",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Simbolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_UnidadesMedida", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArticulosInventario",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CodigoInterno = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    StockMinimo = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AlertaStockActiva = table.Column<bool>(type: "boolean", nullable: false),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ArticulosInventario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticulosInventario_CategoriasInventario_CategoriaId",
                        column: x => x.CategoriaId,
                        principalSchema: "inv",
                        principalTable: "CategoriasInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ArticulosInventario_UnidadesMedida_UnidadBaseId",
                        column: x => x.UnidadBaseId,
                        principalSchema: "inv",
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConversionesUnidad",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    UnidadOrigenId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadDestinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Factor = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
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
                    table.PrimaryKey("PK_ConversionesUnidad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConversionesUnidad_UnidadesMedida_UnidadDestinoId",
                        column: x => x.UnidadDestinoId,
                        principalSchema: "inv",
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConversionesUnidad_UnidadesMedida_UnidadOrigenId",
                        column: x => x.UnidadOrigenId,
                        principalSchema: "inv",
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticuloId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodegaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CantidadBase = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Referencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PedidoItemId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_MovimientosStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_ArticulosInventario_ArticuloId",
                        column: x => x.ArticuloId,
                        principalSchema: "inv",
                        principalTable: "ArticulosInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_Bodegas_BodegaId",
                        column: x => x.BodegaId,
                        principalSchema: "inv",
                        principalTable: "Bodegas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosStock_UnidadesMedida_UnidadId",
                        column: x => x.UnidadId,
                        principalSchema: "inv",
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockBodega",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticuloId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodegaId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UltimaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_StockBodega", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockBodega_ArticulosInventario_ArticuloId",
                        column: x => x.ArticuloId,
                        principalSchema: "inv",
                        principalTable: "ArticulosInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockBodega_Bodegas_BodegaId",
                        column: x => x.BodegaId,
                        principalSchema: "inv",
                        principalTable: "Bodegas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosInventario_BranchId",
                schema: "inv",
                table: "ArticulosInventario",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosInventario_BranchId_CodigoInterno",
                schema: "inv",
                table: "ArticulosInventario",
                columns: new[] { "BranchId", "CodigoInterno" },
                filter: "\"IsDeleted\" = false AND \"CodigoInterno\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosInventario_BranchId_IsDeleted",
                schema: "inv",
                table: "ArticulosInventario",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosInventario_BranchId_Tipo_EsActivo",
                schema: "inv",
                table: "ArticulosInventario",
                columns: new[] { "BranchId", "Tipo", "EsActivo" });

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosInventario_CategoriaId",
                schema: "inv",
                table: "ArticulosInventario",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosInventario_IsDeleted",
                schema: "inv",
                table: "ArticulosInventario",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ArticulosInventario_UnidadBaseId",
                schema: "inv",
                table: "ArticulosInventario",
                column: "UnidadBaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_BranchId",
                schema: "inv",
                table: "Bodegas",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_BranchId_IsDeleted",
                schema: "inv",
                table: "Bodegas",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_BranchId_Nombre",
                schema: "inv",
                table: "Bodegas",
                columns: new[] { "BranchId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Bodegas_IsDeleted",
                schema: "inv",
                table: "Bodegas",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasInventario_BranchId",
                schema: "inv",
                table: "CategoriasInventario",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasInventario_BranchId_IsDeleted",
                schema: "inv",
                table: "CategoriasInventario",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasInventario_BranchId_Nombre",
                schema: "inv",
                table: "CategoriasInventario",
                columns: new[] { "BranchId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasInventario_IsDeleted",
                schema: "inv",
                table: "CategoriasInventario",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionesUnidad_BranchId",
                schema: "inv",
                table: "ConversionesUnidad",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionesUnidad_BranchId_IsDeleted",
                schema: "inv",
                table: "ConversionesUnidad",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ConversionesUnidad_BranchId_UnidadOrigenId_UnidadDestinoId",
                schema: "inv",
                table: "ConversionesUnidad",
                columns: new[] { "BranchId", "UnidadOrigenId", "UnidadDestinoId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionesUnidad_IsDeleted",
                schema: "inv",
                table: "ConversionesUnidad",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionesUnidad_UnidadDestinoId",
                schema: "inv",
                table: "ConversionesUnidad",
                column: "UnidadDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConversionesUnidad_UnidadOrigenId",
                schema: "inv",
                table: "ConversionesUnidad",
                column: "UnidadOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ArticuloId",
                schema: "inv",
                table: "MovimientosStock",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_BodegaId",
                schema: "inv",
                table: "MovimientosStock",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_BranchId",
                schema: "inv",
                table: "MovimientosStock",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_BranchId_ArticuloId_CreatedAt",
                schema: "inv",
                table: "MovimientosStock",
                columns: new[] { "BranchId", "ArticuloId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_BranchId_IsDeleted",
                schema: "inv",
                table: "MovimientosStock",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_BranchId_Tipo_CreatedAt",
                schema: "inv",
                table: "MovimientosStock",
                columns: new[] { "BranchId", "Tipo", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_IsDeleted",
                schema: "inv",
                table: "MovimientosStock",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_UnidadId",
                schema: "inv",
                table: "MovimientosStock",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_ArticuloId",
                schema: "inv",
                table: "StockBodega",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_BodegaId",
                schema: "inv",
                table: "StockBodega",
                column: "BodegaId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_BranchId",
                schema: "inv",
                table: "StockBodega",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_BranchId_ArticuloId_BodegaId",
                schema: "inv",
                table: "StockBodega",
                columns: new[] { "BranchId", "ArticuloId", "BodegaId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_BranchId_IsDeleted",
                schema: "inv",
                table: "StockBodega",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StockBodega_IsDeleted",
                schema: "inv",
                table: "StockBodega",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedida_BranchId",
                schema: "inv",
                table: "UnidadesMedida",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedida_BranchId_IsDeleted",
                schema: "inv",
                table: "UnidadesMedida",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedida_BranchId_Nombre",
                schema: "inv",
                table: "UnidadesMedida",
                columns: new[] { "BranchId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedida_IsDeleted",
                schema: "inv",
                table: "UnidadesMedida",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversionesUnidad",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "MovimientosStock",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "StockBodega",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "ArticulosInventario",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Bodegas",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "CategoriasInventario",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "UnidadesMedida",
                schema: "inv");
        }
    }
}
