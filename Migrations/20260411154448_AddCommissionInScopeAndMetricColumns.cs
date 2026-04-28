using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionInScopeAndMetricColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InScopePhases",
                table: "Commissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InScopeTypes",
                table: "Commissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncludedPerformanceMetricIds",
                table: "Commissions",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InScopePhases",
                table: "Commissions");

            migrationBuilder.DropColumn(
                name: "InScopeTypes",
                table: "Commissions");

            migrationBuilder.DropColumn(
                name: "IncludedPerformanceMetricIds",
                table: "Commissions");
        }
    }
}
