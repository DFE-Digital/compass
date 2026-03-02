using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomCourseFieldsToTrainingRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomCourseProvider",
                table: "TrainingRequests",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CustomCourseCost",
                table: "TrainingRequests",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomCourseUrl",
                table: "TrainingRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomCourseProvider",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "CustomCourseCost",
                table: "TrainingRequests");

            migrationBuilder.DropColumn(
                name: "CustomCourseUrl",
                table: "TrainingRequests");
        }
    }
}
