using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddMonthlyUpdateNarratives : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MonthlyUpdateNarratives",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectMonthlyUpdateId = table.Column<int>(type: "int", nullable: false),
                    Narrative = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedByEntraId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyUpdateNarratives", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyUpdateNarratives_ProjectMonthlyUpdates_ProjectMonthlyUpdateId",
                        column: x => x.ProjectMonthlyUpdateId,
                        principalTable: "ProjectMonthlyUpdates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonthlyUpdateNarratives_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyUpdateNarratives_CreatedByUserId",
                table: "MonthlyUpdateNarratives",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyUpdateNarratives_ProjectMonthlyUpdateId",
                table: "MonthlyUpdateNarratives",
                column: "ProjectMonthlyUpdateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyUpdateNarratives");
        }
    }
}
