using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryPriorities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var seedTimestamp = new DateTime(2025, 11, 14, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryPriorityId",
                table: "Projects",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryPriorities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryPriorities", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "DeliveryPriorities",
                columns: new[] { "Name", "Summary", "Description", "SortOrder", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "High", "Critical delivery focus", "Reserved for urgent projects with significant ministerial or statutory drivers.", 1, true, seedTimestamp, seedTimestamp },
                    { "Medium", "Important delivery focus", "Projects with clear departmental value that may tolerate minor delay.", 2, true, seedTimestamp, seedTimestamp },
                    { "Low", "Routine delivery focus", "Foundational or exploratory work that can flex if higher priorities emerge.", 3, true, seedTimestamp, seedTimestamp }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Projects_DeliveryPriorityId",
                table: "Projects",
                column: "DeliveryPriorityId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPriorities_IsActive",
                table: "DeliveryPriorities",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPriorities_Name",
                table: "DeliveryPriorities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryPriorities_SortOrder",
                table: "DeliveryPriorities",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_DeliveryPriorities_DeliveryPriorityId",
                table: "Projects",
                column: "DeliveryPriorityId",
                principalTable: "DeliveryPriorities",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_DeliveryPriorities_DeliveryPriorityId",
                table: "Projects");

            migrationBuilder.DropTable(
                name: "DeliveryPriorities");

            migrationBuilder.DropIndex(
                name: "IX_Projects_DeliveryPriorityId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DeliveryPriorityId",
                table: "Projects");
        }
    }
}
