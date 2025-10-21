using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRAIDManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Objectives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    RagStatus = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    SuccessMeasures = table.Column<string>(type: "TEXT", nullable: true),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Objectives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Objectives_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObjectiveId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    AssignedToUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ParentActionId = table.Column<int>(type: "INTEGER", nullable: true),
                    EvidenceUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actions_Actions_ParentActionId",
                        column: x => x.ParentActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actions_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actions_Users_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObjectiveId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    Severity = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    DetectedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TargetResolutionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ResolutionSummary = table.Column<string>(type: "TEXT", nullable: true),
                    Workaround = table.Column<string>(type: "TEXT", nullable: true),
                    BlockedFlag = table.Column<bool>(type: "INTEGER", nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Issues_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObjectiveId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    BaselineDueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ProgressPercent = table.Column<int>(type: "INTEGER", nullable: true),
                    ExternalRef = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Milestones_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Risks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObjectiveId = table.Column<int>(type: "INTEGER", nullable: true),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    OwnerUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    ImpactRating = table.Column<int>(type: "INTEGER", nullable: false),
                    LikelihoodRating = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskScore = table.Column<int>(type: "INTEGER", nullable: false),
                    ProximityDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Response = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ResidualImpact = table.Column<int>(type: "INTEGER", nullable: true),
                    ResidualLikelihood = table.Column<int>(type: "INTEGER", nullable: true),
                    TargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ClosedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Risks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Risks_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Risks_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "IssueActions",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueActions", x => new { x.IssueId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_IssueActions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueActions_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneActions",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneActions", x => new { x.MilestoneId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_MilestoneActions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MilestoneActions_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneIssues",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    IssueId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneIssues", x => new { x.MilestoneId, x.IssueId });
                    table.ForeignKey(
                        name: "FK_MilestoneIssues_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MilestoneIssues_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MilestoneRisks",
                columns: table => new
                {
                    MilestoneId = table.Column<int>(type: "INTEGER", nullable: false),
                    RiskId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneRisks", x => new { x.MilestoneId, x.RiskId });
                    table.ForeignKey(
                        name: "FK_MilestoneRisks_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MilestoneRisks_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskActions",
                columns: table => new
                {
                    RiskId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskActions", x => new { x.RiskId, x.ActionId });
                    table.ForeignKey(
                        name: "FK_RiskActions_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RiskActions_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AssignedToUserId",
                table: "Actions",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_DueDate",
                table: "Actions",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ObjectiveId",
                table: "Actions",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ParentActionId",
                table: "Actions",
                column: "ParentActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_Status_Priority",
                table: "Actions",
                columns: new[] { "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_IssueActions_ActionId",
                table: "IssueActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ObjectiveId",
                table: "Issues",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_OwnerUserId",
                table: "Issues",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Severity_Priority",
                table: "Issues",
                columns: new[] { "Severity", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_Status",
                table: "Issues",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_TargetResolutionDate",
                table: "Issues",
                column: "TargetResolutionDate");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneActions_ActionId",
                table: "MilestoneActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneIssues_IssueId",
                table: "MilestoneIssues",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneRisks_RiskId",
                table: "MilestoneRisks",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_DueDate",
                table: "Milestones",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_ObjectiveId",
                table: "Milestones",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_OwnerUserId",
                table: "Milestones",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_Status",
                table: "Milestones",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_OwnerUserId",
                table: "Objectives",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_RagStatus",
                table: "Objectives",
                column: "RagStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_Status",
                table: "Objectives",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RiskActions_ActionId",
                table: "RiskActions",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ObjectiveId",
                table: "Risks",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_OwnerUserId",
                table: "Risks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ProximityDate",
                table: "Risks",
                column: "ProximityDate");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskScore",
                table: "Risks",
                column: "RiskScore",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_Status",
                table: "Risks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueActions");

            migrationBuilder.DropTable(
                name: "MilestoneActions");

            migrationBuilder.DropTable(
                name: "MilestoneIssues");

            migrationBuilder.DropTable(
                name: "MilestoneRisks");

            migrationBuilder.DropTable(
                name: "RiskActions");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "Risks");

            migrationBuilder.DropTable(
                name: "Objectives");
        }
    }
}
