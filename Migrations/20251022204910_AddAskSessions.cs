using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddAskSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "AskQueries",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AskSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AskSessions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AskQueries_SessionId",
                table: "AskQueries",
                column: "SessionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AskQueries_AskSessions_SessionId",
                table: "AskQueries",
                column: "SessionId",
                principalTable: "AskSessions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AskQueries_AskSessions_SessionId",
                table: "AskQueries");

            migrationBuilder.DropTable(
                name: "AskSessions");

            migrationBuilder.DropIndex(
                name: "IX_AskQueries_SessionId",
                table: "AskQueries");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "AskQueries");
        }
    }
}
