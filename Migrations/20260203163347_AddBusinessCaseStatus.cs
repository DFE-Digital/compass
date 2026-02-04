using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessCaseStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BusinessCases",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StatusLookupId",
                table: "BusinessCases",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessCaseStatusLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CssClass = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCaseStatusLookups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCases_StatusLookupId",
                table: "BusinessCases",
                column: "StatusLookupId");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessCases_BusinessCaseStatusLookups_StatusLookupId",
                table: "BusinessCases",
                column: "StatusLookupId",
                principalTable: "BusinessCaseStatusLookups",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessCases_BusinessCaseStatusLookups_StatusLookupId",
                table: "BusinessCases");

            migrationBuilder.DropTable(
                name: "BusinessCaseStatusLookups");

            migrationBuilder.DropIndex(
                name: "IX_BusinessCases_StatusLookupId",
                table: "BusinessCases");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BusinessCases");

            migrationBuilder.DropColumn(
                name: "StatusLookupId",
                table: "BusinessCases");
        }
    }
}
