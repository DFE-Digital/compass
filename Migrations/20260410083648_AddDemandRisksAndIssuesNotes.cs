using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDemandRisksAndIssuesNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DemandIssuesNotes",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DemandRisksNotes",
                table: "DemandPipelineRequests",
                type: "nvarchar(max)",
                maxLength: -1,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DemandIssuesNotes",
                table: "DemandPipelineRequests");

            migrationBuilder.DropColumn(
                name: "DemandRisksNotes",
                table: "DemandPipelineRequests");
        }
    }
}
