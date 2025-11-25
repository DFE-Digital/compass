using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyStatusReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyStatusReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    ReportingYear = table.Column<int>(type: "int", nullable: false),
                    ReportingMonth = table.Column<int>(type: "int", nullable: false),
                    Narrative = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    MilestoneProgress = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    DeliverableProgress = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    KeyAchievements = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Challenges = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    NextMonthOutlook = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyStatusReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyStatusReports_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonthlyStatusReports_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyStatusReportTimescaleConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpcomingDays = table.Column<int>(type: "int", nullable: false),
                    DueGracePeriodDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyStatusReportTimescaleConfigs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatusReports_CreatedByUserId",
                table: "MonthlyStatusReports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatusReports_ProjectId_ReportingYear_ReportingMonth",
                table: "MonthlyStatusReports",
                columns: new[] { "ProjectId", "ReportingYear", "ReportingMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatusReports_ReportingYear_ReportingMonth",
                table: "MonthlyStatusReports",
                columns: new[] { "ReportingYear", "ReportingMonth" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatusReportTimescaleConfigs_IsActive",
                table: "MonthlyStatusReportTimescaleConfigs",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyStatusReportTimescaleConfigs_IsDefault",
                table: "MonthlyStatusReportTimescaleConfigs",
                column: "IsDefault",
                filter: "[IsDefault] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyStatusReports");

            migrationBuilder.DropTable(
                name: "MonthlyStatusReportTimescaleConfigs");
        }
    }
}
