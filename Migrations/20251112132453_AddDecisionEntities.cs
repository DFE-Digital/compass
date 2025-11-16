using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDecisionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceRecordUrl",
                table: "Risks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "Risks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Risks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRecordUrl",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceRiskId",
                table: "Issues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Issues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DecisionId",
                table: "Actions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRecordUrl",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "Actions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Decisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ObjectiveId = table.Column<int>(type: "int", nullable: true),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    OwnerUserId = table.Column<int>(type: "int", nullable: true),
                    FipsId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DecisionType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DecisionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Decisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Decisions_Objectives_ObjectiveId",
                        column: x => x.ObjectiveId,
                        principalTable: "Objectives",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Decisions_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Decisions_Users_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueDecisions",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueDecisions", x => new { x.IssueId, x.DecisionId });
                    table.ForeignKey(
                        name: "FK_IssueDecisions_Decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "Decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueDecisions_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskDecisions",
                columns: table => new
                {
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    DecisionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskDecisions", x => new { x.RiskId, x.DecisionId });
                    table.ForeignKey(
                        name: "FK_RiskDecisions_Decisions_DecisionId",
                        column: x => x.DecisionId,
                        principalTable: "Decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RiskDecisions_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Issues_SourceRiskId",
                table: "Issues",
                column: "SourceRiskId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_DecisionId",
                table: "Actions",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_DecisionDate",
                table: "Decisions",
                column: "DecisionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_FipsId",
                table: "Decisions",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ObjectiveId",
                table: "Decisions",
                column: "ObjectiveId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_OwnerUserId",
                table: "Decisions",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_ProjectId",
                table: "Decisions",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Decisions_Status",
                table: "Decisions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_IssueDecisions_DecisionId",
                table: "IssueDecisions",
                column: "DecisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskDecisions_DecisionId",
                table: "RiskDecisions",
                column: "DecisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Decisions_DecisionId",
                table: "Actions",
                column: "DecisionId",
                principalTable: "Decisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Issues_Risks_SourceRiskId",
                table: "Issues",
                column: "SourceRiskId",
                principalTable: "Risks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Decisions_DecisionId",
                table: "Actions");

            migrationBuilder.DropForeignKey(
                name: "FK_Issues_Risks_SourceRiskId",
                table: "Issues");

            migrationBuilder.DropTable(
                name: "IssueDecisions");

            migrationBuilder.DropTable(
                name: "RiskDecisions");

            migrationBuilder.DropTable(
                name: "Decisions");

            migrationBuilder.DropIndex(
                name: "IX_Issues_SourceRiskId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Actions_DecisionId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "SourceRecordUrl",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "SourceRecordUrl",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SourceRiskId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "DecisionId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "SourceRecordUrl",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "Actions");
        }
    }
}
