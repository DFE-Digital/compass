using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingMetricsValidFrom : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing metrics to have September 2025 as ValidFrom
            migrationBuilder.Sql(@"
                UPDATE PerformanceMetrics 
                SET ValidFromYear = 2025, ValidFromMonth = 9 
                WHERE ValidFromYear = 0 OR ValidFromMonth = 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
