using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCaseIdToDemandRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusinessCaseId",
                table: "DemandRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DemandRequests_BusinessCaseId",
                table: "DemandRequests",
                column: "BusinessCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_DemandRequests_BusinessCases_BusinessCaseId",
                table: "DemandRequests",
                column: "BusinessCaseId",
                principalTable: "BusinessCases",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DemandRequests_BusinessCases_BusinessCaseId",
                table: "DemandRequests");

            migrationBuilder.DropIndex(
                name: "IX_DemandRequests_BusinessCaseId",
                table: "DemandRequests");

            migrationBuilder.DropColumn(
                name: "BusinessCaseId",
                table: "DemandRequests");
        }
    }
}
