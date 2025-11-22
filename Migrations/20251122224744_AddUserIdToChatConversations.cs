using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToChatConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ChatConversations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChatConversations_UserId",
                table: "ChatConversations",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ChatConversations_Users_UserId",
                table: "ChatConversations",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChatConversations_Users_UserId",
                table: "ChatConversations");

            migrationBuilder.DropIndex(
                name: "IX_ChatConversations_UserId",
                table: "ChatConversations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ChatConversations");
        }
    }
}
