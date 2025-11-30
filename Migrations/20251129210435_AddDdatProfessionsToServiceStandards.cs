using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDdatProfessionsToServiceStandards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DdatProfessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DdatProfessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceStandardProfessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceStandardId = table.Column<int>(type: "int", nullable: false),
                    DdatProfessionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStandardProfessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceStandardProfessions_DdatProfessions_DdatProfessionId",
                        column: x => x.DdatProfessionId,
                        principalTable: "DdatProfessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceStandardProfessions_ServiceStandards_ServiceStandardId",
                        column: x => x.ServiceStandardId,
                        principalTable: "ServiceStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStandardProfessions_DdatProfessionId",
                table: "ServiceStandardProfessions",
                column: "DdatProfessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStandardProfessions_ServiceStandardId",
                table: "ServiceStandardProfessions",
                column: "ServiceStandardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceStandardProfessions");

            migrationBuilder.DropTable(
                name: "DdatProfessions");
        }
    }
}
