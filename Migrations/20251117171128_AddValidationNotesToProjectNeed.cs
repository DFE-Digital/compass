using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddValidationNotesToProjectNeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ValidatedAt",
                table: "ProjectNeeds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidationNotes",
                table: "ProjectNeeds",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValidatedAt",
                table: "ProjectNeeds");

            migrationBuilder.DropColumn(
                name: "ValidationNotes",
                table: "ProjectNeeds");
        }
    }
}
