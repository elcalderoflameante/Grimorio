using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeePersonalInformation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CivilStatus",
                schema: "organization",
                table: "Employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                schema: "organization",
                table: "Employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPerson",
                schema: "organization",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactPhone",
                schema: "organization",
                table: "Employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "EmergencyContactRelationship",
                schema: "organization",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Nationality",
                schema: "organization",
                table: "Employees",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Photo",
                schema: "organization",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sex",
                schema: "organization",
                table: "Employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CivilStatus",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPerson",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactPhone",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "EmergencyContactRelationship",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Nationality",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Photo",
                schema: "organization",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Sex",
                schema: "organization",
                table: "Employees");
        }
    }
}
