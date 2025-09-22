using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class AddMandateAndStageFieldsToPerformanceMetricV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mandate",
                table: "PerformanceMetrics",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "StageA",
                table: "PerformanceMetrics",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StageB",
                table: "PerformanceMetrics",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StageD",
                table: "PerformanceMetrics",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StageL",
                table: "PerformanceMetrics",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StageR",
                table: "PerformanceMetrics",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Mandate",
                table: "PerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "StageA",
                table: "PerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "StageB",
                table: "PerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "StageD",
                table: "PerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "StageL",
                table: "PerformanceMetrics");

            migrationBuilder.DropColumn(
                name: "StageR",
                table: "PerformanceMetrics");
        }
    }
}
