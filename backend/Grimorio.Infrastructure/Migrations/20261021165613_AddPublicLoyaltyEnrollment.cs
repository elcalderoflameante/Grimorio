using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicLoyaltyEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PhoneVerifiedAt",
                schema: "marketing",
                table: "Members",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProgramTermsVersion",
                schema: "marketing",
                table: "Members",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PhoneChallenges",
                schema: "marketing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Phone = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Sent = table.Column<bool>(type: "boolean", nullable: false),
                    MessageId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    DeliveryStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_PhoneChallenges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneChallenges_BranchId",
                schema: "marketing",
                table: "PhoneChallenges",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneChallenges_BranchId_IsDeleted",
                schema: "marketing",
                table: "PhoneChallenges",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneChallenges_BranchId_Phone_CreatedAt",
                schema: "marketing",
                table: "PhoneChallenges",
                columns: new[] { "BranchId", "Phone", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PhoneChallenges_IsDeleted",
                schema: "marketing",
                table: "PhoneChallenges",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_PhoneChallenges_MessageId",
                schema: "marketing",
                table: "PhoneChallenges",
                column: "MessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PhoneChallenges",
                schema: "marketing");

            migrationBuilder.DropColumn(
                name: "PhoneVerifiedAt",
                schema: "marketing",
                table: "Members");

            migrationBuilder.DropColumn(
                name: "ProgramTermsVersion",
                schema: "marketing",
                table: "Members");
        }
    }
}
