using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class RaidDivisionBusinessAreaJunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssumptionBusinessAreas",
                columns: table => new
                {
                    AssumptionId = table.Column<int>(type: "int", nullable: false),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssumptionBusinessAreas", x => new { x.AssumptionId, x.BusinessAreaLookupId });
                    table.ForeignKey(
                        name: "FK_AssumptionBusinessAreas_Assumptions_AssumptionId",
                        column: x => x.AssumptionId,
                        principalTable: "Assumptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssumptionBusinessAreas_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssumptionDivisions",
                columns: table => new
                {
                    AssumptionId = table.Column<int>(type: "int", nullable: false),
                    DivisionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssumptionDivisions", x => new { x.AssumptionId, x.DivisionId });
                    table.ForeignKey(
                        name: "FK_AssumptionDivisions_Assumptions_AssumptionId",
                        column: x => x.AssumptionId,
                        principalTable: "Assumptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssumptionDivisions_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueBusinessAreas",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueBusinessAreas", x => new { x.IssueId, x.BusinessAreaLookupId });
                    table.ForeignKey(
                        name: "FK_IssueBusinessAreas_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueBusinessAreas_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueDivisions",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    DivisionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueDivisions", x => new { x.IssueId, x.DivisionId });
                    table.ForeignKey(
                        name: "FK_IssueDivisions_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueDivisions_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskBusinessAreas",
                columns: table => new
                {
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskBusinessAreas", x => new { x.RiskId, x.BusinessAreaLookupId });
                    table.ForeignKey(
                        name: "FK_RiskBusinessAreas_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskBusinessAreas_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskDivisions",
                columns: table => new
                {
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    DivisionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskDivisions", x => new { x.RiskId, x.DivisionId });
                    table.ForeignKey(
                        name: "FK_RiskDivisions_Divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "Divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskDivisions_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssumptionBusinessAreas_BusinessAreaLookupId",
                table: "AssumptionBusinessAreas",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_AssumptionDivisions_DivisionId",
                table: "AssumptionDivisions",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueBusinessAreas_BusinessAreaLookupId",
                table: "IssueBusinessAreas",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueDivisions_DivisionId",
                table: "IssueDivisions",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskBusinessAreas_BusinessAreaLookupId",
                table: "RiskBusinessAreas",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskDivisions_DivisionId",
                table: "RiskDivisions",
                column: "DivisionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssumptionBusinessAreas");

            migrationBuilder.DropTable(
                name: "AssumptionDivisions");

            migrationBuilder.DropTable(
                name: "IssueBusinessAreas");

            migrationBuilder.DropTable(
                name: "IssueDivisions");

            migrationBuilder.DropTable(
                name: "RiskBusinessAreas");

            migrationBuilder.DropTable(
                name: "RiskDivisions");
        }
    }
}
