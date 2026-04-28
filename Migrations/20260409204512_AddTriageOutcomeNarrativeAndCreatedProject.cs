using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageOutcomeNarrativeAndCreatedProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "DraftRagJustification",
                table: "ProjectMonthlyUpdates",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DraftPathToGreen",
                table: "ProjectMonthlyUpdates",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TriageCreatedProjectId",
                table: "DemandPipelineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriageOutcomeNarrative",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandPipelineRequests_TriageCreatedProjectId",
                table: "DemandPipelineRequests",
                column: "TriageCreatedProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandPipelineRequests_Projects_TriageCreatedProjectId",
                table: "DemandPipelineRequests",
                column: "TriageCreatedProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandPipelineRequests_Projects_TriageCreatedProjectId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropIndex(
                name: "IX_DemandPipelineRequests_TriageCreatedProjectId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "TriageCreatedProjectId",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "TriageOutcomeNarrative",
                table: "DemandPipelineRequests");

            migrationBuilder.AlterColumn<string>(
                name: "DraftRagJustification",
                table: "ProjectMonthlyUpdates",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DraftPathToGreen",
                table: "ProjectMonthlyUpdates",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);
        }
    }
}
