using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievementStatusToProjectOutcome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AchievementNotes",
                table: "ProjectOutcomes",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AchievementStatus",
                table: "ProjectOutcomes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                defaultValue: "In progress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AchievementNotes",
                table: "ProjectOutcomes");

            migrationBuilder.DropColumn(
                name: "AchievementStatus",
                table: "ProjectOutcomes");
        }
    }
}
