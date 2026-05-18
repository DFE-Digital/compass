using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class CreateRaidMonthlyReviewsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaidMonthlyReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecordType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RecordId = table.Column<int>(type: "int", nullable: false),
                    ReviewYear = table.Column<int>(type: "int", nullable: false),
                    ReviewMonth = table.Column<int>(type: "int", nullable: false),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MonthlyComment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidMonthlyReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidMonthlyReviews_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidMonthlyReviews_RecordType_RecordId_ReviewYear_ReviewMonth",
                table: "RaidMonthlyReviews",
                columns: new[] { "RecordType", "RecordId", "ReviewYear", "ReviewMonth" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RaidMonthlyReviews_ReviewedByUserId",
                table: "RaidMonthlyReviews",
                column: "ReviewedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidMonthlyReviews");
        }
    }
}
