using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RebuildAttendanceFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeeClockings_BranchId_ClockInTime",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeClockings_EmployeeId_ClockInTime",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "IsLate",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.RenameColumn(
                name: "Notes",
                schema: "organization",
                table: "EmployeeClockings",
                newName: "AdministrativeNotes");

            migrationBuilder.RenameColumn(
                name: "ClockOutTime",
                schema: "organization",
                table: "EmployeeClockings",
                newName: "ClockOutTimeUtc");

            migrationBuilder.RenameColumn(
                name: "ClockInTime",
                schema: "organization",
                table: "EmployeeClockings",
                newName: "ClockInTimeUtc");

            migrationBuilder.Sql("""
                ALTER TABLE organization."EmployeeClockings"
                ALTER COLUMN "LateMinutes" TYPE integer
                USING COALESCE(ROUND(EXTRACT(EPOCH FROM "LateMinutes") / 60.0), 0)::integer;
                ALTER TABLE organization."EmployeeClockings"
                ALTER COLUMN "LateMinutes" SET DEFAULT 0;
                ALTER TABLE organization."EmployeeClockings"
                ALTER COLUMN "LateMinutes" SET NOT NULL;
                """);

            migrationBuilder.AddColumn<int>(
                name: "BreakMinutes",
                schema: "organization",
                table: "EmployeeClockings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ClockInEvidencePath",
                schema: "organization",
                table: "EmployeeClockings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ClockInKioskDeviceId",
                schema: "organization",
                table: "EmployeeClockings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockOutEvidencePath",
                schema: "organization",
                table: "EmployeeClockings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockInMethod",
                schema: "organization",
                table: "EmployeeClockings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Manual");

            migrationBuilder.AddColumn<Guid>(
                name: "ClockOutKioskDeviceId",
                schema: "organization",
                table: "EmployeeClockings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClockOutMethod",
                schema: "organization",
                table: "EmployeeClockings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EarlyArrivalMinutes",
                schema: "organization",
                table: "EmployeeClockings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OvertimeMinutes",
                schema: "organization",
                table: "EmployeeClockings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledEndTime",
                schema: "organization",
                table: "EmployeeClockings",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "ScheduledStartTime",
                schema: "organization",
                table: "EmployeeClockings",
                type: "interval",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "organization",
                table: "EmployeeClockings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.AddColumn<DateOnly>(
                name: "WorkDate",
                schema: "organization",
                table: "EmployeeClockings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkedMinutes",
                schema: "organization",
                table: "EmployeeClockings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE organization."EmployeeClockings"
                SET "WorkDate" = ("ClockInTimeUtc" AT TIME ZONE 'America/Guayaquil')::date,
                    "Status" = CASE WHEN "ClockOutTimeUtc" IS NULL THEN 'Working' ELSE 'Completed' END;
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "WorkDate",
                schema: "organization",
                table: "EmployeeClockings",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceKioskDevices",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DeviceIdentifier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApiKeyHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSeenAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_AttendanceKioskDevices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeClockingBreaks",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EmployeeClockingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EndMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    StartKioskDeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    EndKioskDeviceId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartEvidencePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EndEvidencePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    ClosedAutomaticallyOnClockOut = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeClockingBreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeClockingBreaks_EmployeeClockings_EmployeeClockingId",
                        column: x => x.EmployeeClockingId,
                        principalSchema: "organization",
                        principalTable: "EmployeeClockings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFacialTemplates",
                schema: "organization",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedEmbedding = table.Column<string>(type: "text", nullable: false),
                    ModelVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EmbeddingDimensions = table.Column<int>(type: "integer", nullable: false),
                    SampleCount = table.Column<int>(type: "integer", nullable: false),
                    EnrolledAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EnrolledByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeFacialTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFacialTemplates_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClockings_BranchId_WorkDate",
                schema: "organization",
                table: "EmployeeClockings",
                columns: new[] { "BranchId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClockings_EmployeeId_WorkDate",
                schema: "organization",
                table: "EmployeeClockings",
                columns: new[] { "EmployeeId", "WorkDate" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceKioskDevices_BranchId",
                schema: "organization",
                table: "AttendanceKioskDevices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceKioskDevices_BranchId_IsDeleted",
                schema: "organization",
                table: "AttendanceKioskDevices",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceKioskDevices_BranchId_Status",
                schema: "organization",
                table: "AttendanceKioskDevices",
                columns: new[] { "BranchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceKioskDevices_DeviceIdentifier",
                schema: "organization",
                table: "AttendanceKioskDevices",
                column: "DeviceIdentifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceKioskDevices_IsDeleted",
                schema: "organization",
                table: "AttendanceKioskDevices",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClockingBreaks_BranchId",
                schema: "organization",
                table: "EmployeeClockingBreaks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClockingBreaks_BranchId_IsDeleted",
                schema: "organization",
                table: "EmployeeClockingBreaks",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClockingBreaks_EmployeeClockingId",
                schema: "organization",
                table: "EmployeeClockingBreaks",
                column: "EmployeeClockingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClockingBreaks_IsDeleted",
                schema: "organization",
                table: "EmployeeClockingBreaks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFacialTemplates_BranchId",
                schema: "organization",
                table: "EmployeeFacialTemplates",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFacialTemplates_BranchId_IsDeleted",
                schema: "organization",
                table: "EmployeeFacialTemplates",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFacialTemplates_EmployeeId_ModelVersion",
                schema: "organization",
                table: "EmployeeFacialTemplates",
                columns: new[] { "EmployeeId", "ModelVersion" },
                unique: true,
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFacialTemplates_IsDeleted",
                schema: "organization",
                table: "EmployeeFacialTemplates",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceKioskDevices",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "EmployeeClockingBreaks",
                schema: "organization");

            migrationBuilder.DropTable(
                name: "EmployeeFacialTemplates",
                schema: "organization");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeClockings_BranchId_WorkDate",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeClockings_EmployeeId_WorkDate",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "BreakMinutes",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "ClockInEvidencePath",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "ClockInKioskDeviceId",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "ClockInMethod",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "ClockOutKioskDeviceId",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "ClockOutEvidencePath",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "ClockOutMethod",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "EarlyArrivalMinutes",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "OvertimeMinutes",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "ScheduledEndTime",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "ScheduledStartTime",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "WorkDate",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.DropColumn(
                name: "WorkedMinutes",
                schema: "organization",
                table: "EmployeeClockings");

            migrationBuilder.RenameColumn(
                name: "ClockOutTimeUtc",
                schema: "organization",
                table: "EmployeeClockings",
                newName: "ClockOutTime");

            migrationBuilder.RenameColumn(
                name: "AdministrativeNotes",
                schema: "organization",
                table: "EmployeeClockings",
                newName: "Notes");

            migrationBuilder.RenameColumn(
                name: "ClockInTimeUtc",
                schema: "organization",
                table: "EmployeeClockings",
                newName: "ClockInTime");

            migrationBuilder.Sql("""
                ALTER TABLE organization."EmployeeClockings"
                ALTER COLUMN "LateMinutes" DROP NOT NULL;
                ALTER TABLE organization."EmployeeClockings"
                ALTER COLUMN "LateMinutes" DROP DEFAULT;
                ALTER TABLE organization."EmployeeClockings"
                ALTER COLUMN "LateMinutes" TYPE interval
                USING make_interval(mins => "LateMinutes");
                """);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                schema: "organization",
                table: "EmployeeClockings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClockings_BranchId_ClockInTime",
                schema: "organization",
                table: "EmployeeClockings",
                columns: new[] { "BranchId", "ClockInTime" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeClockings_EmployeeId_ClockInTime",
                schema: "organization",
                table: "EmployeeClockings",
                columns: new[] { "EmployeeId", "ClockInTime" });
        }
    }
}
