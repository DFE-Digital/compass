using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class ChangeRiskOwnerToEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Risks_Users_OwnerUserId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Risks_OwnerUserId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Risks");

            migrationBuilder.AddColumn<string>(
                name: "OwnerEmail",
                table: "Risks",
                type: "TEXT",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerEmail",
                table: "Risks");

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserId",
                table: "Risks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_OwnerUserId",
                table: "Risks",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Risks_Users_OwnerUserId",
                table: "Risks",
                column: "OwnerUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
