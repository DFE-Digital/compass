using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDueMonthOffsetAndDueDayRuleToMonthlyUpdateDeadlineConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DueDayRule",
                table: "MonthlyUpdateDeadlineConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DueMonthOffset",
                table: "MonthlyUpdateDeadlineConfigs",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueDayRule",
                table: "MonthlyUpdateDeadlineConfigs");

            migrationBuilder.DropColumn(
                name: "DueMonthOffset",
                table: "MonthlyUpdateDeadlineConfigs");
        }
    }
}
