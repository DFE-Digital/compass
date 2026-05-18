using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class RaidEscalationTierChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaidEscalationTierChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RiskId = table.Column<int>(type: "int", nullable: true),
                    IssueId = table.Column<int>(type: "int", nullable: true),
                    FromRiskTierId = table.Column<int>(type: "int", nullable: true),
                    ToRiskTierId = table.Column<int>(type: "int", nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SubmittedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DecidedByUserId = table.Column<int>(type: "int", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidEscalationTierChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidEscalationTierChangeRequests_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidEscalationTierChangeRequests_RiskTiers_FromRiskTierId",
                        column: x => x.FromRiskTierId,
                        principalTable: "RiskTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidEscalationTierChangeRequests_RiskTiers_ToRiskTierId",
                        column: x => x.ToRiskTierId,
                        principalTable: "RiskTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidEscalationTierChangeRequests_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidEscalationTierChangeRequests_Users_DecidedByUserId",
                        column: x => x.DecidedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RaidEscalationTierChangeRequests_Users_SubmittedByUserId",
                        column: x => x.SubmittedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidEscalationTierChangeRequests_DecidedByUserId",
                table: "RaidEscalationTierChangeRequests",
                column: "DecidedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidEscalationTierChangeRequests_FromRiskTierId",
                table: "RaidEscalationTierChangeRequests",
                column: "FromRiskTierId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidEscalationTierChangeRequests_IssueId",
                table: "RaidEscalationTierChangeRequests",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidEscalationTierChangeRequests_RiskId",
                table: "RaidEscalationTierChangeRequests",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidEscalationTierChangeRequests_SubmittedByUserId",
                table: "RaidEscalationTierChangeRequests",
                column: "SubmittedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidEscalationTierChangeRequests_ToRiskTierId",
                table: "RaidEscalationTierChangeRequests",
                column: "ToRiskTierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidEscalationTierChangeRequests");
        }
    }
}
