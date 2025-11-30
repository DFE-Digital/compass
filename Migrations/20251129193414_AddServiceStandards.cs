using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceStandards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ServiceStandards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StandardNumber = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStandards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceStandards_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceStandards_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ServiceStandardPhaseGuidance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceStandardId = table.Column<int>(type: "int", nullable: false),
                    Phase = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Guidance = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    KeyActivities = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    QuestionsToConsider = table.Column<string>(type: "nvarchar(max)", maxLength: 450, nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStandardPhaseGuidance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceStandardPhaseGuidance_ServiceStandards_ServiceStandardId",
                        column: x => x.ServiceStandardId,
                        principalTable: "ServiceStandards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ServiceStandardPhaseGuidance_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ServiceStandardPhaseGuidance_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStandardPhaseGuidance_CreatedByUserId",
                table: "ServiceStandardPhaseGuidance",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStandardPhaseGuidance_ServiceStandardId",
                table: "ServiceStandardPhaseGuidance",
                column: "ServiceStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStandardPhaseGuidance_UpdatedByUserId",
                table: "ServiceStandardPhaseGuidance",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStandards_CreatedByUserId",
                table: "ServiceStandards",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceStandards_UpdatedByUserId",
                table: "ServiceStandards",
                column: "UpdatedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceStandardPhaseGuidance");

            migrationBuilder.DropTable(
                name: "ServiceStandards");
        }
    }
}
