using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectArtefacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectArtefacts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Url = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedByEntraId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectArtefacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectArtefacts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectArtefacts_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProjectArtefacts_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectArtefacts_CreatedAt",
                table: "ProjectArtefacts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectArtefacts_CreatedByUserId",
                table: "ProjectArtefacts",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectArtefacts_IsDeleted",
                table: "ProjectArtefacts",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectArtefacts_ProjectId",
                table: "ProjectArtefacts",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectArtefacts_UpdatedByUserId",
                table: "ProjectArtefacts",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectArtefacts");
        }
    }
}
