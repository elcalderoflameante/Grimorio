using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComprasModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "compras");

            migrationBuilder.CreateTable(
                name: "Proveedores",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RucCedula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Direccion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Contacto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesCompra",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    NumeroOrden = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEsperada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BodegaDestinoId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesCompra_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalSchema: "compras",
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenCompraItems",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdenCompraId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticuloId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CantidadPedida = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    PrecioTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenCompraItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenCompraItems_ArticulosInventario_ArticuloId",
                        column: x => x.ArticuloId,
                        principalSchema: "inv",
                        principalTable: "ArticulosInventario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenCompraItems_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalSchema: "compras",
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenCompraItems_UnidadesMedida_UnidadId",
                        column: x => x.UnidadId,
                        principalSchema: "inv",
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompraItems_ArticuloId",
                schema: "compras",
                table: "OrdenCompraItems",
                column: "ArticuloId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompraItems_OrdenCompraId",
                schema: "compras",
                table: "OrdenCompraItems",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenCompraItems_UnidadId",
                schema: "compras",
                table: "OrdenCompraItems",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_BranchId_Estado",
                schema: "compras",
                table: "OrdenesCompra",
                columns: new[] { "BranchId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_BranchId_NumeroOrden",
                schema: "compras",
                table: "OrdenesCompra",
                columns: new[] { "BranchId", "NumeroOrden" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_ProveedorId",
                schema: "compras",
                table: "OrdenesCompra",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_BranchId_Nombre",
                schema: "compras",
                table: "Proveedores",
                columns: new[] { "BranchId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdenCompraItems",
                schema: "compras");

            migrationBuilder.DropTable(
                name: "OrdenesCompra",
                schema: "compras");

            migrationBuilder.DropTable(
                name: "Proveedores",
                schema: "compras");
        }
    }
}
