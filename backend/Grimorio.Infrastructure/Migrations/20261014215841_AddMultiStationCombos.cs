using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiStationCombos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCombo",
                schema: "menu",
                table: "MenuItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "MenuComboComponents",
                schema: "menu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ComboId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_MenuComboComponents", x => x.Id);
                    table.CheckConstraint("CK_MenuComboComponents_Quantity", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_MenuComboComponents_MenuItems_ComboId",
                        column: x => x.ComboId,
                        principalSchema: "menu",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuComboComponents_MenuItems_ComponentItemId",
                        column: x => x.ComponentItemId,
                        principalSchema: "menu",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrderItemPreparationTasks",
                schema: "pos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    OrderItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    StationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ComponentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ComboName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    QuantityPerCombo = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RecipeSnapshot = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_OrderItemPreparationTasks", x => x.Id);
                    table.CheckConstraint("CK_OrderItemPreparationTasks_Quantity", "\"QuantityPerCombo\" > 0");
                    table.ForeignKey(
                        name: "FK_OrderItemPreparationTasks_MenuItems_ComponentItemId",
                        column: x => x.ComponentItemId,
                        principalSchema: "menu",
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrderItemPreparationTasks_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalSchema: "pos",
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItemPreparationTasks_WorkStations_StationId",
                        column: x => x.StationId,
                        principalSchema: "pos",
                        principalTable: "WorkStations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuComboComponents_BranchId",
                schema: "menu",
                table: "MenuComboComponents",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuComboComponents_BranchId_IsDeleted",
                schema: "menu",
                table: "MenuComboComponents",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MenuComboComponents_ComboId_ComponentItemId",
                schema: "menu",
                table: "MenuComboComponents",
                columns: new[] { "ComboId", "ComponentItemId" },
                unique: true,
                filter: "NOT \"IsDeleted\"");

            migrationBuilder.CreateIndex(
                name: "IX_MenuComboComponents_ComponentItemId",
                schema: "menu",
                table: "MenuComboComponents",
                column: "ComponentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuComboComponents_IsDeleted",
                schema: "menu",
                table: "MenuComboComponents",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemPreparationTasks_BranchId",
                schema: "pos",
                table: "OrderItemPreparationTasks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemPreparationTasks_BranchId_IsDeleted",
                schema: "pos",
                table: "OrderItemPreparationTasks",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemPreparationTasks_BranchId_StationId_Status",
                schema: "pos",
                table: "OrderItemPreparationTasks",
                columns: new[] { "BranchId", "StationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemPreparationTasks_ComponentItemId",
                schema: "pos",
                table: "OrderItemPreparationTasks",
                column: "ComponentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemPreparationTasks_IsDeleted",
                schema: "pos",
                table: "OrderItemPreparationTasks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemPreparationTasks_OrderItemId_ComponentItemId",
                schema: "pos",
                table: "OrderItemPreparationTasks",
                columns: new[] { "OrderItemId", "ComponentItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItemPreparationTasks_StationId",
                schema: "pos",
                table: "OrderItemPreparationTasks",
                column: "StationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuComboComponents",
                schema: "menu");

            migrationBuilder.DropTable(
                name: "OrderItemPreparationTasks",
                schema: "pos");

            migrationBuilder.DropColumn(
                name: "IsCombo",
                schema: "menu",
                table: "MenuItems");
        }
    }
}
