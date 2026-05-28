using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddFipsDirectorateToCmdbProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FipsDirectorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DirectorateLookupId = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FipsDirectorates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FipsDirectorates_DirectorateLookups_DirectorateLookupId",
                        column: x => x.DirectorateLookupId,
                        principalTable: "DirectorateLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CMDBProductDirectorates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CMDBProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FipsDirectorateId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CMDBProductDirectorates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CMDBProductDirectorates_CMDBProducts_CMDBProductId",
                        column: x => x.CMDBProductId,
                        principalTable: "CMDBProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CMDBProductDirectorates_FipsDirectorates_FipsDirectorateId",
                        column: x => x.FipsDirectorateId,
                        principalTable: "FipsDirectorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductDirectorates_CMDBProductId",
                table: "CMDBProductDirectorates",
                column: "CMDBProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CMDBProductDirectorates_FipsDirectorateId",
                table: "CMDBProductDirectorates",
                column: "FipsDirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_FipsDirectorates_DirectorateLookupId",
                table: "FipsDirectorates",
                column: "DirectorateLookupId",
                unique: true,
                filter: "[DirectorateLookupId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CMDBProductDirectorates");

            migrationBuilder.DropTable(
                name: "FipsDirectorates");
        }
    }
}
