using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiSelectOutcomesPillarsToBusinessCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PriorityOutcomeIds",
                table: "DemandPipelineBusinessCases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissionPillarIds",
                table: "DemandPipelineBusinessCases",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PriorityOutcomeIds",
                table: "DemandPipelineBusinessCases");

            migrationBuilder.DropColumn(
                name: "MissionPillarIds",
                table: "DemandPipelineBusinessCases");
        }
    }
}
