using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidRegisterBusinessAreaToUserPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RaidRegisterBusinessAreaLookupId",
                table: "UserPreferences",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_RaidRegisterBusinessAreaLookupId",
                table: "UserPreferences",
                column: "RaidRegisterBusinessAreaLookupId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPreferences_BusinessAreaLookups_RaidRegisterBusinessAreaLookupId",
                table: "UserPreferences",
                column: "RaidRegisterBusinessAreaLookupId",
                principalTable: "BusinessAreaLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_BusinessAreaLookups_RaidRegisterBusinessAreaLookupId",
                table: "UserPreferences");

            migrationBuilder.DropIndex(
                name: "IX_UserPreferences_RaidRegisterBusinessAreaLookupId",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "RaidRegisterBusinessAreaLookupId",
                table: "UserPreferences");
        }
    }
}
