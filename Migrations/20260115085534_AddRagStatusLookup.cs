using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRagStatusLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RagStatusLookupId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RagStatusLookupId",
                table: "ProjectRagHistories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RagStatusLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CssClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RagStatusLookups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_RagStatusLookupId",
                table: "Projects",
                column: "RagStatusLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectRagHistories_RagStatusLookupId",
                table: "ProjectRagHistories",
                column: "RagStatusLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_RagStatusLookups_IsActive",
                table: "RagStatusLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RagStatusLookups_Name",
                table: "RagStatusLookups",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProjectRagHistories_RagStatusLookups_RagStatusLookupId",
                table: "ProjectRagHistories",
                column: "RagStatusLookupId",
                principalTable: "RagStatusLookups",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_RagStatusLookups_RagStatusLookupId",
                table: "Projects",
                column: "RagStatusLookupId",
                principalTable: "RagStatusLookups",
                principalColumn: "Id");

            // Seed initial RAG statuses
            var seedTimestamp = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
            
            migrationBuilder.InsertData(
                table: "RagStatusLookups",
                columns: new[] { "Name", "Description", "SortOrder", "IsActive", "CssClass", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "Green", "On track, no issues", 1, true, "badge badge-green", seedTimestamp, seedTimestamp },
                    { "Amber-Green", "Minor issues, corrective action in place", 2, true, "badge badge-amber-green", seedTimestamp, seedTimestamp },
                    { "Amber", "Some issues, may impact delivery", 3, true, "badge badge-amber", seedTimestamp, seedTimestamp },
                    { "Amber-Red", "Significant issues, likely to impact delivery", 4, true, "badge badge-amber-red", seedTimestamp, seedTimestamp },
                    { "Red", "Critical issues, will impact delivery", 5, true, "badge badge-red", seedTimestamp, seedTimestamp }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProjectRagHistories_RagStatusLookups_RagStatusLookupId",
                table: "ProjectRagHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_RagStatusLookups_RagStatusLookupId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "RagStatusLookups");

            migrationBuilder.DropIndex(
                name: "IX_Projects_RagStatusLookupId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_ProjectRagHistories_RagStatusLookupId",
                table: "ProjectRagHistories");

            migrationBuilder.DropIndex(
                name: "IX_RagStatusLookups_IsActive",
                table: "RagStatusLookups");

            migrationBuilder.DropIndex(
                name: "IX_RagStatusLookups_Name",
                table: "RagStatusLookups");

            migrationBuilder.DropColumn(
                name: "RagStatusLookupId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RagStatusLookupId",
                table: "ProjectRagHistories");
        }
    }
}
