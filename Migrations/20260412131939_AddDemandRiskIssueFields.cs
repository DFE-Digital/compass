using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandRiskIssueFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "DemandPipelineRiskIssues",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DirectorateId",
                table: "DemandPipelineRiskIssues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactOnDelivery",
                table: "DemandPipelineRiskIssues",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MitigationOrAction",
                table: "DemandPipelineRiskIssues",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "DemandPipelineRiskIssues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "DemandPipelineRiskIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetResolutionDate",
                table: "DemandPipelineRiskIssues",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Tier",
                table: "DemandPipelineRiskIssues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "DemandPipelineRiskIssues");

            migrationBuilder.DropColumn(
                name: "DirectorateId",
                table: "DemandPipelineRiskIssues");

            migrationBuilder.DropColumn(
                name: "ImpactOnDelivery",
                table: "DemandPipelineRiskIssues");

            migrationBuilder.DropColumn(
                name: "MitigationOrAction",
                table: "DemandPipelineRiskIssues");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "DemandPipelineRiskIssues");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "DemandPipelineRiskIssues");

            migrationBuilder.DropColumn(
                name: "TargetResolutionDate",
                table: "DemandPipelineRiskIssues");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "DemandPipelineRiskIssues");
        }
    }
}
