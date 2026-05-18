using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRiskLikelihoodImpactMatrixScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: columns may already exist from runtime repair (Program.cs) or manual fix while
            // __EFMigrationsHistory was out of sync — plain AddColumn then fails (e.g. duplicate column).
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.RiskLikelihoods', N'MatrixScore') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[RiskLikelihoods] ADD [MatrixScore] int NOT NULL CONSTRAINT DF_RiskLikelihoods_MatrixScore DEFAULT ((3));
                END
                IF COL_LENGTH(N'dbo.RiskImpactLevels', N'MatrixScore') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[RiskImpactLevels] ADD [MatrixScore] int NOT NULL CONSTRAINT DF_RiskImpactLevels_MatrixScore DEFAULT ((3));
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.RiskLikelihoods', N'MatrixScore') IS NOT NULL
                    ALTER TABLE [dbo].[RiskLikelihoods] DROP COLUMN [MatrixScore];
                IF COL_LENGTH(N'dbo.RiskImpactLevels', N'MatrixScore') IS NOT NULL
                    ALTER TABLE [dbo].[RiskImpactLevels] DROP COLUMN [MatrixScore];
                """);
        }
    }
}
