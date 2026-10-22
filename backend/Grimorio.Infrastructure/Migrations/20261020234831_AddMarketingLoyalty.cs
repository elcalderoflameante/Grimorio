using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingLoyalty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "marketing");

            migrationBuilder.AddColumn<Guid>(
                name: "LoyaltyMemberId",
                schema: "billing",
                table: "OrderPayments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Members",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardCode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    PublicToken = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MarketingConsent = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Members_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "billing",
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Programs",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumSpend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IncludeTax = table.Column<bool>(type: "boolean", nullable: false),
                    RewardValidityDays = table.Column<int>(type: "integer", nullable: false),
                    TiersJson = table.Column<string>(type: "jsonb", nullable: false),
                    ExcludedItemIdsJson = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_Programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contributions",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderPaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EligibleAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    EligibleSubtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_Contributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contributions_Members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "marketing",
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contributions_OrderPayments_OrderPaymentId",
                        column: x => x.OrderPaymentId,
                        principalSchema: "billing",
                        principalTable: "OrderPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contributions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "pos",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rewards",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    VisitNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RuleSnapshotJson = table.Column<string>(type: "jsonb", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_Rewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rewards_Members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "marketing",
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visits",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QualifiedSpend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumSpend = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
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
                    table.PrimaryKey("PK_Visits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visits_Members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "marketing",
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Visits_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "pos",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderPayments_LoyaltyMemberId",
                schema: "billing",
                table: "OrderPayments",
                column: "LoyaltyMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_BranchId",
                schema: "marketing",
                table: "Contributions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_BranchId_IsDeleted",
                schema: "marketing",
                table: "Contributions",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_IsDeleted",
                schema: "marketing",
                table: "Contributions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_MemberId_OrderId_BusinessDate",
                schema: "marketing",
                table: "Contributions",
                columns: new[] { "MemberId", "OrderId", "BusinessDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_OrderId",
                schema: "marketing",
                table: "Contributions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Contributions_OrderPaymentId",
                schema: "marketing",
                table: "Contributions",
                column: "OrderPaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_BranchId",
                schema: "marketing",
                table: "Members",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_BranchId_CustomerId",
                schema: "marketing",
                table: "Members",
                columns: new[] { "BranchId", "CustomerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_BranchId_IsDeleted",
                schema: "marketing",
                table: "Members",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Members_CardCode",
                schema: "marketing",
                table: "Members",
                column: "CardCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Members_CustomerId",
                schema: "marketing",
                table: "Members",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_IsDeleted",
                schema: "marketing",
                table: "Members",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Members_PublicToken",
                schema: "marketing",
                table: "Members",
                column: "PublicToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_BranchId",
                schema: "marketing",
                table: "Programs",
                column: "BranchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Programs_BranchId_IsDeleted",
                schema: "marketing",
                table: "Programs",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Programs_IsDeleted",
                schema: "marketing",
                table: "Programs",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_BranchId",
                schema: "marketing",
                table: "Rewards",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_BranchId_IsDeleted",
                schema: "marketing",
                table: "Rewards",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_IsDeleted",
                schema: "marketing",
                table: "Rewards",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_MemberId_VisitNumber",
                schema: "marketing",
                table: "Rewards",
                columns: new[] { "MemberId", "VisitNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_BranchId",
                schema: "marketing",
                table: "Visits",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_BranchId_IsDeleted",
                schema: "marketing",
                table: "Visits",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_IsDeleted",
                schema: "marketing",
                table: "Visits",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_MemberId_BusinessDate",
                schema: "marketing",
                table: "Visits",
                columns: new[] { "MemberId", "BusinessDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visits_OrderId",
                schema: "marketing",
                table: "Visits",
                column: "OrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderPayments_Members_LoyaltyMemberId",
                schema: "billing",
                table: "OrderPayments",
                column: "LoyaltyMemberId",
                principalSchema: "marketing",
                principalTable: "Members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderPayments_Members_LoyaltyMemberId",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.DropTable(
                name: "Contributions",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "Programs",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "Rewards",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "Visits",
                schema: "marketing");

            migrationBuilder.DropTable(
                name: "Members",
                schema: "marketing");

            migrationBuilder.DropIndex(
                name: "IX_OrderPayments_LoyaltyMemberId",
                schema: "billing",
                table: "OrderPayments");

            migrationBuilder.DropColumn(
                name: "LoyaltyMemberId",
                schema: "billing",
                table: "OrderPayments");
        }
    }
}
