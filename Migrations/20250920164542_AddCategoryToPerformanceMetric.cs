using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToPerformanceMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "PerformanceMetrics",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "PerformanceMetrics");
        }
    }
}
