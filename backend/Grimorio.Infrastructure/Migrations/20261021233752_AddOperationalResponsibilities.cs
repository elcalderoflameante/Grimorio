using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Grimorio.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalResponsibilities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "operations");

            migrationBuilder.CreateTable(
                name: "ResponsibilityAreas",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                    table.PrimaryKey("PK_ResponsibilityAreas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityPositions",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
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
                    table.PrimaryKey("PK_ResponsibilityPositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityAssignments",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftStartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
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
                    table.PrimaryKey("PK_ResponsibilityAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsibilityAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityAssignments_ResponsibilityPositions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "operations",
                        principalTable: "ResponsibilityPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityTasks",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    AreaId = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Frequency = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Stage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PhotoMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AnchorDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DaysOfWeekMask = table.Column<int>(type: "integer", nullable: false),
                    EveryNDays = table.Column<int>(type: "integer", nullable: false),
                    IntervalMinutes = table.Column<int>(type: "integer", nullable: true),
                    WindowMinutes = table.Column<int>(type: "integer", nullable: false),
                    ChecklistJson = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_ResponsibilityTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsibilityTasks_ResponsibilityAreas_AreaId",
                        column: x => x.AreaId,
                        principalSchema: "operations",
                        principalTable: "ResponsibilityAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityTasks_ResponsibilityPositions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "operations",
                        principalTable: "ResponsibilityPositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityActivities",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShiftAssignmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    PositionName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    AreaName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TaskName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Stage = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PhotoMode = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChecklistJson = table.Column<string>(type: "jsonb", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    AvailableAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ShiftEndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CompletionKey = table.Column<Guid>(type: "uuid", nullable: true),
                    AnswersJson = table.Column<string>(type: "jsonb", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_ResponsibilityActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsibilityActivities_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "organization",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityActivities_ResponsibilityAssignments_Assignme~",
                        column: x => x.AssignmentId,
                        principalSchema: "operations",
                        principalTable: "ResponsibilityAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityActivities_ResponsibilityTasks_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "operations",
                        principalTable: "ResponsibilityTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponsibilityActivities_ShiftAssignments_ShiftAssignmentId",
                        column: x => x.ShiftAssignmentId,
                        principalSchema: "scheduling",
                        principalTable: "ShiftAssignments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityEvidence",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_ResponsibilityEvidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsibilityEvidence_ResponsibilityActivities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "operations",
                        principalTable: "ResponsibilityActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ResponsibilityTransfers",
                schema: "operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    ActivityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToEmployeeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
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
                    table.PrimaryKey("PK_ResponsibilityTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponsibilityTransfers_ResponsibilityActivities_ActivityId",
                        column: x => x.ActivityId,
                        principalSchema: "operations",
                        principalTable: "ResponsibilityActivities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_AssignmentId",
                schema: "operations",
                table: "ResponsibilityActivities",
                column: "AssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_BranchId",
                schema: "operations",
                table: "ResponsibilityActivities",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_BranchId_EmployeeId_WorkDate",
                schema: "operations",
                table: "ResponsibilityActivities",
                columns: new[] { "BranchId", "EmployeeId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_BranchId_IsDeleted",
                schema: "operations",
                table: "ResponsibilityActivities",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_BranchId_TaskId_ShiftAssignmentId_~",
                schema: "operations",
                table: "ResponsibilityActivities",
                columns: new[] { "BranchId", "TaskId", "ShiftAssignmentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_BranchId_TaskId_WorkDate_Available~",
                schema: "operations",
                table: "ResponsibilityActivities",
                columns: new[] { "BranchId", "TaskId", "WorkDate", "AvailableAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_BranchId_WorkDate_Status",
                schema: "operations",
                table: "ResponsibilityActivities",
                columns: new[] { "BranchId", "WorkDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_EmployeeId",
                schema: "operations",
                table: "ResponsibilityActivities",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_IsDeleted",
                schema: "operations",
                table: "ResponsibilityActivities",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_ShiftAssignmentId",
                schema: "operations",
                table: "ResponsibilityActivities",
                column: "ShiftAssignmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityActivities_TaskId",
                schema: "operations",
                table: "ResponsibilityActivities",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAreas_BranchId",
                schema: "operations",
                table: "ResponsibilityAreas",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAreas_BranchId_IsDeleted",
                schema: "operations",
                table: "ResponsibilityAreas",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAreas_BranchId_Name",
                schema: "operations",
                table: "ResponsibilityAreas",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAreas_IsDeleted",
                schema: "operations",
                table: "ResponsibilityAreas",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAssignments_BranchId",
                schema: "operations",
                table: "ResponsibilityAssignments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAssignments_BranchId_IsDeleted",
                schema: "operations",
                table: "ResponsibilityAssignments",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAssignments_BranchId_PositionId_FromDate_ToDa~",
                schema: "operations",
                table: "ResponsibilityAssignments",
                columns: new[] { "BranchId", "PositionId", "FromDate", "ToDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAssignments_EmployeeId",
                schema: "operations",
                table: "ResponsibilityAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAssignments_IsDeleted",
                schema: "operations",
                table: "ResponsibilityAssignments",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityAssignments_PositionId",
                schema: "operations",
                table: "ResponsibilityAssignments",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityEvidence_ActivityId",
                schema: "operations",
                table: "ResponsibilityEvidence",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityEvidence_BranchId",
                schema: "operations",
                table: "ResponsibilityEvidence",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityEvidence_BranchId_IsDeleted",
                schema: "operations",
                table: "ResponsibilityEvidence",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityEvidence_IsDeleted",
                schema: "operations",
                table: "ResponsibilityEvidence",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityPositions_BranchId",
                schema: "operations",
                table: "ResponsibilityPositions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityPositions_BranchId_IsDeleted",
                schema: "operations",
                table: "ResponsibilityPositions",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityPositions_BranchId_Name",
                schema: "operations",
                table: "ResponsibilityPositions",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityPositions_IsDeleted",
                schema: "operations",
                table: "ResponsibilityPositions",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTasks_AreaId",
                schema: "operations",
                table: "ResponsibilityTasks",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTasks_BranchId",
                schema: "operations",
                table: "ResponsibilityTasks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTasks_BranchId_IsDeleted",
                schema: "operations",
                table: "ResponsibilityTasks",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTasks_IsDeleted",
                schema: "operations",
                table: "ResponsibilityTasks",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTasks_PositionId",
                schema: "operations",
                table: "ResponsibilityTasks",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTransfers_ActivityId",
                schema: "operations",
                table: "ResponsibilityTransfers",
                column: "ActivityId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTransfers_BranchId",
                schema: "operations",
                table: "ResponsibilityTransfers",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTransfers_BranchId_IsDeleted",
                schema: "operations",
                table: "ResponsibilityTransfers",
                columns: new[] { "BranchId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_ResponsibilityTransfers_IsDeleted",
                schema: "operations",
                table: "ResponsibilityTransfers",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResponsibilityEvidence",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "ResponsibilityTransfers",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "ResponsibilityActivities",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "ResponsibilityAssignments",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "ResponsibilityTasks",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "ResponsibilityAreas",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "ResponsibilityPositions",
                schema: "operations");
        }
    }
}
