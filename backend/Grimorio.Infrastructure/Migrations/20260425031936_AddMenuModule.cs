using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "menu");

            migrationBuilder.CreateTable(
                name: "CategoriasMenu",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    EsActiva = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_CategoriasMenu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemsMenu",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoriaMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CodigoInterno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Precio = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    DisponibleParaVenta = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_ItemsMenu", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemsMenu_CategoriasMenu_CategoriaMenuId",
                        column: x => x.CategoriaMenuId,
                        principalSchema: "menu",
                        principalTable: "CategoriasMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecetaIngredientes",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticuloId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_RecetaIngredientes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecetaIngredientes_ArticulosInventario_ArticuloId",
                        column: x => x.ArticuloId,
                        principalSchema: "inv",
                        principalTable: "ArticulosInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecetaIngredientes_ItemsMenu_ItemMenuId",
                        column: x => x.ItemMenuId,
                        principalSchema: "menu",
                        principalTable: "ItemsMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecetaIngredientes_UnidadesMedida_UnidadId",
                        column: x => x.UnidadId,
                        principalSchema: "inv",
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasMenu_BranchId",
                schema: "menu",
                table: "CategoriasMenu",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasMenu_BranchId_IsDeleted",
                schema: "menu",
                table: "CategoriasMenu",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasMenu_BranchId_Nombre",
                schema: "menu",
                table: "CategoriasMenu",
                columns: new[] { "BranchId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasMenu_BranchId_Orden",
                schema: "menu",
                table: "CategoriasMenu",
                columns: new[] { "BranchId", "Orden" });

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasMenu_IsDeleted",
                schema: "menu",
                table: "CategoriasMenu",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsMenu_BranchId",
                schema: "menu",
                table: "ItemsMenu",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsMenu_BranchId_CategoriaMenuId",
                schema: "menu",
                table: "ItemsMenu",
                columns: new[] { "BranchId", "CategoriaMenuId" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsMenu_BranchId_EsActivo",
                schema: "menu",
                table: "ItemsMenu",
                columns: new[] { "BranchId", "EsActivo" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsMenu_BranchId_IsDeleted",
                schema: "menu",
                table: "ItemsMenu",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsMenu_CategoriaMenuId",
                schema: "menu",
                table: "ItemsMenu",
                column: "CategoriaMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsMenu_IsDeleted",
                schema: "menu",
                table: "ItemsMenu",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaIngredientes_ArticuloId",
                schema: "menu",
                table: "RecetaIngredientes",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaIngredientes_BranchId",
                schema: "menu",
                table: "RecetaIngredientes",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaIngredientes_BranchId_IsDeleted",
                schema: "menu",
                table: "RecetaIngredientes",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_RecetaIngredientes_IsDeleted",
                schema: "menu",
                table: "RecetaIngredientes",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaIngredientes_ItemMenuId_ArticuloId",
                schema: "menu",
                table: "RecetaIngredientes",
                columns: new[] { "ItemMenuId", "ArticuloId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaIngredientes_UnidadId",
                schema: "menu",
                table: "RecetaIngredientes",
                column: "UnidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecetaIngredientes",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "ItemsMenu",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "CategoriasMenu",
                schema: "menu");
        }
    }
}
