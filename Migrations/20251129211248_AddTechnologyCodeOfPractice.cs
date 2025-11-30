using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddTechnologyCodeOfPractice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TechnologyCodeOfPractice",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PointNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    GuidanceUrl = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyCodeOfPractice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyCodeOfPractice_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TechnologyCodeOfPractice_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TechnologyCodeOfPracticeProfessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TechnologyCodeOfPracticeId = table.Column<int>(type: "int", nullable: false),
                    DdatProfessionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechnologyCodeOfPracticeProfessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnologyCodeOfPracticeProfessions_DdatProfessions_DdatProfessionId",
                        column: x => x.DdatProfessionId,
                        principalTable: "DdatProfessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnologyCodeOfPracticeProfessions_TechnologyCodeOfPractice_TechnologyCodeOfPracticeId",
                        column: x => x.TechnologyCodeOfPracticeId,
                        principalTable: "TechnologyCodeOfPractice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyCodeOfPractice_CreatedByUserId",
                table: "TechnologyCodeOfPractice",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyCodeOfPractice_UpdatedByUserId",
                table: "TechnologyCodeOfPractice",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyCodeOfPracticeProfessions_DdatProfessionId",
                table: "TechnologyCodeOfPracticeProfessions",
                column: "DdatProfessionId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnologyCodeOfPracticeProfessions_TechnologyCodeOfPracticeId",
                table: "TechnologyCodeOfPracticeProfessions",
                column: "TechnologyCodeOfPracticeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TechnologyCodeOfPracticeProfessions");

            migrationBuilder.DropTable(
                name: "TechnologyCodeOfPractice");
        }
    }
}
