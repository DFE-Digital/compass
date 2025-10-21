using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddProductReporting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FipsId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductReturns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductMetricValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductReturnId = table.Column<int>(type: "INTEGER", nullable: false),
                    PerformanceMetricId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    IsComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductMetricValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductMetricValues_PerformanceMetrics_PerformanceMetricId",
                        column: x => x.PerformanceMetricId,
                        principalTable: "PerformanceMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductMetricValues_ProductReturns_ProductReturnId",
                        column: x => x.ProductReturnId,
                        principalTable: "ProductReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductMetricValues_PerformanceMetricId",
                table: "ProductMetricValues",
                column: "PerformanceMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductMetricValues_ProductReturnId_PerformanceMetricId",
                table: "ProductMetricValues",
                columns: new[] { "ProductReturnId", "PerformanceMetricId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductReturns_FipsId_Year_Month",
                table: "ProductReturns",
                columns: new[] { "FipsId", "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductMetricValues");

            migrationBuilder.DropTable(
                name: "ProductReturns");
        }
    }
}
