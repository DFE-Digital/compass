using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddRaidRegisterSpreadsheetLayouts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaidRegisterSpreadsheetLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EntityType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ColumnOrderJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidRegisterSpreadsheetLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidRegisterSpreadsheetLayouts_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterSpreadsheetLayouts_EntityType",
                table: "RaidRegisterSpreadsheetLayouts",
                column: "EntityType",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidRegisterSpreadsheetLayouts_UpdatedByUserId",
                table: "RaidRegisterSpreadsheetLayouts",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidRegisterSpreadsheetLayouts");
        }
    }
}
