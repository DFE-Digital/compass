using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandRequestImpactColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "RiskTypes",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "Medium");

            migrationBuilder.AddColumn<string>(
                name: "ImpactLevel",
                table: "DemandRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImpactSummary",
                table: "DemandRequests",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PredictedRiskLevel",
                table: "DemandRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskLevelOverride",
                table: "DemandRequests",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Severity",
                table: "RiskTypes");

            migrationBuilder.DropColumn(
                name: "ImpactLevel",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "ImpactSummary",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "PredictedRiskLevel",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "RiskLevelOverride",
                table: "DemandRequests");
        }
    }
}
