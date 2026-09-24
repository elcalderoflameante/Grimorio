using Grimorio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations;

[DbContext(typeof(GrimorioDbContext))]
[Migration("20260924000100_AddPublicOrderSubmissionId")]
public partial class AddPublicOrderSubmissionId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "PublicSubmissionId",
            schema: "pos",
            table: "Orders",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_BranchId_PublicSubmissionId",
            schema: "pos",
            table: "Orders",
            columns: new[] { "BranchId", "PublicSubmissionId" },
            unique: true,
            filter: "\"PublicSubmissionId\" IS NOT NULL AND \"IsDeleted\" = false");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Orders_BranchId_PublicSubmissionId",
            schema: "pos",
            table: "Orders");

        migrationBuilder.DropColumn(
            name: "PublicSubmissionId",
            schema: "pos",
            table: "Orders");
    }
}
