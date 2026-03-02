using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddCapabilityGapModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CapabilityGaps",
                table: "UserProfessionalProfiles",
                newName: "CapabilityGapsLegacy");

            migrationBuilder.CreateTable(
                name: "CapabilityGaps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ActionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapabilityGaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapabilityGaps_Actions_ActionId",
                        column: x => x.ActionId,
                        principalTable: "Actions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CapabilityGaps_UserProfessionalProfiles_UserProfessionalProfileId",
                        column: x => x.UserProfessionalProfileId,
                        principalTable: "UserProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityGaps_ActionId",
                table: "CapabilityGaps",
                column: "ActionId");

            migrationBuilder.CreateIndex(
                name: "IX_CapabilityGaps_UserProfessionalProfileId",
                table: "CapabilityGaps",
                column: "UserProfessionalProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CapabilityGaps");

            migrationBuilder.RenameColumn(
                name: "CapabilityGapsLegacy",
                table: "UserProfessionalProfiles",
                newName: "CapabilityGaps");
        }
    }
}
