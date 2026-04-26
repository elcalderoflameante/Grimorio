using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPOSOrdersAndEstaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PosX",
                schema: "pos",
                table: "RestaurantTables",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PosY",
                schema: "pos",
                table: "RestaurantTables",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "EstacionId",
                schema: "menu",
                table: "ItemsMenu",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EstacionesTrabajo",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EsActiva = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_EstacionesTrabajo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ordenes",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MesaId = table.Column<Guid>(type: "uuid", nullable: true),
                    MeseroId = table.Column<Guid>(type: "uuid", nullable: true),
                    NombreCliente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DireccionEntrega = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ConfirmadaAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EntregadaAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Ordenes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ordenes_RestaurantTables_MesaId",
                        column: x => x.MesaId,
                        principalSchema: "pos",
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrdenItems",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdenId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstacionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrecioTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_OrdenItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenItems_EstacionesTrabajo_EstacionId",
                        column: x => x.EstacionId,
                        principalSchema: "pos",
                        principalTable: "EstacionesTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrdenItems_ItemsMenu_ItemMenuId",
                        column: x => x.ItemMenuId,
                        principalSchema: "menu",
                        principalTable: "ItemsMenu",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrdenItems_Ordenes_OrdenId",
                        column: x => x.OrdenId,
                        principalSchema: "pos",
                        principalTable: "Ordenes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemsMenu_EstacionId",
                schema: "menu",
                table: "ItemsMenu",
                column: "EstacionId");

            migrationBuilder.CreateIndex(
                name: "IX_EstacionesTrabajo_BranchId_Nombre",
                schema: "pos",
                table: "EstacionesTrabajo",
                columns: new[] { "BranchId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_BranchId_Estado",
                schema: "pos",
                table: "Ordenes",
                columns: new[] { "BranchId", "Estado" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_BranchId_Numero",
                schema: "pos",
                table: "Ordenes",
                columns: new[] { "BranchId", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_Ordenes_MesaId",
                schema: "pos",
                table: "Ordenes",
                column: "MesaId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenItems_BranchId_OrdenId",
                schema: "pos",
                table: "OrdenItems",
                columns: new[] { "BranchId", "OrdenId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenItems_EstacionId_Estado",
                schema: "pos",
                table: "OrdenItems",
                columns: new[] { "EstacionId", "Estado" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenItems_ItemMenuId",
                schema: "pos",
                table: "OrdenItems",
                column: "ItemMenuId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenItems_OrdenId",
                schema: "pos",
                table: "OrdenItems",
                column: "OrdenId");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemsMenu_EstacionesTrabajo_EstacionId",
                schema: "menu",
                table: "ItemsMenu",
                column: "EstacionId",
                principalSchema: "pos",
                principalTable: "EstacionesTrabajo",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemsMenu_EstacionesTrabajo_EstacionId",
                schema: "menu",
                table: "ItemsMenu");

            migrationBuilder.DropTable(
                name: "OrdenItems",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "EstacionesTrabajo",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "Ordenes",
                schema: "pos");

            migrationBuilder.DropIndex(
                name: "IX_ItemsMenu_EstacionId",
                schema: "menu",
                table: "ItemsMenu");

            migrationBuilder.DropColumn(
                name: "PosX",
                schema: "pos",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "PosY",
                schema: "pos",
                table: "RestaurantTables");

            migrationBuilder.DropColumn(
                name: "EstacionId",
                schema: "menu",
                table: "ItemsMenu");
        }
    }
}
