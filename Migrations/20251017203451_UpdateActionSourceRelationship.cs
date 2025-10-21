using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class UpdateActionSourceRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionSources_ActionSourceId",
                table: "Actions");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionSources_ActionSourceId",
                table: "Actions",
                column: "ActionSourceId",
                principalTable: "ActionSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actions_ActionSources_ActionSourceId",
                table: "Actions");

            migrationBuilder.AddForeignKey(
                name: "FK_Actions_ActionSources_ActionSourceId",
                table: "Actions",
                column: "ActionSourceId",
                principalTable: "ActionSources",
                principalColumn: "Id");
        }
    }
}
