using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddKpiEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Kpis",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnitOfMeasure = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CalculationMethod = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Frequency = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Thresholds = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportingStage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AssignedToEntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ProductFipsId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ObjectiveId = table.Column<int>(type: "int", nullable: true),
                    MilestoneId = table.Column<int>(type: "int", nullable: true),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    ValidationRule = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kpis", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Kpis_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Kpis_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Kpis_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Kpis_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KpiDataPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KpiId = table.Column<int>(type: "int", nullable: false),
                    ReportingPeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReportingPeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Value = table.Column<decimal>(type: "decimal(20,4)", precision: 20, scale: 4, nullable: true),
                    ValueNarrative = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsValidated = table.Column<bool>(type: "bit", nullable: false),
                    SubmissionStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiDataPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiDataPoints_Kpis_KpiId",
                        column: x => x.KpiId,
                        principalTable: "Kpis",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KpiDataPoints_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KpiDataPoints_KpiId",
                table: "KpiDataPoints",
                column: "KpiId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDataPoints_ReportingPeriodStart",
                table: "KpiDataPoints",
                column: "ReportingPeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_KpiDataPoints_SubmittedByUserId",
                table: "KpiDataPoints",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_Active",
                table: "Kpis",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_AssignedToEntityId_EntityType",
                table: "Kpis",
                columns: new[] { "AssignedToEntityId", "EntityType" });

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_Code",
                table: "Kpis",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_MilestoneId",
                table: "Kpis",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_ObjectiveId",
                table: "Kpis",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_OwnerUserId",
                table: "Kpis",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Kpis_ProjectId",
                table: "Kpis",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KpiDataPoints");

            migrationBuilder.DropTable(
                name: "Kpis");
        }
    }
}
