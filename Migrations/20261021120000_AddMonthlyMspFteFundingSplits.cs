using Compass.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CompassDbContext))]
    [Migration("20261021120000_AddMonthlyMspFteFundingSplits")]
    public class AddMonthlyMspFteFundingSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyMspAdminFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyMspProgrammeFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyMspCapitalFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyMspAdminFte",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "MonthlyMspProgrammeFte",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "MonthlyMspCapitalFte",
                table: "ProjectMonthlyUpdates");
        }
    }
}
