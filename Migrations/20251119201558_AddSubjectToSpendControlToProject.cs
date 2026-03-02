using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjectToSpendControlToProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSubjectToSpendControl",
                table: "Projects",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSubjectToSpendControl",
                table: "Projects");
        }
    }
}
