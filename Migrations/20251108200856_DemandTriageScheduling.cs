using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class DemandTriageScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConvertedProjectId",
                table: "DemandRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConvertedToProjectAt",
                table: "DemandRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSubmittedToTriage",
                table: "DemandRequests",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriageMeetingId",
                table: "DemandRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriageNotes",
                table: "DemandRequests",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TriageSubmittedAt",
                table: "DemandRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TriageMeetings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    StartAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TriageMeetings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_ConvertedProjectId",
                table: "DemandRequests",
                column: "ConvertedProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_IsSubmittedToTriage",
                table: "DemandRequests",
                column: "IsSubmittedToTriage");

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_TriageMeetingId",
                table: "DemandRequests",
                column: "TriageMeetingId");

            migrationBuilder.CreateIndex(
                name: "IX_TriageMeetings_EndAt",
                table: "TriageMeetings",
                column: "EndAt");

            migrationBuilder.CreateIndex(
                name: "IX_TriageMeetings_IsActive",
                table: "TriageMeetings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TriageMeetings_StartAt",
                table: "TriageMeetings",
                column: "StartAt");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandRequests_Projects_ConvertedProjectId",
                table: "DemandRequests",
                column: "ConvertedProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DemandRequests_TriageMeetings_TriageMeetingId",
                table: "DemandRequests",
                column: "TriageMeetingId",
                principalTable: "TriageMeetings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandRequests_Projects_ConvertedProjectId",
                table: "DemandRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_DemandRequests_TriageMeetings_TriageMeetingId",
                table: "DemandRequests");

            migrationBuilder.DropTable(
                name: "TriageMeetings");

            migrationBuilder.DropIndex(
                name: "IX_DemandRequests_ConvertedProjectId",
                table: "DemandRequests");

            migrationBuilder.DropIndex(
                name: "IX_DemandRequests_IsSubmittedToTriage",
                table: "DemandRequests");

            migrationBuilder.DropIndex(
                name: "IX_DemandRequests_TriageMeetingId",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "ConvertedProjectId",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "ConvertedToProjectAt",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "IsSubmittedToTriage",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "TriageMeetingId",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "TriageNotes",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "TriageSubmittedAt",
                table: "DemandRequests");
        }
    }
}
