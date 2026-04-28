using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandScoringFramework : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ScoreFunding",
                table: "DemandPipelineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreRice",
                table: "DemandPipelineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreStrategic",
                table: "DemandPipelineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ScoreUrgency",
                table: "DemandPipelineRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoringAssessmentNotes",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoringConcernsNotes",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoreFunding",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ScoreRice",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ScoreStrategic",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ScoreUrgency",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ScoringAssessmentNotes",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ScoringConcernsNotes",
                table: "DemandPipelineRequests");
        }
    }
}
