using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class FipsBusinessAreaBusinessAreaLookupId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BusinessAreaLookupId",
                table: "FipsBusinessAreas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FipsBusinessAreas_BusinessAreaLookupId",
                table: "FipsBusinessAreas",
                column: "BusinessAreaLookupId",
                unique: true,
                filter: "[BusinessAreaLookupId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FipsBusinessAreas_BusinessAreaLookups_BusinessAreaLookupId",
                table: "FipsBusinessAreas",
                column: "BusinessAreaLookupId",
                principalTable: "BusinessAreaLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FipsBusinessAreas_BusinessAreaLookups_BusinessAreaLookupId",
                table: "FipsBusinessAreas");

            migrationBuilder.DropIndex(
                name: "IX_FipsBusinessAreas_BusinessAreaLookupId",
                table: "FipsBusinessAreas");

            migrationBuilder.DropColumn(
                name: "BusinessAreaLookupId",
                table: "FipsBusinessAreas");
        }
    }
}
