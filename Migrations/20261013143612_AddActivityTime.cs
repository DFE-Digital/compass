using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_Milestones_MilestoneId",
                table: "WorkUpdateMilestoneEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_ProjectMonthlyUpdates_ProjectMonthlyUpdateId",
                table: "WorkUpdateMilestoneEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_ProjectWeeklyWorkUpdates_ProjectWeeklyWorkUpdateId",
                table: "WorkUpdateMilestoneEntries");

            migrationBuilder.CreateTable(
                name: "ActivityTimeThemeLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTimeThemeLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTimeTypeLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTimeTypeLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTimeEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityDate = table.Column<DateTime>(type: "date", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    ActivityTimeTypeLookupId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTimeEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityTimeEntries_ActivityTimeTypeLookups_ActivityTimeTypeLookupId",
                        column: x => x.ActivityTimeTypeLookupId,
                        principalTable: "ActivityTimeTypeLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityTimeEntries_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ActivityTimeEntries_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ActivityTimeEntryThemes",
                columns: table => new
                {
                    ActivityTimeEntryId = table.Column<int>(type: "int", nullable: false),
                    ActivityTimeThemeLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityTimeEntryThemes", x => new { x.ActivityTimeEntryId, x.ActivityTimeThemeLookupId });
                    table.ForeignKey(
                        name: "FK_ActivityTimeEntryThemes_ActivityTimeEntries_ActivityTimeEntryId",
                        column: x => x.ActivityTimeEntryId,
                        principalTable: "ActivityTimeEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityTimeEntryThemes_ActivityTimeThemeLookups_ActivityTimeThemeLookupId",
                        column: x => x.ActivityTimeThemeLookupId,
                        principalTable: "ActivityTimeThemeLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeEntries_ActivityDate",
                table: "ActivityTimeEntries",
                column: "ActivityDate");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeEntries_ActivityTimeTypeLookupId",
                table: "ActivityTimeEntries",
                column: "ActivityTimeTypeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeEntries_BusinessAreaLookupId",
                table: "ActivityTimeEntries",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeEntries_CreatedByEmail",
                table: "ActivityTimeEntries",
                column: "CreatedByEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeEntries_ProjectId",
                table: "ActivityTimeEntries",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeEntryThemes_ActivityTimeThemeLookupId",
                table: "ActivityTimeEntryThemes",
                column: "ActivityTimeThemeLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeThemeLookups_IsActive",
                table: "ActivityTimeThemeLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeThemeLookups_Name",
                table: "ActivityTimeThemeLookups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeTypeLookups_IsActive",
                table: "ActivityTimeTypeLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityTimeTypeLookups_Name",
                table: "ActivityTimeTypeLookups",
                column: "Name",
                unique: true);

            var seedNow = new DateTime(2026, 10, 13, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "ActivityTimeTypeLookups",
                columns: new[] { "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "Advice", null, 10, true, seedNow, seedNow },
                    { "Support", null, 20, true, seedNow, seedNow },
                    { "Questions", null, 30, true, seedNow, seedNow },
                    { "Commissions", null, 40, true, seedNow, seedNow },
                    { "Processing", null, 50, true, seedNow, seedNow }
                });

            migrationBuilder.InsertData(
                table: "ActivityTimeThemeLookups",
                columns: new[] { "Name", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "Business cases", null, 10, true, seedNow, seedNow },
                    { "Internal commission request", null, 20, true, seedNow, seedNow },
                    { "External commission request", null, 30, true, seedNow, seedNow },
                    { "Demand", null, 40, true, seedNow, seedNow },
                    { "Work", null, 50, true, seedNow, seedNow },
                    { "Risks and Issues", null, 60, true, seedNow, seedNow },
                    { "Products and services", null, 70, true, seedNow, seedNow },
                    { "Reporting", null, 80, true, seedNow, seedNow }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_Milestones_MilestoneId",
                table: "WorkUpdateMilestoneEntries",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_ProjectMonthlyUpdates_ProjectMonthlyUpdateId",
                table: "WorkUpdateMilestoneEntries",
                column: "ProjectMonthlyUpdateId",
                principalTable: "ProjectMonthlyUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_ProjectWeeklyWorkUpdates_ProjectWeeklyWorkUpdateId",
                table: "WorkUpdateMilestoneEntries",
                column: "ProjectWeeklyWorkUpdateId",
                principalTable: "ProjectWeeklyWorkUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_Milestones_MilestoneId",
                table: "WorkUpdateMilestoneEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_ProjectMonthlyUpdates_ProjectMonthlyUpdateId",
                table: "WorkUpdateMilestoneEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_ProjectWeeklyWorkUpdates_ProjectWeeklyWorkUpdateId",
                table: "WorkUpdateMilestoneEntries");

            migrationBuilder.DropTable(
                name: "ActivityTimeEntryThemes");

            migrationBuilder.DropTable(
                name: "ActivityTimeEntries");

            migrationBuilder.DropTable(
                name: "ActivityTimeThemeLookups");

            migrationBuilder.DropTable(
                name: "ActivityTimeTypeLookups");

            migrationBuilder.AddForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_Milestones_MilestoneId",
                table: "WorkUpdateMilestoneEntries",
                column: "MilestoneId",
                principalTable: "Milestones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_ProjectMonthlyUpdates_ProjectMonthlyUpdateId",
                table: "WorkUpdateMilestoneEntries",
                column: "ProjectMonthlyUpdateId",
                principalTable: "ProjectMonthlyUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkUpdateMilestoneEntries_ProjectWeeklyWorkUpdates_ProjectWeeklyWorkUpdateId",
                table: "WorkUpdateMilestoneEntries",
                column: "ProjectWeeklyWorkUpdateId",
                principalTable: "ProjectWeeklyWorkUpdates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
