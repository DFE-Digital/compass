using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnologyCodeOfPracticePhaseGuidance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TechnologyCodeOfPracticePhaseGuidance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnologyCodeOfPracticeId = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Guidance = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    KeyActivities = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    QuestionsToConsider = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyCodeOfPracticePhaseGuidance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyCodeOfPracticePhaseGuidance_TechnologyCodeOfPractice_TechnologyCodeOfPracticeId",
                        column: x => x.TechnologyCodeOfPracticeId,
                        principalTable: "TechnologyCodeOfPractice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologyCodeOfPracticePhaseGuidance_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechnologyCodeOfPracticePhaseGuidance_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyCodeOfPracticePhaseGuidance_CreatedByUserId",
                table: "TechnologyCodeOfPracticePhaseGuidance",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyCodeOfPracticePhaseGuidance_TechnologyCodeOfPracticeId",
                table: "TechnologyCodeOfPracticePhaseGuidance",
                column: "TechnologyCodeOfPracticeId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyCodeOfPracticePhaseGuidance_UpdatedByUserId",
                table: "TechnologyCodeOfPracticePhaseGuidance",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnologyCodeOfPracticePhaseGuidance");
        }
    }
}
