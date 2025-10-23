using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDependencySystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectDependencies");

            migrationBuilder.CreateTable(
                name: "Dependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceEntityType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SourceEntityId = table.Column<int>(type: "int", nullable: false),
                    TargetEntityType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TargetEntityId = table.Column<int>(type: "int", nullable: false),
                    DependencyType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResolvedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ResolvedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: true),
                    ActionId1 = table.Column<int>(type: "int", nullable: true),
                    IssueId = table.Column<int>(type: "int", nullable: true),
                    IssueId1 = table.Column<int>(type: "int", nullable: true),
                    MilestoneId = table.Column<int>(type: "int", nullable: true),
                    MilestoneId1 = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    ProjectId1 = table.Column<int>(type: "int", nullable: true),
                    RiskId = table.Column<int>(type: "int", nullable: true),
                    RiskId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dependencies_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Actions_ActionId1",
                        column: x => x.ActionId1,
                        principalTable: "Actions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Issues_IssueId1",
                        column: x => x.IssueId1,
                        principalTable: "Issues",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Milestones_MilestoneId1",
                        column: x => x.MilestoneId1,
                        principalTable: "Milestones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Projects_ProjectId1",
                        column: x => x.ProjectId1,
                        principalTable: "Projects",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Dependencies_Risks_RiskId1",
                        column: x => x.RiskId1,
                        principalTable: "Risks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_ActionId",
                table: "Dependencies",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_ActionId1",
                table: "Dependencies",
                column: "ActionId1");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_DependencyType",
                table: "Dependencies",
                column: "DependencyType");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_IssueId",
                table: "Dependencies",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_IssueId1",
                table: "Dependencies",
                column: "IssueId1");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_MilestoneId",
                table: "Dependencies",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_MilestoneId1",
                table: "Dependencies",
                column: "MilestoneId1");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_ProjectId",
                table: "Dependencies",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_ProjectId1",
                table: "Dependencies",
                column: "ProjectId1");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_RiskId",
                table: "Dependencies",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_RiskId1",
                table: "Dependencies",
                column: "RiskId1");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_SourceEntityType_SourceEntityId",
                table: "Dependencies",
                columns: new[] { "SourceEntityType", "SourceEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_Status",
                table: "Dependencies",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Dependencies_TargetEntityType_TargetEntityId",
                table: "Dependencies",
                columns: new[] { "TargetEntityType", "TargetEntityId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Dependencies");

            migrationBuilder.CreateTable(
                name: "ProjectDependencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DependsOnProjectId = table.Column<int>(type: "int", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DependencyType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectDependencies_Projects_DependsOnProjectId",
                        column: x => x.DependsOnProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectDependencies_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependencies_DependsOnProjectId",
                table: "ProjectDependencies",
                column: "DependsOnProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectDependencies_ProjectId",
                table: "ProjectDependencies",
                column: "ProjectId");
        }
    }
}
