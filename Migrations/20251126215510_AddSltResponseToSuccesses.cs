using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddSltResponseToSuccesses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SltRespondedAt",
                table: "ProjectWeeklySuccessUpdates",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SltRespondedByEmail",
                table: "ProjectWeeklySuccessUpdates",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SltRespondedByName",
                table: "ProjectWeeklySuccessUpdates",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SltResponse",
                table: "ProjectWeeklySuccessUpdates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SltRespondedAt",
                table: "ProjectSuccesses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SltRespondedByEmail",
                table: "ProjectSuccesses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SltRespondedByName",
                table: "ProjectSuccesses",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SltResponse",
                table: "ProjectSuccesses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SltRespondedAt",
                table: "ProjectWeeklySuccessUpdates");

            migrationBuilder.DropColumn(
                name: "SltRespondedByEmail",
                table: "ProjectWeeklySuccessUpdates");

            migrationBuilder.DropColumn(
                name: "SltRespondedByName",
                table: "ProjectWeeklySuccessUpdates");

            migrationBuilder.DropColumn(
                name: "SltResponse",
                table: "ProjectWeeklySuccessUpdates");

            migrationBuilder.DropColumn(
                name: "SltRespondedAt",
                table: "ProjectSuccesses");

            migrationBuilder.DropColumn(
                name: "SltRespondedByEmail",
                table: "ProjectSuccesses");

            migrationBuilder.DropColumn(
                name: "SltRespondedByName",
                table: "ProjectSuccesses");

            migrationBuilder.DropColumn(
                name: "SltResponse",
                table: "ProjectSuccesses");
        }
    }
}
