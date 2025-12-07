using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddPrimaryAndSecondaryProfessionTagsToTrainingCourse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryProfessionTags",
                table: "TrainingCourses",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryProfessionTags",
                table: "TrainingCourses",
                type: "nvarchar(max)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryProfessionTags",
                table: "TrainingCourses");

            migrationBuilder.DropColumn(
                name: "SecondaryProfessionTags",
                table: "TrainingCourses");
        }
    }
}
