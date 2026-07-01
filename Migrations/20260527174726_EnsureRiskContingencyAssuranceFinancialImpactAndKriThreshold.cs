using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class EnsureRiskContingencyAssuranceFinancialImpactAndKriThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prior migration 20260527171114 was applied with an empty Up(); add columns if missing.
            migrationBuilder.Sql("""
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Risks' AND COLUMN_NAME = 'Contingency')
                    ALTER TABLE [Risks] ADD [Contingency] nvarchar(450) NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Risks' AND COLUMN_NAME = 'Assurance')
                    ALTER TABLE [Risks] ADD [Assurance] nvarchar(450) NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Risks' AND COLUMN_NAME = 'FinancialImpact')
                    ALTER TABLE [Risks] ADD [FinancialImpact] nvarchar(450) NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RiskKeyRiskIndicators' AND COLUMN_NAME = 'Threshold')
                    ALTER TABLE [RiskKeyRiskIndicators] ADD [Threshold] nvarchar(450) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Assurance",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "Contingency",
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
