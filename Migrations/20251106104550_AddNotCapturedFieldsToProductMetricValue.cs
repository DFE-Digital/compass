using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddNotCapturedFieldsToProductMetricValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNotCaptured",
                table: "ProductMetricValues",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NotCapturedReason",
                table: "ProductMetricValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonForDifference",
                table: "ProductMetricValues",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsNotCaptured",
                table: "ProductMetricValues");

            migrationBuilder.DropColumn(
                name: "NotCapturedReason",
                table: "ProductMetricValues");

            migrationBuilder.DropColumn(
                name: "ReasonForDifference",
                table: "ProductMetricValues");
        }
    }
}
