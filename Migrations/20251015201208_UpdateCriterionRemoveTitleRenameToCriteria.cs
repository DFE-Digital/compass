using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCriterionRemoveTitleRenameToCriteria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Title",
                table: "Criteria");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Criteria",
                newName: "Criteria");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Criteria",
                table: "Criteria",
                newName: "Description");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Criteria",
                type: "TEXT",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }
    }
}
