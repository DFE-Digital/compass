using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnterpriseMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Identifier = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    HintText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ValueType = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidationRules = table.Column<string>(type: "TEXT", nullable: false),
                    ValidFromYear = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidFromMonth = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseReturns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SubmittedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseReturns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnterpriseMetricValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EnterpriseReturnId = table.Column<int>(type: "INTEGER", nullable: false),
                    EnterpriseMetricId = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: true),
                    IsComplete = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnterpriseMetricValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnterpriseMetricValues_EnterpriseMetrics_EnterpriseMetricId",
                        column: x => x.EnterpriseMetricId,
                        principalTable: "EnterpriseMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EnterpriseMetricValues_EnterpriseReturns_EnterpriseReturnId",
                        column: x => x.EnterpriseReturnId,
                        principalTable: "EnterpriseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseMetrics_Identifier",
                table: "EnterpriseMetrics",
                column: "Identifier",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseMetricValues_EnterpriseMetricId",
                table: "EnterpriseMetricValues",
                column: "EnterpriseMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseMetricValues_EnterpriseReturnId_EnterpriseMetricId",
                table: "EnterpriseMetricValues",
                columns: new[] { "EnterpriseReturnId", "EnterpriseMetricId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnterpriseReturns_Year_Month",
                table: "EnterpriseReturns",
                columns: new[] { "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnterpriseMetricValues");

            migrationBuilder.DropTable(
                name: "EnterpriseMetrics");

            migrationBuilder.DropTable(
                name: "EnterpriseReturns");
        }
    }
}
