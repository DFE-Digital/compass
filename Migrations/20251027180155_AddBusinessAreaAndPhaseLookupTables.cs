using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessAreaAndPhaseLookupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusinessAreaLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAreaLookups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PhaseLookups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhaseLookups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAreaLookups_IsActive",
                table: "BusinessAreaLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAreaLookups_Name",
                table: "BusinessAreaLookups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAreaLookups_SortOrder",
                table: "BusinessAreaLookups",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseLookups_IsActive",
                table: "PhaseLookups",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PhaseLookups_Name",
                table: "PhaseLookups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhaseLookups_SortOrder",
                table: "PhaseLookups",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusinessAreaLookups");

            migrationBuilder.DropTable(
                name: "PhaseLookups");
        }
    }
}
