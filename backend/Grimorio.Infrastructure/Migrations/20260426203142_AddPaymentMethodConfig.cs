using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentMethodConfig : Migration
    {
        // GUIDs fijos para los 4 métodos predeterminados (usados en la migración de datos)
        private const string IdEfectivo     = "a0000000-0000-0000-0000-000000000001";
        private const string IdTarjeta      = "a0000000-0000-0000-0000-000000000002";
        private const string IdTransferencia = "a0000000-0000-0000-0000-000000000003";
        private const string IdQR           = "a0000000-0000-0000-0000-000000000004";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Crear la tabla de métodos de pago
            migrationBuilder.CreateTable(
                name: "PaymentMethodConfigs",
                schema: "billing",
                columns: table => new
                {
                    Id        = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name      = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Color     = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsCash    = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive  = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodConfigs", x => x.Id);
                });

            // 2. Sembrar los 4 métodos predeterminados
            migrationBuilder.Sql($@"
                INSERT INTO billing.""PaymentMethodConfigs"" (""Id"", ""Name"", ""Color"", ""IsCash"", ""IsActive"", ""SortOrder"", ""IsDeleted"")
                VALUES
                    ('{IdEfectivo}',      'Efectivo',       '#52c41a', true,  true, 1, false),
                    ('{IdTarjeta}',       'Tarjeta',        '#1677ff', false, true, 2, false),
                    ('{IdTransferencia}', 'Transferencia',  '#722ed1', false, true, 3, false),
                    ('{IdQR}',            'QR',             '#13c2c2', false, true, 4, false)
                ON CONFLICT DO NOTHING;
            ");

            // 3. Agregar la columna como nullable primero para poder migrar datos
            migrationBuilder.AddColumn<Guid>(
                name: "PaymentMethodConfigId",
                schema: "billing",
                table: "PaymentLines",
                type: "uuid",
                nullable: true);

            // 4. Migrar datos existentes: mapear el enum Method al nuevo FK
            migrationBuilder.Sql($@"
                UPDATE billing.""PaymentLines"" SET ""PaymentMethodConfigId"" =
                    CASE ""Method""
                        WHEN 1 THEN '{IdEfectivo}'::uuid
                        WHEN 2 THEN '{IdTarjeta}'::uuid
                        WHEN 3 THEN '{IdTransferencia}'::uuid
                        WHEN 4 THEN '{IdQR}'::uuid
                        ELSE '{IdEfectivo}'::uuid
                    END
                WHERE ""PaymentMethodConfigId"" IS NULL;
            ");

            // 5. Hacer NOT NULL ahora que todos los registros tienen valor
            migrationBuilder.AlterColumn<Guid>(
                name: "PaymentMethodConfigId",
                schema: "billing",
                table: "PaymentLines",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 6. Eliminar la columna de enum antigua
            migrationBuilder.DropColumn(
                name: "Method",
                schema: "billing",
                table: "PaymentLines");

            // 7. Índices
            migrationBuilder.CreateIndex(
                name: "IX_PaymentLines_PaymentMethodConfigId",
                schema: "billing",
                table: "PaymentLines",
                column: "PaymentMethodConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodConfigs_SortOrder",
                schema: "billing",
                table: "PaymentMethodConfigs",
                column: "SortOrder");

            // 8. FK
            migrationBuilder.AddForeignKey(
                name: "FK_PaymentLines_PaymentMethodConfigs_PaymentMethodConfigId",
                schema: "billing",
                table: "PaymentLines",
                column: "PaymentMethodConfigId",
                principalSchema: "billing",
                principalTable: "PaymentMethodConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentLines_PaymentMethodConfigs_PaymentMethodConfigId",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.DropIndex(
                name: "IX_PaymentLines_PaymentMethodConfigId",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.AddColumn<int>(
                name: "Method",
                schema: "billing",
                table: "PaymentLines",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.Sql($@"
                UPDATE billing.""PaymentLines"" SET ""Method"" =
                    CASE ""PaymentMethodConfigId""
                        WHEN '{IdEfectivo}'::uuid      THEN 1
                        WHEN '{IdTarjeta}'::uuid       THEN 2
                        WHEN '{IdTransferencia}'::uuid THEN 3
                        WHEN '{IdQR}'::uuid            THEN 4
                        ELSE 1
                    END;
            ");

            migrationBuilder.DropColumn(
                name: "PaymentMethodConfigId",
                schema: "billing",
                table: "PaymentLines");

            migrationBuilder.DropTable(
                name: "PaymentMethodConfigs",
                schema: "billing");
        }
    }
}
