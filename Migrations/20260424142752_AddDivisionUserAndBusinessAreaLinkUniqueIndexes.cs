using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDivisionUserAndBusinessAreaLinkUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DivisionUsers_UserId",
                table: "DivisionUsers");

            migrationBuilder.DropIndex(
                name: "IX_DivisionBusinessAreas_DivisionId",
                table: "DivisionBusinessAreas");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionUsers_UserId_DivisionId",
                table: "DivisionUsers",
                columns: new[] { "UserId", "DivisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DivisionBusinessAreas_DivisionId_BusinessAreaLookupId",
                table: "DivisionBusinessAreas",
                columns: new[] { "DivisionId", "BusinessAreaLookupId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DivisionUsers_UserId_DivisionId",
                table: "DivisionUsers");

            migrationBuilder.DropIndex(
                name: "IX_DivisionBusinessAreas_DivisionId_BusinessAreaLookupId",
                table: "DivisionBusinessAreas");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionUsers_UserId",
                table: "DivisionUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DivisionBusinessAreas_DivisionId",
                table: "DivisionBusinessAreas",
                column: "DivisionId");
        }
    }
}
