using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Compass.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkReportingCyclesAndPeriods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportingCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    PeriodType = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportingCyclePeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportingCycleId = table.Column<int>(type: "int", nullable: false),
                    PeriodKey = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PeriodLabel = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportingCyclePeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportingCyclePeriods_ReportingCycles_ReportingCycleId",
                        column: x => x.ReportingCycleId,
                        principalTable: "ReportingCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportingCyclePeriods_ReportingCycleId_PeriodKey",
                table: "ReportingCyclePeriods",
                columns: new[] { "ReportingCycleId", "PeriodKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportingCycles_Code",
                table: "ReportingCycles",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportingCyclePeriods");

            migrationBuilder.DropTable(
                name: "ReportingCycles");
        }
    }
}
