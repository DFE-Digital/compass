using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddExploreDetailFieldsToDemandPipelineRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExploreNotes",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExploreAimClarification",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExploreLinksToExistingWork",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExplorePolicies",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExploreResearchAndInsights",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExploreUserGroups",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExploreAimClarification",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ExploreLinksToExistingWork",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ExplorePolicies",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ExploreResearchAndInsights",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "ExploreUserGroups",
                table: "DemandPipelineRequests");

            migrationBuilder.AlterColumn<string>(
                name: "ExploreNotes",
                table: "DemandPipelineRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: -1,
                oldNullable: true);
        }
    }
}
