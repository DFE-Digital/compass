using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class RenameMissionPillarToPlural : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpportunityMissionPillar",
                table: "DemandRequests",
                newName: "OpportunityMissionPillars");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpportunityMissionPillars",
                table: "DemandRequests",
                newName: "OpportunityMissionPillar");
        }
    }
}
