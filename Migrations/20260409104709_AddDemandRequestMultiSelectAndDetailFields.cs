using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandRequestMultiSelectAndDetailFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DigitalServiceChangeDetails",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FundingProvidedDetails",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeadcountProvidedDetails",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissionPillarIds",
                table: "DemandPipelineRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriorityOutcomeIds",
                table: "DemandPipelineRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "DigitalServiceChangeDetails", table: "DemandPipelineRequests");
            migrationBuilder.DropColumn(name: "FundingProvidedDetails", table: "DemandPipelineRequests");
            migrationBuilder.DropColumn(name: "HeadcountProvidedDetails", table: "DemandPipelineRequests");
            migrationBuilder.DropColumn(name: "MissionPillarIds", table: "DemandPipelineRequests");
            migrationBuilder.DropColumn(name: "PriorityOutcomeIds", table: "DemandPipelineRequests");
        }
    }
}
