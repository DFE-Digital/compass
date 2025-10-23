using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProjectModelsForMultipleMissionsAndFunding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_FundingSources_FundingSourceId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Missions_MissionId",
                table: "Projects");

            migrationBuilder.CreateTable(
                name: "ProjectFundingAllocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    FundingSourceId = table.Column<int>(type: "int", nullable: false),
                    AllocationPercentage = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectFundingAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectFundingAllocations_FundingSources_FundingSourceId",
                        column: x => x.FundingSourceId,
                        principalTable: "FundingSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectFundingAllocations_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProjectMissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    MissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectMissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectMissions_Missions_MissionId",
                        column: x => x.MissionId,
                        principalTable: "Missions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectMissions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFundingAllocations_FundingSourceId",
                table: "ProjectFundingAllocations",
                column: "FundingSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectFundingAllocations_ProjectId",
                table: "ProjectFundingAllocations",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMissions_MissionId",
                table: "ProjectMissions",
                column: "MissionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMissions_ProjectId",
                table: "ProjectMissions",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_FundingSources_FundingSourceId",
                table: "Projects",
                column: "FundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Missions_MissionId",
                table: "Projects",
                column: "MissionId",
                principalTable: "Missions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_FundingSources_FundingSourceId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Missions_MissionId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "ProjectFundingAllocations");

            migrationBuilder.DropTable(
                name: "ProjectMissions");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_FundingSources_FundingSourceId",
                table: "Projects",
                column: "FundingSourceId",
                principalTable: "FundingSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Missions_MissionId",
                table: "Projects",
                column: "MissionId",
                principalTable: "Missions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
