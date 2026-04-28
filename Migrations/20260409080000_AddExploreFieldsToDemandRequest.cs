using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddExploreFieldsToDemandRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExploreNotes",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExploreFeasibility",
                table: "DemandPipelineRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExploreRecommendation",
                table: "DemandPipelineRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExploreCompletedAt",
                table: "DemandPipelineRequests",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExploreCompletedBy",
                table: "DemandPipelineRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ExploreNotes", table: "DemandPipelineRequests");
            migrationBuilder.DropColumn(name: "ExploreFeasibility", table: "DemandPipelineRequests");
            migrationBuilder.DropColumn(name: "ExploreRecommendation", table: "DemandPipelineRequests");
            migrationBuilder.DropColumn(name: "ExploreCompletedAt", table: "DemandPipelineRequests");
            migrationBuilder.DropColumn(name: "ExploreCompletedBy", table: "DemandPipelineRequests");
        }
    }
}
