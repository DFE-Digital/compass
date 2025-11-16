using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class DemandManagementNotesAssessments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemandRequestAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    AssessmentType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssessmentContent = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    AssessedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    AssessedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequestAssessments_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandRequestNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequestNotes_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DemandRequestSectionCompletions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DemandRequestId = table.Column<int>(type: "int", nullable: false),
                    SectionName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CompletionStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CompletedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemandRequestSectionCompletions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DemandRequestSectionCompletions_DemandRequests_DemandRequestId",
                        column: x => x.DemandRequestId,
                        principalTable: "DemandRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestAssessments_DemandRequestId_AssessmentType",
                table: "DemandRequestAssessments",
                columns: new[] { "DemandRequestId", "AssessmentType" });

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestNotes_DemandRequestId",
                table: "DemandRequestNotes",
                column: "DemandRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequestSectionCompletions_DemandRequestId_SectionName",
                table: "DemandRequestSectionCompletions",
                columns: new[] { "DemandRequestId", "SectionName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemandRequestAssessments");

            migrationBuilder.DropTable(
                name: "DemandRequestNotes");

            migrationBuilder.DropTable(
                name: "DemandRequestSectionCompletions");
        }
    }
}
