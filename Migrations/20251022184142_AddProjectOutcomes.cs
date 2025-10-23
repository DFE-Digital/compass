using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Outcomes",
                table: "Projects");

            migrationBuilder.AlterColumn<string>(
                name: "Aim",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.CreateTable(
                name: "ProjectOutcomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    MeasureOfSuccess = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ConfidenceExplanation = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectOutcomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectOutcomes_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutcomes_ConfidenceLevel",
                table: "ProjectOutcomes",
                column: "ConfidenceLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutcomes_ProjectId",
                table: "ProjectOutcomes",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectOutcomes_SortOrder",
                table: "ProjectOutcomes",
                column: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectOutcomes");

            migrationBuilder.AlterColumn<string>(
                name: "Aim",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Outcomes",
                table: "Projects",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");
        }
    }
}
