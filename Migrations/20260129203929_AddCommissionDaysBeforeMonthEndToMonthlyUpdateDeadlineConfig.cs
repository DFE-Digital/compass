using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddCommissionDaysBeforeMonthEndToMonthlyUpdateDeadlineConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CommissionDaysBeforeMonthEnd",
                table: "MonthlyUpdateDeadlineConfigs",
                type: "int",
                nullable: false,
                defaultValue: 6);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommissionDaysBeforeMonthEnd",
                table: "MonthlyUpdateDeadlineConfigs");
        }
    }
}
