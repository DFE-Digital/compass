using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFipsIdToRAIDEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FipsId",
                table: "Risks",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FipsId",
                table: "Milestones",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FipsId",
                table: "Issues",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FipsId",
                table: "Actions",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Risks_FipsId",
                table: "Risks",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_FipsId",
                table: "Milestones",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_FipsId",
                table: "Issues",
                column: "FipsId");

            migrationBuilder.CreateIndex(
                name: "IX_Actions_FipsId",
                table: "Actions",
                column: "FipsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Risks_FipsId",
                table: "Risks");

            migrationBuilder.DropIndex(
                name: "IX_Milestones_FipsId",
                table: "Milestones");

            migrationBuilder.DropIndex(
                name: "IX_Issues_FipsId",
                table: "Issues");

            migrationBuilder.DropIndex(
                name: "IX_Actions_FipsId",
                table: "Actions");

            migrationBuilder.DropColumn(
                name: "FipsId",
                table: "Risks");

            migrationBuilder.DropColumn(
                name: "FipsId",
                table: "Milestones");

            migrationBuilder.DropColumn(
                name: "FipsId",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "FipsId",
                table: "Actions");
        }
    }
}
