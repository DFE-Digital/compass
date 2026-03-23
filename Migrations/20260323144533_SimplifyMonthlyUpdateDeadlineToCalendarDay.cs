using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyMonthlyUpdateDeadlineToCalendarDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DueCalendarDay",
                table: "MonthlyUpdateDeadlineConfigs",
                type: "int",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.Sql(
                """
                UPDATE MonthlyUpdateDeadlineConfigs
                SET DueCalendarDay =
                    CASE
                        WHEN WorkingDayDeadline < 1 THEN 1
                        WHEN WorkingDayDeadline > 31 THEN 31
                        ELSE WorkingDayDeadline
                    END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DueCalendarDay",
                table: "MonthlyUpdateDeadlineConfigs");
        }
    }
}
