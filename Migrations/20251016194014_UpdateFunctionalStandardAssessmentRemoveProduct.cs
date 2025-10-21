using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFunctionalStandardAssessmentRemoveProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FipsId",
                table: "FunctionalStandardAssessments");

            migrationBuilder.AddColumn<string>(
                name: "AssessmentName",
                table: "FunctionalStandardAssessments",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FunctionalStandardAssessments",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssessmentName",
                table: "FunctionalStandardAssessments");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FunctionalStandardAssessments");

            migrationBuilder.AddColumn<string>(
                name: "FipsId",
                table: "FunctionalStandardAssessments",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
