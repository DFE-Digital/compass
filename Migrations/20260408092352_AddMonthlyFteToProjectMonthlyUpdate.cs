using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyFteToProjectMonthlyUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyMspFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyPermFte",
                table: "ProjectMonthlyUpdates",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MonthlyMspFte",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "MonthlyPermFte",
                table: "ProjectMonthlyUpdates");
        }
    }
}
