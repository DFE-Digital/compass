using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestoneUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MilestoneUpdates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MilestoneId = table.Column<int>(type: "int", nullable: false),
                    UpdateDetails = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PreviousProgress = table.Column<int>(type: "int", nullable: true),
                    NewProgress = table.Column<int>(type: "int", nullable: true),
                    UpdatedByEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UpdatedByName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MilestoneUpdates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MilestoneUpdates_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MilestoneUpdates_MilestoneId",
                table: "MilestoneUpdates",
                column: "MilestoneId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MilestoneUpdates");
        }
    }
}
