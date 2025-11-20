using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class ChangeDirectoratesToUseBusinessAreas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDirectorates_DirectorateLookups_DirectorateLookupId",
                table: "ProjectDirectorates");

            migrationBuilder.RenameColumn(
                name: "DirectorateLookupId",
                table: "ProjectDirectorates",
                newName: "BusinessAreaLookupId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDirectorates_ProjectId_DirectorateLookupId",
                table: "ProjectDirectorates",
                newName: "IX_ProjectDirectorates_ProjectId_BusinessAreaLookupId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDirectorates_DirectorateLookupId",
                table: "ProjectDirectorates",
                newName: "IX_ProjectDirectorates_BusinessAreaLookupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDirectorates_BusinessAreaLookups_BusinessAreaLookupId",
                table: "ProjectDirectorates",
                column: "BusinessAreaLookupId",
                principalTable: "BusinessAreaLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectDirectorates_BusinessAreaLookups_BusinessAreaLookupId",
                table: "ProjectDirectorates");

            migrationBuilder.RenameColumn(
                name: "BusinessAreaLookupId",
                table: "ProjectDirectorates",
                newName: "DirectorateLookupId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDirectorates_ProjectId_BusinessAreaLookupId",
                table: "ProjectDirectorates",
                newName: "IX_ProjectDirectorates_ProjectId_DirectorateLookupId");

            migrationBuilder.RenameIndex(
                name: "IX_ProjectDirectorates_BusinessAreaLookupId",
                table: "ProjectDirectorates",
                newName: "IX_ProjectDirectorates_DirectorateLookupId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectDirectorates_DirectorateLookups_DirectorateLookupId",
                table: "ProjectDirectorates",
                column: "DirectorateLookupId",
                principalTable: "DirectorateLookups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
