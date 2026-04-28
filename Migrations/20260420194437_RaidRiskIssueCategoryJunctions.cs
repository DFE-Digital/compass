using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class RaidRiskIssueCategoryJunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueIssueCategories",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    IssueCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueIssueCategories", x => new { x.IssueId, x.IssueCategoryId });
                    table.ForeignKey(
                        name: "FK_IssueIssueCategories_IssueCategories_IssueCategoryId",
                        column: x => x.IssueCategoryId,
                        principalTable: "IssueCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueIssueCategories_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RiskRiskCategories",
                columns: table => new
                {
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    RiskCategoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskRiskCategories", x => new { x.RiskId, x.RiskCategoryId });
                    table.ForeignKey(
                        name: "FK_RiskRiskCategories_RiskCategories_RiskCategoryId",
                        column: x => x.RiskCategoryId,
                        principalTable: "RiskCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskRiskCategories_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueIssueCategories_IssueCategoryId",
                table: "IssueIssueCategories",
                column: "IssueCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskRiskCategories_RiskCategoryId",
                table: "RiskRiskCategories",
                column: "RiskCategoryId");

            migrationBuilder.Sql("""
                INSERT INTO RiskRiskCategories (RiskId, RiskCategoryId)
                SELECT r.Id, r.RiskCategoryId
                FROM Risks r
                WHERE r.RiskCategoryId IS NOT NULL
                  AND r.IsDeleted = 0
                  AND NOT EXISTS (
                    SELECT 1 FROM RiskRiskCategories x
                    WHERE x.RiskId = r.Id AND x.RiskCategoryId = r.RiskCategoryId);
                """);

            migrationBuilder.Sql("""
                INSERT INTO IssueIssueCategories (IssueId, IssueCategoryId)
                SELECT i.Id, i.IssueCategoryId
                FROM Issues i
                WHERE i.IssueCategoryId IS NOT NULL
                  AND i.IsDeleted = 0
                  AND NOT EXISTS (
                    SELECT 1 FROM IssueIssueCategories x
                    WHERE x.IssueId = i.Id AND x.IssueCategoryId = i.IssueCategoryId);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueIssueCategories");

            migrationBuilder.DropTable(
                name: "RiskRiskCategories");
        }
    }
}
