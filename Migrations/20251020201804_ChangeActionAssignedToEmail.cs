using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class ChangeActionAssignedToEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_Users_AssignedToUserId",
                table: "Actions");

            migrationBuilder.DropIndex(
                name: "IX_Actions_AssignedToUserId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Actions");

            migrationBuilder.AddColumn<string>(
                name: "AssignedToEmail",
                table: "Actions",
                type: "TEXT",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AssignedToEmail",
                table: "Actions",
                column: "AssignedToEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Actions_AssignedToEmail",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "AssignedToEmail",
                table: "Actions");

            migrationBuilder.AddColumn<int>(
                name: "AssignedToUserId",
                table: "Actions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actions_AssignedToUserId",
                table: "Actions",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_Users_AssignedToUserId",
                table: "Actions",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
