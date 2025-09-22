using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FipsReporting.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PerformanceMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UniqueId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    LegalRegulatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notice = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ReportableInPhase = table.Column<string>(type: "TEXT", nullable: false),
                    Measure = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Mandatory = table.Column<bool>(type: "INTEGER", nullable: false),
                    ValidationCriteria = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CanReportNullReturn = table.Column<bool>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerformanceMetricData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PerformanceMetricId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReportingPeriod = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Comment = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsNullReturn = table.Column<bool>(type: "INTEGER", nullable: false),
                    SubmittedBy = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerformanceMetricData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PerformanceMetricData_PerformanceMetrics_PerformanceMetricId",
                        column: x => x.PerformanceMetricId,
                        principalTable: "PerformanceMetrics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetricData_PerformanceMetricId",
                table: "PerformanceMetricData",
                column: "PerformanceMetricId");

            migrationBuilder.CreateIndex(
                name: "IX_PerformanceMetrics_UniqueId",
                table: "PerformanceMetrics",
                column: "UniqueId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PerformanceMetricData");

            migrationBuilder.DropTable(
                name: "PerformanceMetrics");
        }
    }
}
