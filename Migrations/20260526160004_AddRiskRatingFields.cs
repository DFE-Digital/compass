using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskRatingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentImpactLevelId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentLikelihoodId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentScore",
                table: "Risks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResidualImpactLevelId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResidualLikelihoodId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToleranceImpactLevelId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ToleranceLikelihoodId",
                table: "Risks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ToleranceScore",
                table: "Risks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RiskRatingHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RiskId = table.Column<int>(type: "int", nullable: false),
                    RatingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LikelihoodId = table.Column<int>(type: "int", nullable: true),
                    ImpactLevelId = table.Column<int>(type: "int", nullable: true),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskRatingHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RiskRatingHistory_RiskImpactLevels_ImpactLevelId",
                        column: x => x.ImpactLevelId,
                        principalTable: "RiskImpactLevels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RiskRatingHistory_RiskLikelihoods_LikelihoodId",
                        column: x => x.LikelihoodId,
                        principalTable: "RiskLikelihoods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RiskRatingHistory_Risks_RiskId",
                        column: x => x.RiskId,
                        principalTable: "Risks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RiskRatingHistory_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Risks_CurrentImpactLevelId",
                table: "Risks",
                column: "CurrentImpactLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_CurrentLikelihoodId",
                table: "Risks",
                column: "CurrentLikelihoodId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ResidualImpactLevelId",
                table: "Risks",
                column: "ResidualImpactLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ResidualLikelihoodId",
                table: "Risks",
                column: "ResidualLikelihoodId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ToleranceImpactLevelId",
                table: "Risks",
                column: "ToleranceImpactLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Risks_ToleranceLikelihoodId",
                table: "Risks",
                column: "ToleranceLikelihoodId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskRatingHistory_ChangedAt",
                table: "RiskRatingHistory",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RiskRatingHistory_ChangedByUserId",
                table: "RiskRatingHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskRatingHistory_ImpactLevelId",
                table: "RiskRatingHistory",
                column: "ImpactLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskRatingHistory_LikelihoodId",
                table: "RiskRatingHistory",
                column: "LikelihoodId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskRatingHistory_RiskId",
                table: "RiskRatingHistory",
                column: "RiskId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskRatingHistory_RiskId_RatingType",
                table: "RiskRatingHistory",
                columns: new[] { "RiskId", "RatingType" });

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskImpactLevels_CurrentImpactLevelId",
                table: "Risks",
                column: "CurrentImpactLevelId",
                principalTable: "RiskImpactLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskImpactLevels_ResidualImpactLevelId",
                table: "Risks",
                column: "ResidualImpactLevelId",
                principalTable: "RiskImpactLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskImpactLevels_ToleranceImpactLevelId",
                table: "Risks",
                column: "ToleranceImpactLevelId",
                principalTable: "RiskImpactLevels",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskLikelihoods_CurrentLikelihoodId",
                table: "Risks",
                column: "CurrentLikelihoodId",
                principalTable: "RiskLikelihoods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskLikelihoods_ResidualLikelihoodId",
                table: "Risks",
                column: "ResidualLikelihoodId",
                principalTable: "RiskLikelihoods",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskLikelihoods_ToleranceLikelihoodId",
                table: "Risks",
                column: "ToleranceLikelihoodId",
                principalTable: "RiskLikelihoods",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskImpactLevels_CurrentImpactLevelId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskImpactLevels_ResidualImpactLevelId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskImpactLevels_ToleranceImpactLevelId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskLikelihoods_CurrentLikelihoodId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskLikelihoods_ResidualLikelihoodId",
                table: "Risks");

            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskLikelihoods_ToleranceLikelihoodId",
                table: "Risks");

            migrationBuilder.DropTable(
                name: "RiskRatingHistory");

            migrationBuilder.DropIndex(
                name: "IX_Risks_CurrentImpactLevelId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_CurrentLikelihoodId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_ResidualImpactLevelId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_ResidualLikelihoodId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_ToleranceImpactLevelId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_ToleranceLikelihoodId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "CurrentImpactLevelId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "CurrentLikelihoodId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "CurrentScore",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "ResidualImpactLevelId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "ResidualLikelihoodId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "ToleranceImpactLevelId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "ToleranceLikelihoodId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "ToleranceScore",
                table: "Risks");
        }
    }
}
