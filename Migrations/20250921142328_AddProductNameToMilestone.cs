using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class AddProductNameToMilestone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "Milestones",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "Milestones");
        }
    }
}
