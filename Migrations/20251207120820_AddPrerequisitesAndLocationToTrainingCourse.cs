using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddPrerequisitesAndLocationToTrainingCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "TrainingCourses",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prerequisites",
                table: "TrainingCourses",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "TrainingCourses");

            migrationBuilder.DropColumn(
                name: "Prerequisites",
                table: "TrainingCourses");
        }
    }
}
