using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAreaToRAIDEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BusinessArea",
                table: "Risks",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessArea",
                table: "Milestones",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessArea",
                table: "Issues",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BusinessArea",
                table: "Actions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessArea",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "BusinessArea",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "BusinessArea",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "BusinessArea",
                table: "Actions");
        }
    }
}
