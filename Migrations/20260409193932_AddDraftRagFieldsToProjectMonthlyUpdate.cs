using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDraftRagFieldsToProjectMonthlyUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DraftPathToGreen",
                table: "ProjectMonthlyUpdates",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DraftRagJustification",
                table: "ProjectMonthlyUpdates",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DraftRagStatusLookupId",
                table: "ProjectMonthlyUpdates",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectMonthlyUpdates_DraftRagStatusLookupId",
                table: "ProjectMonthlyUpdates",
                column: "DraftRagStatusLookupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectMonthlyUpdates_RagStatusLookups_DraftRagStatusLookupId",
                table: "ProjectMonthlyUpdates",
                column: "DraftRagStatusLookupId",
                principalTable: "RagStatusLookups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectMonthlyUpdates_RagStatusLookups_DraftRagStatusLookupId",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropIndex(
                name: "IX_ProjectMonthlyUpdates_DraftRagStatusLookupId",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "DraftPathToGreen",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "DraftRagJustification",
                table: "ProjectMonthlyUpdates");

            migrationBuilder.DropColumn(
                name: "DraftRagStatusLookupId",
                table: "ProjectMonthlyUpdates");
        }
    }
}
