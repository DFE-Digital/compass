using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddLeadershipAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserBusinessAreaRoleAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    BusinessAreaLookupId = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserBusinessAreaRoleAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserBusinessAreaRoleAssignments_BusinessAreaLookups_BusinessAreaLookupId",
                        column: x => x.BusinessAreaLookupId,
                        principalTable: "BusinessAreaLookups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserBusinessAreaRoleAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinessAreaRoleAssignments_BusinessAreaLookupId",
                table: "UserBusinessAreaRoleAssignments",
                column: "BusinessAreaLookupId");

            migrationBuilder.CreateIndex(
                name: "IX_UserBusinessAreaRoleAssignments_UserId_BusinessAreaLookupId_Role",
                table: "UserBusinessAreaRoleAssignments",
                columns: new[] { "UserId", "BusinessAreaLookupId", "Role" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserBusinessAreaRoleAssignments");
        }
    }
}
