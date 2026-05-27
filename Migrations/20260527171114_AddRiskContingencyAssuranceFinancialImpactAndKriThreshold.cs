using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskContingencyAssuranceFinancialImpactAndKriThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Contingency",
                table: "Risks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Assurance",
                table: "Risks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinancialImpact",
                table: "Risks",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Threshold",
                table: "RiskKeyRiskIndicators",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Contingency",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "Assurance",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "FinancialImpact",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "Threshold",
                table: "RiskKeyRiskIndicators");
        }
    }
}
