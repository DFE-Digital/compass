using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RiskTierId",
                table: "Risks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RiskTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskTiers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskTierId",
                table: "Risks",
                column: "RiskTierId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskTiers_Code",
                table: "RiskTiers",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RiskTiers_IsActive",
                table: "RiskTiers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RiskTiers_SortOrder",
                table: "RiskTiers",
                column: "SortOrder");

            // Seed Risk Tiers
            var now = DateTime.UtcNow;
            
            migrationBuilder.InsertData(
                table: "RiskTiers",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "PROJECT", "Project-level risk", "Tactical risks that directly affect the delivery of a single project, product, or service. Managed day-to-day by delivery teams. Examples: Supplier delays, resource gaps, accessibility issues, technology bugs, dependency on another service.", "Within a discrete project or service", 1, true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTiers",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "PROGRAMME", "Programme-level risk", "Strategic and operational risks that threaten delivery across multiple projects in a programme. Managed by programme boards and senior responsible owners (SROs). Examples: Conflicting priorities across projects, funding shortfalls, interdependencies, benefits not realised.", "Across a set of related projects", 2, true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTiers",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "PORTFOLIO", "Portfolio-level risk", "Risks affecting the achievement of strategic objectives within a portfolio (e.g. Digital Portfolio, Transformation Portfolio). Examples: Competing resource demand, cross-programme technology dependencies, change fatigue.", "Across multiple programmes or a business area", 3, true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTiers",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "DEPARTMENT", "Department-level risk", "High-level or strategic risks that could impact departmental objectives or public reputation. Usually captured in the Departmental Risk Register. Examples: Failure to deliver strategic outcomes, budgetary control, workforce capability, cyber security, major policy change.", "Across the entire department (e.g. DfE)", 4, true, now, now });

            migrationBuilder.InsertData(
                table: "RiskTiers",
                columns: new[] { "Code", "Name", "Description", "Summary", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { "CROSS_GOVERNMENT", "Cross-government / systemic risk", "Risks that transcend departmental boundaries and could impact public sector capability, continuity, or reputation. Managed by Cabinet Office / HMT via Government Risk Register. Examples: Major data breach across departments, shared platform failure (e.g. Notify, GOV.UK), policy shifts, economic shocks.", "Across multiple departments or sectors", 5, true, now, now });

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskTiers_RiskTierId",
                table: "Risks",
                column: "RiskTierId",
                principalTable: "RiskTiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskTiers_RiskTierId",
                table: "Risks");

            migrationBuilder.DropTable(
                name: "RiskTiers");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskTierId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskTierId",
                table: "Risks");
        }
    }
}
