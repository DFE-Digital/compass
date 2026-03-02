using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDdtStandardExceptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DdtStandardExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DdtStandardId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    StandardProductId = table.Column<int>(type: "int", nullable: true),
                    FipsProductId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GrantedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdtStandardExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DdtStandardExceptions_DdtStandards_DdtStandardId",
                        column: x => x.DdtStandardId,
                        principalTable: "DdtStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DdtStandardExceptions_StandardProducts_StandardProductId",
                        column: x => x.StandardProductId,
                        principalTable: "StandardProducts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DdtStandardExceptions_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DdtStandardExceptions_Users_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardExceptions_CreatedByUserId",
                table: "DdtStandardExceptions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardExceptions_DdtStandardId",
                table: "DdtStandardExceptions",
                column: "DdtStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardExceptions_GrantedByUserId",
                table: "DdtStandardExceptions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DdtStandardExceptions_StandardProductId",
                table: "DdtStandardExceptions",
                column: "StandardProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DdtStandardExceptions");
        }
    }
}
