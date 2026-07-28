using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinanceCostCentersAndExpenseCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "finance");

            migrationBuilder.CreateTable(
                name: "CostCenters",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                schema: "finance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_BranchId",
                schema: "finance",
                table: "CostCenters",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_BranchId_Code",
                schema: "finance",
                table: "CostCenters",
                columns: new[] { "BranchId", "Code" },
                unique: true,
                filter: "\"Code\" IS NOT NULL AND \"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_BranchId_DisplayOrder",
                schema: "finance",
                table: "CostCenters",
                columns: new[] { "BranchId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_BranchId_IsDeleted",
                schema: "finance",
                table: "CostCenters",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_BranchId_Name",
                schema: "finance",
                table: "CostCenters",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_IsDeleted",
                schema: "finance",
                table: "CostCenters",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_BranchId",
                schema: "finance",
                table: "ExpenseCategories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_BranchId_DisplayOrder",
                schema: "finance",
                table: "ExpenseCategories",
                columns: new[] { "BranchId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_BranchId_IsDeleted",
                schema: "finance",
                table: "ExpenseCategories",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_BranchId_Name",
                schema: "finance",
                table: "ExpenseCategories",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_BranchId_Type",
                schema: "finance",
                table: "ExpenseCategories",
                columns: new[] { "BranchId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_IsDeleted",
                schema: "finance",
                table: "ExpenseCategories",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CostCenters",
                schema: "finance");

            migrationBuilder.DropTable(
                name: "ExpenseCategories",
                schema: "finance");
        }
    }
}
