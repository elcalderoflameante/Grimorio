using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversionesUnidad",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "MovimientosStock",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "OrdenCompraItems",
                schema: "compras");

            migrationBuilder.DropTable(
                name: "OrdenItems",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "RecetaIngredientes",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "StockBodega",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "OrdenesCompra",
                schema: "compras");

            migrationBuilder.DropTable(
                name: "Ordenes",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "ItemsMenu",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "ArticulosInventario",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Bodegas",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Proveedores",
                schema: "compras");

            migrationBuilder.DropTable(
                name: "CategoriasMenu",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "EstacionesTrabajo",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "CategoriasInventario",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "UnidadesMedida",
                schema: "inv");

            migrationBuilder.EnsureSchema(
                name: "billing");

            migrationBuilder.CreateTable(
                name: "CashSessions",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    OpenedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    OpeningBalance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OpenedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ClosedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ClosedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ActualCash = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    CloseNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_CashSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TaxIdType = table.Column<int>(type: "integer", nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryCategories",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_InventoryCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MeasurementUnits",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_MeasurementUnits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuCategories",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_MenuCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TaxId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Address = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    ContactName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warehouses",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Warehouses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkStations",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_WorkStations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TableId = table.Column<Guid>(type: "uuid", nullable: true),
                    WaiterId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeliveryAddress = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "billing",
                        principalTable: "Customers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Orders_RestaurantTables_TableId",
                        column: x => x.TableId,
                        principalSchema: "pos",
                        principalTable: "RestaurantTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InventoryArticles",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InternalCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinStock = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    StockAlertActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_InventoryArticles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryArticles_InventoryCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "inv",
                        principalTable: "InventoryCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryArticles_MeasurementUnits_BaseUnitId",
                        column: x => x.BaseUnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnitConversions",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OriginUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationUnitId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_UnitConversions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnitConversions_MeasurementUnits_DestinationUnitId",
                        column: x => x.DestinationUnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UnitConversions_MeasurementUnits_OriginUnitId",
                        column: x => x.OriginUnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DestinationWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalSchema: "compras",
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuCategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InternalCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AvailableForSale = table.Column<bool>(type: "boolean", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuCategories_MenuCategoryId",
                        column: x => x.MenuCategoryId,
                        principalSchema: "menu",
                        principalTable: "MenuCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MenuItems_WorkStations_StationId",
                        column: x => x.StationId,
                        principalSchema: "pos",
                        principalTable: "WorkStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrderPayments",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    CashSessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Method = table.Column<int>(type: "integer", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Change = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OrderTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_OrderPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPayments_CashSessions_CashSessionId",
                        column: x => x.CashSessionId,
                        principalSchema: "billing",
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderPayments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "billing",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrderPayments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "pos",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseQuantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_MeasurementUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StockMovements_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WarehouseStock",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_WarehouseStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WarehouseStock_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WarehouseStock_Warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalSchema: "inv",
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuantityOrdered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuantityReceived = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
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
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_MeasurementUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalSchema: "compras",
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
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
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalSchema: "menu",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "pos",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_WorkStations_StationId",
                        column: x => x.StationId,
                        principalSchema: "pos",
                        principalTable: "WorkStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecipeIngredients",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_RecipeIngredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_InventoryArticles_ArticleId",
                        column: x => x.ArticleId,
                        principalSchema: "inv",
                        principalTable: "InventoryArticles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_MeasurementUnits_UnitId",
                        column: x => x.UnitId,
                        principalSchema: "inv",
                        principalTable: "MeasurementUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeIngredients_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalSchema: "menu",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_BranchId",
                schema: "billing",
                table: "CashSessions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_BranchId_IsDeleted",
                schema: "billing",
                table: "CashSessions",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_BranchId_Status",
                schema: "billing",
                table: "CashSessions",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessions_IsDeleted",
                schema: "billing",
                table: "CashSessions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BranchId",
                schema: "billing",
                table: "Customers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BranchId_IsDeleted",
                schema: "billing",
                table: "Customers",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BranchId_TaxId",
                schema: "billing",
                table: "Customers",
                columns: new[] { "BranchId", "TaxId" },
                filter: "\"TaxId\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_IsDeleted",
                schema: "billing",
                table: "Customers",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryArticles_BaseUnitId",
                schema: "inv",
                table: "InventoryArticles",
                column: "BaseUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryArticles_BranchId",
                schema: "inv",
                table: "InventoryArticles",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryArticles_BranchId_InternalCode",
                schema: "inv",
                table: "InventoryArticles",
                columns: new[] { "BranchId", "InternalCode" },
                filter: "\"IsDeleted\" = false AND \"InternalCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryArticles_BranchId_IsDeleted",
                schema: "inv",
                table: "InventoryArticles",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryArticles_BranchId_Type_IsActive",
                schema: "inv",
                table: "InventoryArticles",
                columns: new[] { "BranchId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryArticles_CategoryId",
                schema: "inv",
                table: "InventoryArticles",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryArticles_IsDeleted",
                schema: "inv",
                table: "InventoryArticles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_BranchId",
                schema: "inv",
                table: "InventoryCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_BranchId_IsDeleted",
                schema: "inv",
                table: "InventoryCategories",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_BranchId_Name",
                schema: "inv",
                table: "InventoryCategories",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryCategories_IsDeleted",
                schema: "inv",
                table: "InventoryCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementUnits_BranchId",
                schema: "inv",
                table: "MeasurementUnits",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementUnits_BranchId_IsDeleted",
                schema: "inv",
                table: "MeasurementUnits",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementUnits_BranchId_Name",
                schema: "inv",
                table: "MeasurementUnits",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MeasurementUnits_IsDeleted",
                schema: "inv",
                table: "MeasurementUnits",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_BranchId",
                schema: "menu",
                table: "MenuCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_BranchId_IsDeleted",
                schema: "menu",
                table: "MenuCategories",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_BranchId_Name",
                schema: "menu",
                table: "MenuCategories",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_BranchId_Order",
                schema: "menu",
                table: "MenuCategories",
                columns: new[] { "BranchId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuCategories_IsDeleted",
                schema: "menu",
                table: "MenuCategories",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_BranchId",
                schema: "menu",
                table: "MenuItems",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_BranchId_IsActive",
                schema: "menu",
                table: "MenuItems",
                columns: new[] { "BranchId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_BranchId_IsDeleted",
                schema: "menu",
                table: "MenuItems",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_BranchId_MenuCategoryId",
                schema: "menu",
                table: "MenuItems",
                columns: new[] { "BranchId", "MenuCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_IsDeleted",
                schema: "menu",
                table: "MenuItems",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_MenuCategoryId",
                schema: "menu",
                table: "MenuItems",
                column: "MenuCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_StationId",
                schema: "menu",
                table: "MenuItems",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_BranchId_OrderId",
                schema: "pos",
                table: "OrderItems",
                columns: new[] { "BranchId", "OrderId" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_MenuItemId",
                schema: "pos",
                table: "OrderItems",
                column: "MenuItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                schema: "pos",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_StationId_Status",
                schema: "pos",
                table: "OrderItems",
                columns: new[] { "StationId", "Status" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_BranchId",
                schema: "billing",
                table: "OrderPayments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_BranchId_IsDeleted",
                schema: "billing",
                table: "OrderPayments",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_BranchId_PaidAt",
                schema: "billing",
                table: "OrderPayments",
                columns: new[] { "BranchId", "PaidAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_CashSessionId",
                schema: "billing",
                table: "OrderPayments",
                column: "CashSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_CustomerId",
                schema: "billing",
                table: "OrderPayments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_IsDeleted",
                schema: "billing",
                table: "OrderPayments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_OrderId",
                schema: "billing",
                table: "OrderPayments",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId_Number",
                schema: "pos",
                table: "Orders",
                columns: new[] { "BranchId", "Number" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_BranchId_Status",
                schema: "pos",
                table: "Orders",
                columns: new[] { "BranchId", "Status" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "pos",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TableId",
                schema: "pos",
                table: "Orders",
                column: "TableId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ArticleId",
                schema: "compras",
                table: "PurchaseOrderItems",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId",
                schema: "compras",
                table: "PurchaseOrderItems",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_UnitId",
                schema: "compras",
                table: "PurchaseOrderItems",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BranchId_OrderNumber",
                schema: "compras",
                table: "PurchaseOrders",
                columns: new[] { "BranchId", "OrderNumber" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_BranchId_Status",
                schema: "compras",
                table: "PurchaseOrders",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                schema: "compras",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_ArticleId",
                schema: "menu",
                table: "RecipeIngredients",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_BranchId",
                schema: "menu",
                table: "RecipeIngredients",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_BranchId_IsDeleted",
                schema: "menu",
                table: "RecipeIngredients",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_IsDeleted",
                schema: "menu",
                table: "RecipeIngredients",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_MenuItemId_ArticleId",
                schema: "menu",
                table: "RecipeIngredients",
                columns: new[] { "MenuItemId", "ArticleId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeIngredients_UnitId",
                schema: "menu",
                table: "RecipeIngredients",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ArticleId",
                schema: "inv",
                table: "StockMovements",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_BranchId",
                schema: "inv",
                table: "StockMovements",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_BranchId_ArticleId_CreatedAt",
                schema: "inv",
                table: "StockMovements",
                columns: new[] { "BranchId", "ArticleId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_BranchId_IsDeleted",
                schema: "inv",
                table: "StockMovements",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_BranchId_Type_CreatedAt",
                schema: "inv",
                table: "StockMovements",
                columns: new[] { "BranchId", "Type", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_IsDeleted",
                schema: "inv",
                table: "StockMovements",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_UnitId",
                schema: "inv",
                table: "StockMovements",
                column: "UnitId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_WarehouseId",
                schema: "inv",
                table: "StockMovements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_BranchId_Name",
                schema: "compras",
                table: "Suppliers",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_BranchId",
                schema: "inv",
                table: "UnitConversions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_BranchId_IsDeleted",
                schema: "inv",
                table: "UnitConversions",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_BranchId_OriginUnitId_DestinationUnitId",
                schema: "inv",
                table: "UnitConversions",
                columns: new[] { "BranchId", "OriginUnitId", "DestinationUnitId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_DestinationUnitId",
                schema: "inv",
                table: "UnitConversions",
                column: "DestinationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_IsDeleted",
                schema: "inv",
                table: "UnitConversions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_UnitConversions_OriginUnitId",
                schema: "inv",
                table: "UnitConversions",
                column: "OriginUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId",
                schema: "inv",
                table: "Warehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId_IsDeleted",
                schema: "inv",
                table: "Warehouses",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_BranchId_Name",
                schema: "inv",
                table: "Warehouses",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Warehouses_IsDeleted",
                schema: "inv",
                table: "Warehouses",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStock_ArticleId",
                schema: "inv",
                table: "WarehouseStock",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStock_BranchId",
                schema: "inv",
                table: "WarehouseStock",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStock_BranchId_ArticleId_WarehouseId",
                schema: "inv",
                table: "WarehouseStock",
                columns: new[] { "BranchId", "ArticleId", "WarehouseId" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStock_BranchId_IsDeleted",
                schema: "inv",
                table: "WarehouseStock",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStock_IsDeleted",
                schema: "inv",
                table: "WarehouseStock",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseStock_WarehouseId",
                schema: "inv",
                table: "WarehouseStock",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkStations_BranchId_Name",
                schema: "pos",
                table: "WorkStations",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "OrderPayments",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems",
                schema: "compras");

            migrationBuilder.DropTable(
                name: "RecipeIngredients",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "StockMovements",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "UnitConversions",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "WarehouseStock",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "CashSessions",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "PurchaseOrders",
                schema: "compras");

            migrationBuilder.DropTable(
                name: "MenuItems",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "InventoryArticles",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Warehouses",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "billing");

            migrationBuilder.DropTable(
                name: "Suppliers",
                schema: "compras");

            migrationBuilder.DropTable(
                name: "MenuCategories",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "WorkStations",
                schema: "pos");

            migrationBuilder.DropTable(
                name: "InventoryCategories",
                schema: "inv");

            migrationBuilder.DropTable(
                name: "MeasurementUnits",
                schema: "inv");

            migrationBuilder.CreateTable(
                name: "Bodegas",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EsActiva = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Ubicacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasInventario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CategoriasMenu",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    EsActiva = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasMenu", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstacionesTrabajo",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EsActiva = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                    MesaId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmadaAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DireccionEntrega = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    EntregadaAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MeseroId = table.Column<Guid>(type: "uuid", nullable: true),
                    NombreCliente = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "Proveedores",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Contacto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Direccion = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RucCedula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Telefono = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesMedida",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Simbolo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesMedida", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ItemsMenu",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CategoriaMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    EstacionId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoInterno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisponibleParaVenta = table.Column<bool>(type: "boolean", nullable: false),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                    table.ForeignKey(
                        name: "FK_ItemsMenu_EstacionesTrabajo_EstacionId",
                        column: x => x.EstacionId,
                        principalSchema: "pos",
                        principalTable: "EstacionesTrabajo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesCompra",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodegaDestinoId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaEsperada = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FechaRecepcion = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    NumeroOrden = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "ArticulosInventario",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CategoriaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadBaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    AlertaStockActiva = table.Column<bool>(type: "boolean", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CodigoInterno = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EsActivo = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StockMinimo = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                    UnidadDestinoId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadOrigenId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Factor = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "OrdenItems",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EstacionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ItemMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdenId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PrecioTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "MovimientosStock",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticuloId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodegaId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CantidadBase = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Observacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PedidoItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Referencia = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Tipo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "OrdenCompraItems",
                schema: "compras",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticuloId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrdenCompraId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CantidadPedida = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CantidadRecibida = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Observacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PrecioTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "RecetaIngredientes",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticuloId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemMenuId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnidadId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Observacion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "StockBodega",
                schema: "inv",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ArticuloId = table.Column<Guid>(type: "uuid", nullable: false),
                    BodegaId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UltimaActualizacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
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
                name: "IX_EstacionesTrabajo_BranchId_Nombre",
                schema: "pos",
                table: "EstacionesTrabajo",
                columns: new[] { "BranchId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

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
                name: "IX_ItemsMenu_EstacionId",
                schema: "menu",
                table: "ItemsMenu",
                column: "EstacionId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemsMenu_IsDeleted",
                schema: "menu",
                table: "ItemsMenu",
                column: "IsDeleted");

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

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_BranchId_Nombre",
                schema: "compras",
                table: "Proveedores",
                columns: new[] { "BranchId", "Nombre" },
                unique: true,
                filter: "\"IsDeleted\" = false");

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
    }
}
