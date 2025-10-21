using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskTypeIdToRisk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RiskTypeId",
                table: "Risks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_RiskTypeId",
                table: "Risks",
                column: "RiskTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_RiskTypes_RiskTypeId",
                table: "Risks",
                column: "RiskTypeId",
                principalTable: "RiskTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Risks_RiskTypes_RiskTypeId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_RiskTypeId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "RiskTypeId",
                table: "Risks");
        }
    }
}
