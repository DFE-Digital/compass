using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddObjectiveSroFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OutcomeSroUserId",
                table: "Objectives",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ThemeSroUserId",
                table: "Objectives",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_OutcomeSroUserId",
                table: "Objectives",
                column: "OutcomeSroUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Objectives_ThemeSroUserId",
                table: "Objectives",
                column: "ThemeSroUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Objectives_Users_OutcomeSroUserId",
                table: "Objectives",
                column: "OutcomeSroUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Objectives_Users_ThemeSroUserId",
                table: "Objectives",
                column: "ThemeSroUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Objectives_Users_OutcomeSroUserId",
                table: "Objectives");

            migrationBuilder.DropForeignKey(
                name: "FK_Objectives_Users_ThemeSroUserId",
                table: "Objectives");

            migrationBuilder.DropIndex(
                name: "IX_Objectives_OutcomeSroUserId",
                table: "Objectives");

            migrationBuilder.DropIndex(
                name: "IX_Objectives_ThemeSroUserId",
                table: "Objectives");

            migrationBuilder.DropColumn(
                name: "OutcomeSroUserId",
                table: "Objectives");

            migrationBuilder.DropColumn(
                name: "ThemeSroUserId",
                table: "Objectives");
        }
    }
}
