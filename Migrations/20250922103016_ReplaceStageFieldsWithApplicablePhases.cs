using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceStageFieldsWithApplicablePhases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.AddColumn<string>(
                name: "ApplicablePhases",
                table: "PerformanceMetrics",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicablePhases",
                table: "PerformanceMetrics");

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
    }
}
