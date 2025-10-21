using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFunctionalStandardAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Criteria_FunctionalStandardId_ThemeId_PracticeAreaId",
                table: "Criteria");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Criteria_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode",
                table: "Criteria",
                columns: new[] { "FunctionalStandardId", "ThemeId", "PracticeAreaId", "CriteriaCode" });

            migrationBuilder.CreateTable(
                name: "FunctionalStandardAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FipsId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FunctionalStandardId = table.Column<int>(type: "INTEGER", nullable: false),
                    AssessedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AssessmentDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FunctionalStandardAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FunctionalStandardAssessments_FunctionalStandards_FunctionalStandardId",
                        column: x => x.FunctionalStandardId,
                        principalTable: "FunctionalStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentCriteriaResponses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AssessmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    FunctionalStandardId = table.Column<int>(type: "INTEGER", nullable: false),
                    ThemeId = table.Column<int>(type: "INTEGER", nullable: false),
                    PracticeAreaId = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CriteriaCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Attainment = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentCriteriaResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssessmentCriteriaResponses_Criteria_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode",
                        columns: x => new { x.FunctionalStandardId, x.ThemeId, x.PracticeAreaId, x.CriteriaCode },
                        principalTable: "Criteria",
                        principalColumns: new[] { "FunctionalStandardId", "ThemeId", "PracticeAreaId", "CriteriaCode" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AssessmentCriteriaResponses_FunctionalStandardAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "FunctionalStandardAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriteriaResponses_AssessmentId_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode",
                table: "AssessmentCriteriaResponses",
                columns: new[] { "AssessmentId", "FunctionalStandardId", "ThemeId", "PracticeAreaId", "CriteriaCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentCriteriaResponses_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode",
                table: "AssessmentCriteriaResponses",
                columns: new[] { "FunctionalStandardId", "ThemeId", "PracticeAreaId", "CriteriaCode" });

            migrationBuilder.CreateIndex(
                name: "IX_FunctionalStandardAssessments_FunctionalStandardId",
                table: "FunctionalStandardAssessments",
                column: "FunctionalStandardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssessmentCriteriaResponses");

            migrationBuilder.DropTable(
                name: "FunctionalStandardAssessments");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Criteria_FunctionalStandardId_ThemeId_PracticeAreaId_CriteriaCode",
                table: "Criteria");

            migrationBuilder.CreateIndex(
                name: "IX_Criteria_FunctionalStandardId_ThemeId_PracticeAreaId",
                table: "Criteria",
                columns: new[] { "FunctionalStandardId", "ThemeId", "PracticeAreaId" });
        }
    }
}
