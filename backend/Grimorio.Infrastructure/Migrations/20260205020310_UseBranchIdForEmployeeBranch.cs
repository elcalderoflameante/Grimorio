using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UseBranchIdForEmployeeBranch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Branches_BranchIdAssigned",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_BranchIdAssigned_IdentificationNumber",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_BranchIdAssigned_IsActive",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "BranchIdAssigned",
                schema: "organization",
                table: "Employees");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchId_IdentificationNumber",
                schema: "organization",
                table: "Employees",
                columns: new[] { "BranchId", "IdentificationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchId_IsActive",
                schema: "organization",
                table: "Employees",
                columns: new[] { "BranchId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Branches_BranchId",
                schema: "organization",
                table: "Employees",
                column: "BranchId",
                principalSchema: "organization",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Branches_BranchId",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_BranchId_IdentificationNumber",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_BranchId_IsActive",
                schema: "organization",
                table: "Employees");

            migrationBuilder.AddColumn<Guid>(
                name: "BranchIdAssigned",
                schema: "organization",
                table: "Employees",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchIdAssigned_IdentificationNumber",
                schema: "organization",
                table: "Employees",
                columns: new[] { "BranchIdAssigned", "IdentificationNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BranchIdAssigned_IsActive",
                schema: "organization",
                table: "Employees",
                columns: new[] { "BranchIdAssigned", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Branches_BranchIdAssigned",
                schema: "organization",
                table: "Employees",
                column: "BranchIdAssigned",
                principalSchema: "organization",
                principalTable: "Branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
