using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class IssueDetailedCauseAndAssurance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssuranceArrangements",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetailedCause",
                table: "Issues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IssueAssuranceEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    EventKind = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecisionSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAssuranceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IssueAssuranceEvents_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssuranceEvents_IssueId",
                table: "IssueAssuranceEvents",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssuranceEvents_IssueId_SortOrder",
                table: "IssueAssuranceEvents",
                columns: new[] { "IssueId", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueAssuranceEvents");

            migrationBuilder.DropColumn(
                name: "AssuranceArrangements",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "DetailedCause",
                table: "Issues");
        }
    }
}
