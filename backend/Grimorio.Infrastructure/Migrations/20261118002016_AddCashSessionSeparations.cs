using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCashSessionSeparations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CashSessionSeparations",
                schema: "billing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PreviousSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CutoffUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorName = table.Column<string>(type: "character varying(201)", maxLength: 201, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PreviewJson = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_CashSessionSeparations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashSessionSeparations_CashSessions_NewSessionId",
                        column: x => x.NewSessionId,
                        principalSchema: "billing",
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashSessionSeparations_CashSessions_PreviousSessionId",
                        column: x => x.PreviousSessionId,
                        principalSchema: "billing",
                        principalTable: "CashSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionSeparations_BranchId",
                schema: "billing",
                table: "CashSessionSeparations",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionSeparations_BranchId_IsDeleted",
                schema: "billing",
                table: "CashSessionSeparations",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionSeparations_BranchId_RequestId",
                schema: "billing",
                table: "CashSessionSeparations",
                columns: new[] { "BranchId", "RequestId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionSeparations_IsDeleted",
                schema: "billing",
                table: "CashSessionSeparations",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionSeparations_NewSessionId",
                schema: "billing",
                table: "CashSessionSeparations",
                column: "NewSessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashSessionSeparations_PreviousSessionId",
                schema: "billing",
                table: "CashSessionSeparations",
                column: "PreviousSessionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashSessionSeparations",
                schema: "billing");
        }
    }
}
