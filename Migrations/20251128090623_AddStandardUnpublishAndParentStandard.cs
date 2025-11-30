using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddStandardUnpublishAndParentStandard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentStandardId",
                table: "DdtStandards",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DdtStandardUnpublishAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: false),
                    UnpublishedByUserId = table.Column<int>(type: "int", nullable: false),
                    UnpublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardUnpublishAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardUnpublishAudits_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdtStandardUnpublishAudits_Users_UnpublishedByUserId",
                        column: x => x.UnpublishedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandards_ParentStandardId",
                table: "DdtStandards",
                column: "ParentStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardUnpublishAudits_DdtStandardId",
                table: "DdtStandardUnpublishAudits",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardUnpublishAudits_UnpublishedByUserId",
                table: "DdtStandardUnpublishAudits",
                column: "UnpublishedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_DdtStandards_DdtStandards_ParentStandardId",
                table: "DdtStandards",
                column: "ParentStandardId",
                principalTable: "DdtStandards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DdtStandards_DdtStandards_ParentStandardId",
                table: "DdtStandards");

            migrationBuilder.DropTable(
                name: "DdtStandardUnpublishAudits");

            migrationBuilder.DropIndex(
                name: "IX_DdtStandards_ParentStandardId",
                table: "DdtStandards");

            migrationBuilder.DropColumn(
                name: "ParentStandardId",
                table: "DdtStandards");
        }
    }
}
