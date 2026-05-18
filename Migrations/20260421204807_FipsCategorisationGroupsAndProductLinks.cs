using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class FipsCategorisationGroupsAndProductLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FipsCategorisationGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsCategorisationGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FipsCategorisationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FipsCategorisationGroupId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsCategorisationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FipsCategorisationItems_FipsCategorisationGroups_FipsCategorisationGroupId",
                        column: x => x.FipsCategorisationGroupId,
                        principalTable: "FipsCategorisationGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductFipsCategorisationItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FipsCategorisationItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductFipsCategorisationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductFipsCategorisationItems_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductFipsCategorisationItems_FipsCategorisationItems_FipsCategorisationItemId",
                        column: x => x.FipsCategorisationItemId,
                        principalTable: "FipsCategorisationItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductFipsCategorisationItems_CMDBProductId",
                table: "CMDBProductFipsCategorisationItems",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductFipsCategorisationItems_FipsCategorisationItemId",
                table: "CMDBProductFipsCategorisationItems",
                column: "FipsCategorisationItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FipsCategorisationItems_FipsCategorisationGroupId",
                table: "FipsCategorisationItems",
                column: "FipsCategorisationGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CMDBProductFipsCategorisationItems");

            migrationBuilder.DropTable(
                name: "FipsCategorisationItems");

            migrationBuilder.DropTable(
                name: "FipsCategorisationGroups");
        }
    }
}
